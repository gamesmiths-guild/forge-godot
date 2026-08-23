// Copyright © Gamesmiths Guild.

using System.Collections.Generic;
using Gamesmiths.Forge.Abilities;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Nodes;
using Godot;

namespace Gamesmiths.Forge.Godot.Core.Statescript.Nodes.Action;

/// <summary>
/// Base for action nodes that write something to the 3D node an entity lives on.
/// </summary>
/// <remarks>
/// <para>Declares the entity input every one of them takes, resolves it through the same owner fallback the core
/// ability nodes use, and gets from that entity to a <see cref="Node3D"/> through <see cref="ForgeEntityBridge"/>,
/// honoring an authored child path so a marker node can be written instead of the body.</para>
/// <para>Configuration is captured in field initializers rather than a constructor body, because the base node
/// constructor calls <see cref="DefineParameters"/> before a body would run.</para>
/// </remarks>
/// <param name="nodePath">Optional path to a descendant node to write instead of the entity's own spatial node.</param>
public abstract class SpatialActionNodeBase3D(string nodePath = "") : ActionNode
{
	/// <summary>
	/// Input property index for the entity to act on. Unbound means the ability's owner.
	/// </summary>
	public const byte EntityInput = 0;

	private readonly string _nodePath = nodePath ?? string.Empty;

	private bool _reportedMissingNode;

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
	protected abstract void ExecuteOn(Node3D spatialNode, GraphContext graphContext);

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

		if (!ForgeEntityBridge.TryGetSpatialNode3D(entity, _nodePath, out Node3D? spatialNode))
		{
			ReportMissingNodeOnce();
			return;
		}

		ExecuteOn(spatialNode, graphContext);
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
			$"Statescript: {GetType().Name} found no Node3D for its entity" +
			(_nodePath.Length == 0 ? "." : $" at [{_nodePath}].") +
			" The write was skipped.");
	}
}
