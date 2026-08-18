// Copyright © Gamesmiths Guild.

using System.Collections.Generic;
using Gamesmiths.Forge.Abilities;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Nodes;
using Gamesmiths.Forge.Statescript.Ports;
using Godot;
using NumericsVector3 = System.Numerics.Vector3;

namespace Gamesmiths.Forge.Godot.Core.Statescript.Nodes.State;

/// <summary>
/// State node that moves an entity to a destination over time, emitting <see cref="OnArrivedPort"/> when it gets there.
/// </summary>
/// <remarks>
/// <para>This drives the transform directly and does not solve collisions: it will pass through geometry. That is a
/// deliberate limit, because the alternative - moving a body properly - depends on which body type the entity is and on
/// the game's own movement rules. Use it for leaps, dashes, hook pulls and forced repositioning, where the path is
/// authored and a snag would be a bug rather than a feature.</para>
/// <para>Aborting stops the move where it stands. It does not snap to the destination and it does not return to the
/// start, so an interrupted leap leaves the character mid-air and whatever follows the abort decides what happens next.
/// </para>
/// <para>Configuration is captured in field initializers rather than a constructor body, because the base node
/// constructor calls <see cref="DefinePorts"/> and <see cref="DefineParameters"/> before a body would run.</para>
/// </remarks>
/// <param name="mode">Whether the value input is a duration or a speed.</param>
/// <param name="easing">How the travel is distributed over time.</param>
/// <param name="nodePath">Optional path to a descendant node to move instead of the entity's own spatial node.</param>
[StatescriptCategory("Spatial")]
public class MoveTo3DNode(
	MoveToMode mode = MoveToMode.Duration,
	MoveToEasing easing = MoveToEasing.Linear,
	string nodePath = "") : StateNode<MoveTo3DNodeContext>
{
	/// <summary>
	/// Input property index for the entity to move. Unbound means the ability's owner.
	/// </summary>
	public const byte EntityInput = 0;

	/// <summary>
	/// Input property index for the destination.
	/// </summary>
	public const byte DestinationInput = 1;

	/// <summary>
	/// Input property index for the duration in seconds, or the speed in units per second.
	/// </summary>
	public const byte ValueInput = 2;

	/// <summary>
	/// Input property index for the optional arc height.
	/// </summary>
	public const byte ArcHeightInput = 3;

	/// <summary>
	/// Output port index for the event emitted when the entity reaches the destination.
	/// </summary>
	public const byte OnArrivedPort = 4;

	private const double MinimumDuration = 0.0001;

	private readonly MoveToMode _mode = mode;
	private readonly MoveToEasing _easing = easing;
	private readonly string _nodePath = nodePath ?? string.Empty;

	/// <inheritdoc/>
	public override string Description =>
		"Moves an entity to a destination over time, emitting OnArrived on completion. Does not solve collisions.";

	/// <inheritdoc/>
	protected override void DefinePorts(List<InputPort> inputPorts, List<OutputPort> outputPorts)
	{
		base.DefinePorts(inputPorts, outputPorts);
		outputPorts.Add(CreatePort<EventPort>(OnArrivedPort, "OnArrived"));
	}

	/// <inheritdoc/>
	protected override void DefineParameters(List<InputProperty> inputProperties, List<OutputVariable> outputVariables)
	{
		inputProperties.Add(new InputProperty("Entity", typeof(IForgeEntity), IsOptional: true));
		inputProperties.Add(new InputProperty("Destination", typeof(NumericsVector3)));
		inputProperties.Add(new InputProperty("Value", typeof(double)));
		inputProperties.Add(new InputProperty("Arc Height", typeof(double), IsOptional: true));
	}

	/// <inheritdoc/>
	protected override void OnActivate(GraphContext graphContext)
	{
		MoveTo3DNodeContext nodeContext = graphContext.GetNodeContext<MoveTo3DNodeContext>(NodeID);
		nodeContext.ElapsedTime = 0;
		nodeContext.HasTarget = false;

		IForgeEntity? entity = ResolveEntityOrOwner(graphContext);

		if (!ForgeEntityBridge.TryGetSpatialNode3D(entity, _nodePath, out Node3D? spatialNode))
		{
			GD.PushWarning(
				"Statescript: Move To 3D found no Node3D for its entity" +
				(_nodePath.Length == 0 ? "." : $" at [{_nodePath}].") +
				" The move was skipped.");
			return;
		}

		if (!graphContext.TryResolve(InputProperties[DestinationInput].BoundName, out NumericsVector3 destination))
		{
			return;
		}

		graphContext.TryResolve(InputProperties[ValueInput].BoundName, out double value);
		graphContext.TryResolve(InputProperties[ArcHeightInput].BoundName, out double arcHeight);

		nodeContext.StartPosition = spatialNode.GlobalPosition;
		nodeContext.Destination = new Vector3(destination.X, destination.Y, destination.Z);
		nodeContext.ArcHeight = (float)arcHeight;
		nodeContext.HasTarget = true;

		nodeContext.Duration = _mode == MoveToMode.Speed
			? ResolveDurationFromSpeed(nodeContext, value)
			: value;

		if (nodeContext.Duration < MinimumDuration)
		{
			// A zero or negative duration is a teleport rather than a move, and it must still report arrival.
			ApplyPosition(spatialNode, nodeContext, 1);
			DeactivateNodeAndEmitMessage(graphContext, OnArrivedPort);
		}
	}

	/// <inheritdoc/>
	protected override void OnDeactivate(GraphContext graphContext)
	{
	}

	/// <inheritdoc/>
	protected override void OnUpdate(double deltaTime, GraphContext graphContext)
	{
		MoveTo3DNodeContext nodeContext = graphContext.GetNodeContext<MoveTo3DNodeContext>(NodeID);

		if (!nodeContext.HasTarget)
		{
			return;
		}

		IForgeEntity? entity = ResolveEntityOrOwner(graphContext);

		// Re-resolved each tick rather than cached: the node may have been freed mid-move, by a death or a despawn.
		if (!ForgeEntityBridge.TryGetSpatialNode3D(entity, _nodePath, out Node3D? spatialNode))
		{
			return;
		}

		nodeContext.ElapsedTime += deltaTime;

		double progress = nodeContext.ElapsedTime / nodeContext.Duration;

		if (progress >= 1)
		{
			ApplyPosition(spatialNode, nodeContext, 1);
			DeactivateNodeAndEmitMessage(graphContext, OnArrivedPort);
			return;
		}

		ApplyPosition(spatialNode, nodeContext, progress);
	}

	private static double ResolveDurationFromSpeed(MoveTo3DNodeContext nodeContext, double speed)
	{
		if (speed <= 0)
		{
			return 0;
		}

		return nodeContext.StartPosition.DistanceTo(nodeContext.Destination) / speed;
	}

	private static float Ease(MoveToEasing easing, float progress)
	{
		return easing switch
		{
			MoveToEasing.EaseIn => progress * progress,
			MoveToEasing.EaseOut => 1.0f - ((1.0f - progress) * (1.0f - progress)),
			MoveToEasing.EaseInOut => progress < 0.5f
				? 2.0f * progress * progress
				: 1.0f - (2.0f * (1.0f - progress) * (1.0f - progress)),
			_ => progress,
		};
	}

	private void ApplyPosition(Node3D spatialNode, MoveTo3DNodeContext nodeContext, double progress)
	{
		float clamped = Mathf.Clamp((float)progress, 0.0f, 1.0f);
		float eased = Ease(_easing, clamped);

		Vector3 position = nodeContext.StartPosition.Lerp(nodeContext.Destination, eased);

		if (Mathf.Abs(nodeContext.ArcHeight) > 0.0001f)
		{
			// A half sine peaks at the midpoint and is zero at both ends, so the arc never shifts where the move lands.
			position.Y += nodeContext.ArcHeight * Mathf.Sin(eased * Mathf.Pi);
		}

		spatialNode.GlobalPosition = position;
	}

	private IForgeEntity? ResolveEntityOrOwner(GraphContext graphContext)
	{
		StringKey boundName = InputProperties[EntityInput].BoundName;

		if (boundName != StringKey.Empty
			&& graphContext.TryResolveObject(boundName, out IForgeEntity? entity)
			&& entity is not null)
		{
			return entity;
		}

		return graphContext.TryGetActivationContext(out AbilityBehaviorContext? abilityContext)
			? abilityContext.Owner
			: null;
	}
}
