// Copyright © Gamesmiths Guild.

using System.Collections.Generic;
using Gamesmiths.Forge.Abilities;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Nodes;
using Godot;

namespace Gamesmiths.Forge.Godot.Core.Statescript.Nodes.Action;

/// <summary>
/// Base for action nodes that write something to the 2D node an entity lives on.
/// </summary>
/// <remarks>
/// <para>Declares the entity input every one of them takes, resolves it through the same owner fallback the core
/// ability nodes use, and gets from that entity to a <see cref="Node2D"/> through <see cref="ForgeEntityBridge"/>,
/// honoring an authored child path so a marker node can be written instead of the body.</para>
/// <para>Configuration is captured in field initializers rather than a constructor body, because the base node
/// constructor calls <see cref="DefineParameters"/> before a body would run.</para>
/// </remarks>
/// <param name="nodePath">Optional path to a descendant node to write instead of the entity's own spatial node.</param>
public abstract class SpatialActionNodeBase2D(string nodePath = "") : ActionNode
{
	/// <summary>
	/// Input property index for the entity to act on. Unbound means the ability's owner.
	/// </summary>
	public const byte EntityInput = 0;

	private readonly string _nodePath = nodePath ?? string.Empty;

	private bool _reportedMissingNode;
	private bool _reportedUnusableNode;

	/// <summary>
	/// Adds this node's own input properties. The entity input is already declared.
	/// </summary>
	/// <param name="inputProperties">The input property list to add to.</param>
	/// <param name="outputVariables">The output variable list to add to.</param>
	protected abstract void DefineSpatialParameters(
		List<InputProperty> inputProperties,
		List<OutputVariable> outputVariables);

	/// <summary>
	/// Performs this node's write against the resolved node.
	/// </summary>
	/// <param name="spatialNode">The entity's spatial node, or the authored descendant of it.</param>
	/// <param name="graphContext">The graph execution context, for resolving this node's own inputs.</param>
	protected abstract void ExecuteOn(Node2D spatialNode, GraphContext graphContext);

	/// <summary>
	/// Gets a value indicating whether a marker path that resolves to nothing falls back to the entity's own node.
	/// </summary>
	/// <remarks>
	/// Opt in where the authored path is a route to the subject rather than the subject itself. Physics state lives on
	/// the body and nowhere else, so an entity without the marker still has a right answer; a node that writes a
	/// transform is the opposite case, where substituting the body turns "rotate the turret" into "rotate the tank".
	/// </remarks>
	protected virtual bool FallsBackToEntityNode => false;

	/// <inheritdoc/>
	protected override void DefineParameters(List<InputProperty> inputProperties, List<OutputVariable> outputVariables)
	{
		inputProperties.Add(new InputProperty("Entity", typeof(IForgeEntity), IsOptional: true));
		DefineSpatialParameters(inputProperties, outputVariables);
	}

	/// <inheritdoc/>
	protected sealed override void Execute(GraphContext graphContext)
	{
		IForgeEntity? entity = ResolveEntityOrOwner(graphContext);

		if (ForgeEntityBridge.TryGetSpatialNode2D(entity, _nodePath, out Node2D? spatialNode))
		{
			ExecuteOn(spatialNode, graphContext);
			return;
		}

		if (FallsBackToEntityNode
			&& _nodePath.Length > 0
			&& ForgeEntityBridge.TryGetSpatialNode2D(entity, out spatialNode))
		{
			ReportMissingNodeOnce($"found no Node2D at [{_nodePath}]. Writing to the entity's own node instead.");
			ExecuteOn(spatialNode, graphContext);
			return;
		}

		ReportMissingNodeOnce(
			"found no Node2D for its entity" +
			(_nodePath.Length == 0 ? "." : $" at [{_nodePath}].") +
			" The write was skipped.");
	}

	/// <summary>
	/// Warns once that the resolved node cannot take this node's write, such as a marker where a body is required.
	/// </summary>
	/// <remarks>
	/// Suppressed separately from the missing-node warning: an entity without the marker falls back to its own node and
	/// can then still fail the subclass's type check, and one warning silencing the other would leave that second
	/// failure invisible, which is the whole reason this exists.
	/// </remarks>
	/// <param name="message">What is wrong with the node, completing "Statescript: {node type} ".</param>
	protected void ReportUnusableNodeOnce(string message)
	{
		if (_reportedUnusableNode)
		{
			return;
		}

		_reportedUnusableNode = true;

		GD.PushWarning($"Statescript: {GetType().Name} {message}");
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

	private void ReportMissingNodeOnce(string message)
	{
		if (_reportedMissingNode)
		{
			return;
		}

		_reportedMissingNode = true;

		GD.PushWarning($"Statescript: {GetType().Name} {message}");
	}
}
