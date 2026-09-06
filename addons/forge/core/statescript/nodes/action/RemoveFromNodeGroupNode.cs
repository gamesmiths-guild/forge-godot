// Copyright © Gamesmiths Guild.

using System.Collections.Generic;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Nodes;
using Godot;
using Node = Godot.Node;

namespace Gamesmiths.Forge.Godot.Core.Statescript.Nodes.Action;

/// <summary>
/// Action node that takes a node out of a Godot group.
/// </summary>
/// <remarks>
/// The other half of Add To Node Group, and the one that keeps a graph-maintained set honest: a mark that wears off, a
/// summon leaving the squad when it is dismissed, a trap that has been sprung dropping out of the live list. A node
/// that is not in the group is left alone rather than reported, which is what lets a cleanup path run without first
/// asking whether it needs to.
/// </remarks>
/// <param name="group">The group to remove from.</param>
[StatescriptCategory("Scene")]
public sealed class RemoveFromNodeGroupNode(string group = "") : ActionNode
{
	/// <summary>
	/// Input property index for the node to remove.
	/// </summary>
	public const byte NodeInput = 0;

	private readonly StringName _group = group;

	/// <inheritdoc/>
	public override string Description => "Takes a node out of a Godot group.";

	/// <inheritdoc/>
	protected override void DefineParameters(List<InputProperty> inputProperties, List<OutputVariable> outputVariables)
	{
		inputProperties.Add(new InputProperty("Node", typeof(Node)));
	}

	/// <inheritdoc/>
	protected override void Execute(GraphContext graphContext)
	{
		if (!_group.IsEmpty
			&& graphContext.TryResolveObject(InputProperties[NodeInput].BoundName, out Node? node)
			&& node is not null
			&& GodotObject.IsInstanceValid(node))
		{
			node.RemoveFromGroup(_group);
		}
	}
}
