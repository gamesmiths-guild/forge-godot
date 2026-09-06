// Copyright © Gamesmiths Guild.

using System.Collections.Generic;
using Gamesmiths.Forge.Abilities;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Nodes;
using Godot;
using NumericsVector3 = System.Numerics.Vector3;

namespace Gamesmiths.Forge.Godot.Core.Statescript.Nodes.State;

/// <summary>
/// State node that keeps an entity facing a point for as long as it is active.
/// </summary>
/// <remarks>
/// <para>The third reading of a turn, and the one a moving target needs. Set Rotation Toward 3D faces a point at the
/// instant it runs; Rotate To 3D turns to a rotation captured when it started; both aim at where something
/// <em>was</em>. This re-resolves its target every update, so binding Target to an Entity Position 3D of whoever is
/// being followed keeps the caster pointed at them as they move — a turret holding a lead, a channelled beam that
/// follows, a boss keeping the player in front of it.</para>
/// <para><b>The turn rate is a ceiling, not a rate to be met.</b> Unbound, zero or negative snaps to the target every
/// update, which is the honest reading of "no limit on how fast it may turn". A rate makes the facing lag a target
/// that jinks, and that lag is what makes a tracking attack dodgeable — which is usually the whole point of authoring
/// one.</para>
/// <para>Flattening is on by default for the reason Set Rotation Toward 3D has it on: the usual intent is "turn
/// towards them", and a character that pitches at a target's feet reads as a bug. Turn it off for something that
/// genuinely aims in three dimensions.</para>
/// <para>There is no aligned event. Whether the facing has arrived is a question the layer already answers — Is In
/// Cone 3D over the caster's forward and the target — and an event here would be a second answer that could disagree
/// with it. A graph that must wait for the turn gates on that condition.</para>
/// <para>Nothing is restored on deactivate, unlike the override nodes. A facing is not a property borrowed from the
/// scene: where a thing ended up looking is where it is looking, and snapping it back would undo a turn the player
/// watched happen.</para>
/// <para>Configuration is captured in field initializers rather than a constructor body, because the base node
/// constructor calls <see cref="DefineParameters"/> before a body would run.</para>
/// </remarks>
/// <param name="flatten">Whether to ignore the height difference and turn only around the vertical axis.</param>
/// <param name="nodePath">Optional path to a descendant node to turn instead of the entity's own spatial node.</param>
[StatescriptCategory("Spatial")]
public class LookAt3DNode(bool flatten = true, string nodePath = "") : StateNode<StateNodeContext>
{
	/// <summary>
	/// Input property index for the entity to turn. Unbound means the ability's owner.
	/// </summary>
	public const byte EntityInput = 0;

	/// <summary>
	/// Input property index for the point to keep facing.
	/// </summary>
	public const byte TargetInput = 1;

	/// <summary>
	/// Input property index for the most the facing may turn in a second, in radians. Unbound snaps.
	/// </summary>
	public const byte SpeedInput = 2;

	private const float MinimumOffsetSquared = 0.000001f;

	// Above this the offset is close enough to the up axis that a look-at has no perpendicular left to build from.
	private const float ParallelToUpDot = 0.9999f;

	private readonly bool _flatten = flatten;
	private readonly string _nodePath = nodePath ?? string.Empty;

	private bool _reportedMissingNode;

	/// <inheritdoc/>
	public override string Description => "Keeps an entity facing a point for as long as it is active.";

	/// <inheritdoc/>
	protected override void DefineParameters(List<InputProperty> inputProperties, List<OutputVariable> outputVariables)
	{
		inputProperties.Add(new InputProperty("Entity", typeof(IForgeEntity), IsOptional: true));
		inputProperties.Add(new InputProperty("Target", typeof(NumericsVector3)));
		inputProperties.Add(new InputProperty("Speed", typeof(double), IsOptional: true));
	}

	/// <inheritdoc/>
	protected override void OnActivate(GraphContext graphContext)
	{
	}

	/// <inheritdoc/>
	protected override void OnDeactivate(GraphContext graphContext)
	{
	}

	/// <inheritdoc/>
	protected override void OnFixedUpdate(double deltaTime, GraphContext graphContext)
	{
		IForgeEntity? entity = ResolveEntityOrOwner(graphContext);

		// Re-resolved every update rather than captured at activation, because a tracker runs for as long as its
		// ability does and the node it turns can be freed under it by a death or a despawn.
		if (!ForgeEntityBridge.TryGetSpatialNode3D(entity, _nodePath, out Node3D? spatialNode))
		{
			ReportMissingNodeOnce();
			return;
		}

		if (!graphContext.TryResolve(InputProperties[TargetInput].BoundName, out NumericsVector3 target))
		{
			return;
		}

		var point = new Vector3(target.X, target.Y, target.Z);

		if (_flatten)
		{
			point.Y = spatialNode.GlobalPosition.Y;
		}

		Vector3 offset = point - spatialNode.GlobalPosition;

		// The two offsets a look-at cannot be built from: a target standing exactly on the node names no direction,
		// and one directly overhead leaves the up axis with nothing to be perpendicular to. Both happen in play, and
		// both mean "keep the facing you have" rather than "turn somewhere arbitrary".
		if (offset.LengthSquared() <= MinimumOffsetSquared
			|| Mathf.Abs(offset.Normalized().Dot(Vector3.Up)) > ParallelToUpDot)
		{
			return;
		}

		graphContext.TryResolve(InputProperties[SpeedInput].BoundName, out double speed);

		Quaternion current = spatialNode.GlobalBasis.GetRotationQuaternion();
		Quaternion desired = Basis.LookingAt(offset, Vector3.Up).GetRotationQuaternion();

		// Scale is carried separately so a scaled node keeps its scale, and applied in the new rotation's own axes,
		// matching Rotate To 3D - scaling in parent axes would shear a non-uniformly scaled node as it turns.
		Vector3 scale = spatialNode.GlobalBasis.Scale;
		Quaternion rotation = StepToward(current, desired, speed * deltaTime);
		spatialNode.GlobalBasis = new Basis(rotation).ScaledLocal(scale);
	}

	// The step is a ceiling: one longer than the turn that is left lands on the target rather than overshooting it and
	// swinging back, which is what a per-frame step would otherwise do as the facing closes in.
	private static Quaternion StepToward(Quaternion current, Quaternion desired, double maxStep)
	{
		float angle = current.AngleTo(desired);

		if (maxStep <= 0 || angle <= maxStep)
		{
			return desired;
		}

		return current.Slerp(desired, (float)(maxStep / angle));
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

	private void ReportMissingNodeOnce()
	{
		if (_reportedMissingNode)
		{
			return;
		}

		_reportedMissingNode = true;

		GD.PushWarning(
			"Statescript: Look At 3D found no Node3D for its entity" +
			(_nodePath.Length == 0 ? "." : $" at [{_nodePath}].") +
			" Nothing was turned.");
	}
}
