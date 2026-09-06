// Copyright © Gamesmiths Guild.

using System.Collections.Generic;
using Gamesmiths.Forge.Abilities;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Nodes;
using Godot;
using NumericsVector2 = System.Numerics.Vector2;

namespace Gamesmiths.Forge.Godot.Core.Statescript.Nodes.State;

/// <summary>
/// State node that keeps an entity facing a point for as long as it is active.
/// </summary>
/// <remarks>
/// <para>The third reading of a turn, and the one a moving target needs. Set Rotation Toward 2D faces a point at the
/// instant it runs; Rotate To 2D turns to a rotation captured when it started; both aim at where something
/// <em>was</em>. This re-resolves its target every update, so binding Target to an Entity Position 2D of whoever is
/// being followed keeps the caster pointed at them as they move — a turret holding a lead, a channelled beam that
/// follows, an enemy that keeps the player in front of it.</para>
/// <para><b>The turn rate is a ceiling, not a rate to be met.</b> Unbound, zero or negative snaps to the target every
/// update, which is the honest reading of "no limit on how fast it may turn". A rate makes the facing lag a target
/// that jinks, and that lag is what makes a tracking attack dodgeable — which is usually the whole point of authoring
/// one.</para>
/// <para>Unlike its 3D twin there is nothing to flatten: a plane has one axis to turn around, so this is the turn the
/// 3D node has to be told to restrict itself to. The node faces the point with its +X axis, which is what Godot
/// treats as a 2D node's forward, so the offset's own angle is the facing directly.</para>
/// <para>There is no aligned event. Whether the facing has arrived is a question the layer already answers — Is In
/// Cone 2D over the caster's forward and the target — and an event here would be a second answer that could disagree
/// with it. A graph that must wait for the turn gates on that condition.</para>
/// <para>Nothing is restored on deactivate, unlike the override nodes. A facing is not a property borrowed from the
/// scene: where a thing ended up looking is where it is looking, and snapping it back would undo a turn the player
/// watched happen.</para>
/// <para>Configuration is captured in field initializers rather than a constructor body, because the base node
/// constructor calls <see cref="DefineParameters"/> before a body would run.</para>
/// </remarks>
/// <param name="nodePath">Optional path to a descendant node to turn instead of the entity's own spatial node.</param>
[StatescriptCategory("Spatial")]
public class LookAt2DNode(string nodePath = "") : StateNode<StateNodeContext>
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

	private readonly string _nodePath = nodePath ?? string.Empty;

	private bool _reportedMissingNode;

	/// <inheritdoc/>
	public override string Description => "Keeps an entity facing a point for as long as it is active.";

	/// <inheritdoc/>
	protected override void DefineParameters(List<InputProperty> inputProperties, List<OutputVariable> outputVariables)
	{
		inputProperties.Add(new InputProperty("Entity", typeof(IForgeEntity), IsOptional: true));
		inputProperties.Add(new InputProperty("Target", typeof(NumericsVector2)));
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
		if (!ForgeEntityBridge.TryGetSpatialNode2D(entity, _nodePath, out Node2D? spatialNode))
		{
			ReportMissingNodeOnce();
			return;
		}

		if (!graphContext.TryResolve(InputProperties[TargetInput].BoundName, out NumericsVector2 target))
		{
			return;
		}

		Vector2 offset = new Vector2(target.X, target.Y) - spatialNode.GlobalPosition;

		// A target standing exactly on the node names no direction, and turning to the angle a zero vector rounds to
		// would snap the facing somewhere arbitrary. Keeping the facing it has is the reading that matches.
		if (offset.LengthSquared() <= MinimumOffsetSquared)
		{
			return;
		}

		graphContext.TryResolve(InputProperties[SpeedInput].BoundName, out double speed);

		float current = spatialNode.GlobalRotation;
		float desired = offset.Angle();
		float maxStep = (float)(speed * deltaTime);

		// Stepped through the signed difference rather than towards the absolute angle, for the reason Rotate To 2D
		// takes its delta once: a facing accumulates past a full turn, and comparing absolutes would send a node that
		// has spun twice the long way round to a target directly in front of it.
		spatialNode.GlobalRotation = maxStep <= 0
			? desired
			: current + Mathf.Clamp(Mathf.AngleDifference(current, desired), -maxStep, maxStep);
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
			"Statescript: Look At 2D found no Node2D for its entity" +
			(_nodePath.Length == 0 ? "." : $" at [{_nodePath}].") +
			" Nothing was turned.");
	}
}
