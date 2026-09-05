// Copyright © Gamesmiths Guild.

using System.Collections.Generic;
using Gamesmiths.Forge.Abilities;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Nodes;
using Gamesmiths.Forge.Statescript.Ports;
using Godot;
using NumericsQuaternion = System.Numerics.Quaternion;

namespace Gamesmiths.Forge.Godot.Core.Statescript.Nodes.State;

/// <summary>
/// State node that turns an entity to a rotation over time, emitting <see cref="OnAlignedPort"/> when it gets there.
/// </summary>
/// <remarks>
/// <para>The rotation counterpart of Move To 3D, and the node for a turn that has to be seen: a caster winding up to
/// face their target, a turret coming round onto a lead. Setting a rotation outright is Set Rotation 3D; this is what
/// makes the same turn take time, which is what a graph gates a cast on.</para>
/// <para>The turn is a slerp along the shortest arc between the two rotations, so a target a hair off the current
/// facing takes the short way round rather than the long one. It writes the rotation only, leaving scale and position
/// alone, so it composes with a Move To running on the same entity.</para>
/// <para>Aborting stops the turn where it stands, the same as Move To: an interrupted wind-up leaves the caster part
/// way round, and whatever follows the abort decides what happens next.</para>
/// <para>A turn that cannot start - no node to turn, or no rotation authored - warns and reports alignment on its
/// first update rather than holding the node open, since nothing gives the node a target after activation.</para>
/// <para>Configuration is captured in field initializers rather than a constructor body, because the base node
/// constructor calls <see cref="DefinePorts"/> and <see cref="DefineParameters"/> before a body would run.</para>
/// </remarks>
/// <param name="mode">Whether the value input is a duration or an angular speed.</param>
/// <param name="nodePath">Optional path to a descendant node to turn instead of the entity's own spatial node.</param>
[StatescriptCategory("Spatial")]
public class RotateTo3DNode(MoveToMode mode = MoveToMode.Duration, string nodePath = "")
	: StateNode<RotateTo3DNodeContext>
{
	/// <summary>
	/// Input property index for the entity to turn. Unbound means the ability's owner.
	/// </summary>
	public const byte EntityInput = 0;

	/// <summary>
	/// Input property index for the rotation to turn to.
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
		"Turns an entity to a rotation over time, emitting OnAligned on completion.";

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
		inputProperties.Add(new InputProperty("Rotation", typeof(NumericsQuaternion)));
		inputProperties.Add(new InputProperty("Value", typeof(double)));
	}

	/// <inheritdoc/>
	protected override void OnActivate(GraphContext graphContext)
	{
		RotateTo3DNodeContext nodeContext = graphContext.GetNodeContext<RotateTo3DNodeContext>(NodeID);
		nodeContext.ElapsedTime = 0;
		nodeContext.HasTarget = false;

		IForgeEntity? entity = ResolveEntityOrOwner(graphContext);

		if (!ForgeEntityBridge.TryGetSpatialNode3D(entity, _nodePath, out Node3D? spatialNode))
		{
			GD.PushWarning(
				"Statescript: Rotate To 3D found no Node3D for its entity" +
				(_nodePath.Length == 0 ? "." : $" at [{_nodePath}].") +
				" The turn was skipped.");
			return;
		}

		graphContext.TryResolve(InputProperties[RotationInput].BoundName, out NumericsQuaternion rotation);

		var target = new Quaternion(rotation.X, rotation.Y, rotation.Z, rotation.W);

		// A zero quaternion is what an unfilled rotation operand resolves to, and it is not a rotation Godot can
		// normalize or slerp towards. Identity is (0, 0, 0, 1), so nothing an author meant is lost by rejecting it.
		if (target.LengthSquared() <= 0.000001f)
		{
			GD.PushWarning("Statescript: Rotate To 3D could not resolve a rotation. The turn was skipped.");
			return;
		}

		graphContext.TryResolve(InputProperties[ValueInput].BoundName, out double value);

		nodeContext.StartRotation = spatialNode.GlobalBasis.GetRotationQuaternion();
		nodeContext.TargetRotation = target.Normalized();
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
	protected override void OnFixedUpdate(double deltaTime, GraphContext graphContext)
	{
		RotateTo3DNodeContext nodeContext = graphContext.GetNodeContext<RotateTo3DNodeContext>(NodeID);

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
		if (!ForgeEntityBridge.TryGetSpatialNode3D(entity, _nodePath, out Node3D? spatialNode))
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

	private static double ResolveDurationFromSpeed(RotateTo3DNodeContext nodeContext, double speed)
	{
		if (speed <= 0)
		{
			return 0;
		}

		return nodeContext.StartRotation.AngleTo(nodeContext.TargetRotation) / speed;
	}

	private static void ApplyRotation(Node3D spatialNode, RotateTo3DNodeContext nodeContext, double progress)
	{
		Quaternion rotation = nodeContext.StartRotation.Slerp(
			nodeContext.TargetRotation,
			Mathf.Clamp((float)progress, 0.0f, 1.0f));

		// Scale is carried separately so a scaled node keeps its scale, and applied in the new rotation's own axes,
		// matching Set Rotation 3D - scaling in parent axes would shear a non-uniformly scaled node mid-turn.
		Vector3 scale = spatialNode.GlobalBasis.Scale;
		spatialNode.GlobalBasis = new Basis(rotation).ScaledLocal(scale);
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
