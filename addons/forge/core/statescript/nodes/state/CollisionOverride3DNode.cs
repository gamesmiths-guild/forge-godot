// Copyright © Gamesmiths Guild.

using System.Collections.Generic;
using Gamesmiths.Forge.Abilities;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Godot.Core.Statescript.Physics;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Nodes;
using Godot;

namespace Gamesmiths.Forge.Godot.Core.Statescript.Nodes.State;

/// <summary>
/// State node that changes collision layer or mask bits while active and puts them back when it ends.
/// </summary>
/// <remarks>
/// <para>Every deactivation path restores the original bits, including an abort. That is the whole reason this node
/// exists rather than a pair of writes the graph has to remember on each exit: a dash that is cancelled mid-flight by a
/// stun must not leave the character permanently intangible.</para>
/// <para>Only the bits this node acted on are put back, at the values they had when it activated. Everything else in
/// the field is left as it currently stands, so a second override on different bits of the same field, or a permanent
/// write made while this one was running, survives it rather than being reverted by a stale snapshot. Two overrides on
/// the *same* bits still resolve to whichever ends last, since neither of them can know what the other found.</para>
/// </remarks>
/// <param name="target">Which of the two bit fields to change.</param>
/// <param name="operation">Whether the given bits are turned on or off for the duration.</param>
/// <param name="nodePath">Optional path to a descendant node to change instead of the entity's own spatial node.
/// </param>
[StatescriptCategory("Physics")]
public class CollisionOverride3DNode(
	CollisionBitsTarget target = CollisionBitsTarget.Layer,
	CollisionBitsOperation operation = CollisionBitsOperation.Clear,
	string nodePath = "") : StateNode<CollisionOverride3DNodeContext>
{
	/// <summary>
	/// Input property index for the entity to change. Unbound means the ability's owner.
	/// </summary>
	public const byte EntityInput = 0;

	/// <summary>
	/// Input property index for the bits to act on.
	/// </summary>
	public const byte BitsInput = 1;

	private readonly CollisionBitsTarget _target = target;
	private readonly CollisionBitsOperation _operation = operation;
	private readonly string _nodePath = nodePath ?? string.Empty;

	/// <inheritdoc/>
	public override string Description =>
		"Changes collision layer or mask bits while active, restoring them on deactivation or abort.";

	/// <inheritdoc/>
	protected override void DefineParameters(List<InputProperty> inputProperties, List<OutputVariable> outputVariables)
	{
		inputProperties.Add(new InputProperty("Entity", typeof(IForgeEntity), IsOptional: true));
		inputProperties.Add(new InputProperty("Bits", typeof(int)));
	}

	/// <inheritdoc/>
	protected override void OnActivate(GraphContext graphContext)
	{
		CollisionOverride3DNodeContext nodeContext =
			graphContext.GetNodeContext<CollisionOverride3DNodeContext>(NodeID);
		nodeContext.Body = null;

		IForgeEntity? entity = ResolveEntityOrOwner(graphContext);

		if (!ForgeEntityBridge.TryGetSpatialNode3D(entity, _nodePath, out Node3D? spatialNode)
			|| spatialNode is not CollisionObject3D body)
		{
			GD.PushWarning(
				"Statescript: Collision Override 3D found no CollisionObject3D for its entity" +
				(_nodePath.Length == 0 ? "." : $" at [{_nodePath}].") +
				" The override was skipped.");
			return;
		}

		graphContext.TryResolve(InputProperties[BitsInput].BoundName, out int bits);

		uint original = CollisionBits3D.Read(body, _target);
		uint overridden = unchecked((uint)bits);

		nodeContext.Body = body;
		nodeContext.OriginalBits = original;
		nodeContext.OverriddenBits = overridden;

		CollisionBits3D.Write(body, _target, CollisionBits3D.Apply(original, overridden, _operation));
	}

	/// <inheritdoc/>
	protected override void OnDeactivate(GraphContext graphContext)
	{
		CollisionOverride3DNodeContext nodeContext =
			graphContext.GetNodeContext<CollisionOverride3DNodeContext>(NodeID);
		CollisionObject3D? body = nodeContext.Body;
		nodeContext.Body = null;

		if (body is not null && GodotObject.IsInstanceValid(body))
		{
			CollisionBits3D.Write(
				body,
				_target,
				CollisionBits3D.Restore(
					CollisionBits3D.Read(body, _target),
					nodeContext.OriginalBits,
					nodeContext.OverriddenBits));
		}
	}

	/// <inheritdoc/>
	protected override void OnUpdate(double deltaTime, GraphContext graphContext)
	{
		// Nothing to do per tick: the override is applied once on activation and undone once on deactivation.
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
