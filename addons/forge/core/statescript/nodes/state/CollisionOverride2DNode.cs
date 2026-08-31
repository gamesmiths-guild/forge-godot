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
/// <para>The bits found at activation are what gets restored, so a scene that authored extra layers keeps them, and two
/// overlapping overrides of the same field resolve to whichever ends last rather than to a value neither of them
/// intended.</para>
/// </remarks>
/// <param name="target">Which of the two bit fields to change.</param>
/// <param name="operation">Whether the given bits are turned on or off for the duration.</param>
/// <param name="nodePath">Optional path to a descendant node to change instead of the entity's own spatial node.
/// </param>
[StatescriptCategory("Physics")]
public class CollisionOverride2DNode(
	CollisionBitsTarget target = CollisionBitsTarget.Layer,
	CollisionBitsOperation operation = CollisionBitsOperation.Clear,
	string nodePath = "") : StateNode<CollisionOverride2DNodeContext>
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
		CollisionOverride2DNodeContext nodeContext =
			graphContext.GetNodeContext<CollisionOverride2DNodeContext>(NodeID);
		nodeContext.Body = null;

		IForgeEntity? entity = ResolveEntityOrOwner(graphContext);

		if (!ForgeEntityBridge.TryGetSpatialNode2D(entity, _nodePath, out Node2D? spatialNode)
			|| spatialNode is not CollisionObject2D body)
		{
			GD.PushWarning(
				"Statescript: Collision Override 2D found no CollisionObject2D for its entity" +
				(_nodePath.Length == 0 ? "." : $" at [{_nodePath}].") +
				" The override was skipped.");
			return;
		}

		graphContext.TryResolve(InputProperties[BitsInput].BoundName, out int bits);

		uint original = CollisionBits2D.Read(body, _target);

		nodeContext.Body = body;
		nodeContext.OriginalBits = original;

		CollisionBits2D.Write(
			body,
			_target,
			CollisionBits2D.Apply(original, unchecked((uint)bits), _operation));
	}

	/// <inheritdoc/>
	protected override void OnDeactivate(GraphContext graphContext)
	{
		CollisionOverride2DNodeContext nodeContext =
			graphContext.GetNodeContext<CollisionOverride2DNodeContext>(NodeID);
		CollisionObject2D? body = nodeContext.Body;
		nodeContext.Body = null;

		if (body is not null && GodotObject.IsInstanceValid(body))
		{
			CollisionBits2D.Write(body, _target, nodeContext.OriginalBits);
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
