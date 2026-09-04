// Copyright © Gamesmiths Guild.

using System.Collections.Generic;
using Gamesmiths.Forge.Abilities;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Nodes;
using Gamesmiths.Forge.Statescript.Ports;
using Godot;

namespace Gamesmiths.Forge.Godot.Core.Statescript.Nodes.State;

/// <summary>
/// State node that turns an entity to a rotation over time, emitting <see cref="OnAlignedPort"/> when it gets there.
/// </summary>
/// <remarks>
/// <para>The rotation counterpart of Move To 2D, and the node for a turn that has to be seen: a caster winding up to
/// face their target, a turret coming round onto a lead. Setting a rotation outright is Set Rotation 2D; this is what
/// makes the same turn take time, which is what a graph gates a cast on.</para>
/// <para>The rotation is an angle in radians, not a quaternion, so core's Deg To Rad and Look At resolvers feed it
/// directly. The turn takes the shortest way round, resolved once at activation: a plane's rotation keeps counting
/// past a full turn, so a facing of -3 radians and one of 3 are a third of a turn apart rather than most of one, and
/// interpolating the two numbers would go the long way.</para>
/// <para>Unlike its 3D twin there is no guard on the rotation input. An unfilled angle is zero, which is a facing a
/// graph can genuinely mean; the quaternion an unfilled 3D operand resolves to is not a rotation at all.</para>
/// <para>Aborting stops the turn where it stands, the same as Move To: an interrupted wind-up leaves the caster part
/// way round, and whatever follows the abort decides what happens next.</para>
/// <para>Configuration is captured in field initializers rather than a constructor body, because the base node
/// constructor calls <see cref="DefinePorts"/> and <see cref="DefineParameters"/> before a body would run.</para>
/// </remarks>
/// <param name="mode">Whether the value input is a duration or an angular speed.</param>
/// <param name="nodePath">Optional path to a descendant node to turn instead of the entity's own spatial node.</param>
[StatescriptCategory("Spatial")]
public class RotateTo2DNode(MoveToMode mode = MoveToMode.Duration, string nodePath = "")
	: StateNode<RotateTo2DNodeContext>
{
	/// <summary>
	/// Input property index for the entity to turn. Unbound means the ability's owner.
	/// </summary>
	public const byte EntityInput = 0;

	/// <summary>
	/// Input property index for the rotation to turn to, in radians.
	/// </summary>
	public const byte RotationInput = 1;

	/// <summary>
	/// Input property index for the duration in seconds, or the angular speed in radians per second.
	/// </summary>
	public const byte ValueInput = 2;

	/// <summary>
	/// Output port index for the event emitted when the entity reaches the rotation.
	/// </summary>
	public const byte OnAlignedPort = 4;

	private const double MinimumDuration = 0.0001;

	private readonly MoveToMode _mode = mode;
	private readonly string _nodePath = nodePath ?? string.Empty;

	/// <inheritdoc/>
	public override string Description =>
		"Turns an entity to a rotation over time, in radians, emitting OnAligned on completion.";

	/// <inheritdoc/>
	protected override void DefinePorts(List<InputPort> inputPorts, List<OutputPort> outputPorts)
	{
		base.DefinePorts(inputPorts, outputPorts);
		outputPorts.Add(CreatePort<EventPort>(OnAlignedPort, "OnAligned"));
	}

	/// <inheritdoc/>
	protected override void DefineParameters(List<InputProperty> inputProperties, List<OutputVariable> outputVariables)
	{
		inputProperties.Add(new InputProperty("Entity", typeof(IForgeEntity), IsOptional: true));
		inputProperties.Add(new InputProperty("Rotation", typeof(double)));
		inputProperties.Add(new InputProperty("Value", typeof(double)));
	}

	/// <inheritdoc/>
	protected override void OnActivate(GraphContext graphContext)
	{
		RotateTo2DNodeContext nodeContext = graphContext.GetNodeContext<RotateTo2DNodeContext>(NodeID);
		nodeContext.ElapsedTime = 0;
		nodeContext.HasTarget = false;

		IForgeEntity? entity = ResolveEntityOrOwner(graphContext);

		if (!ForgeEntityBridge.TryGetSpatialNode2D(entity, _nodePath, out Node2D? spatialNode))
		{
			GD.PushWarning(
				"Statescript: Rotate To 2D found no Node2D for its entity" +
				(_nodePath.Length == 0 ? "." : $" at [{_nodePath}].") +
				" The turn was skipped.");
			return;
		}

		graphContext.TryResolve(InputProperties[RotationInput].BoundName, out double rotation);
		graphContext.TryResolve(InputProperties[ValueInput].BoundName, out double value);

		nodeContext.StartRotation = spatialNode.GlobalRotation;
		nodeContext.DeltaRotation = Mathf.AngleDifference(nodeContext.StartRotation, (float)rotation);
		nodeContext.HasTarget = true;

		nodeContext.Duration = _mode == MoveToMode.Speed
			? ResolveDurationFromSpeed(nodeContext, value)
			: value;

		if (nodeContext.Duration < MinimumDuration)
		{
			// A zero or negative duration is a snap rather than a turn, and it must still report alignment.
			ApplyRotation(spatialNode, nodeContext, 1);
			DeactivateNodeAndEmitMessage(graphContext, OnAlignedPort);
		}
	}

	/// <inheritdoc/>
	protected override void OnDeactivate(GraphContext graphContext)
	{
	}

	/// <inheritdoc/>
	protected override void OnUpdate(double deltaTime, GraphContext graphContext)
	{
		RotateTo2DNodeContext nodeContext = graphContext.GetNodeContext<RotateTo2DNodeContext>(NodeID);

		// A turn that could not start is over rather than pending: nothing sets a target after activation, so holding
		// the node open would stall every graph waiting on OnAligned. Activation already warned about why.
		if (!nodeContext.HasTarget)
		{
			DeactivateNodeAndEmitMessage(graphContext, OnAlignedPort);
			return;
		}

		IForgeEntity? entity = ResolveEntityOrOwner(graphContext);

		// Re-resolved each tick rather than cached: the node may have been freed mid-turn, by a death or a despawn.
		// A turn whose subject has gone is over, for the same reason a turn that could not start is: nothing gives it
		// one back, so waiting would stall every graph whose next step hangs off OnAligned.
		if (!ForgeEntityBridge.TryGetSpatialNode2D(entity, _nodePath, out Node2D? spatialNode))
		{
			DeactivateNodeAndEmitMessage(graphContext, OnAlignedPort);
			return;
		}

		nodeContext.ElapsedTime += deltaTime;

		double progress = nodeContext.ElapsedTime / nodeContext.Duration;

		if (progress >= 1)
		{
			ApplyRotation(spatialNode, nodeContext, 1);
			DeactivateNodeAndEmitMessage(graphContext, OnAlignedPort);
			return;
		}

		ApplyRotation(spatialNode, nodeContext, progress);
	}

	private static double ResolveDurationFromSpeed(RotateTo2DNodeContext nodeContext, double speed)
	{
		if (speed <= 0)
		{
			return 0;
		}

		return Mathf.Abs(nodeContext.DeltaRotation) / speed;
	}

	private static void ApplyRotation(Node2D spatialNode, RotateTo2DNodeContext nodeContext, double progress)
	{
		float eased = Mathf.Clamp((float)progress, 0.0f, 1.0f);
		spatialNode.GlobalRotation = nodeContext.StartRotation + (nodeContext.DeltaRotation * eased);
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
