// Copyright © Gamesmiths Guild.

using System.Collections.Generic;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Nodes;
using Godot;
using Node = Godot.Node;

namespace Gamesmiths.Forge.Godot.Core.Statescript.Nodes.Action;

/// <summary>
/// Action node that puts a node into a Godot group.
/// </summary>
/// <remarks>
/// <para>The write half of the group resolvers, and what makes a group a set a graph maintains rather than only one a
/// level author fills in: an ability that marks its victims so a later one can find them all, a summon joining the
/// squad it was called into, a trap arming itself into the list of live traps.</para>
/// <para>The membership is not persistent, so it belongs to the running game and is never saved back into the scene.
/// A node already in the group is left alone rather than reported - Godot treats a group as a set, and a graph
/// reaching this twice is ordinary rather than a mistake.</para>
/// </remarks>
/// <param name="group">The group to add to.</param>
[StatescriptCategory("Scene")]
public sealed class AddToNodeGroupNode(string group = "") : ActionNode
{
	/// <summary>
	/// Input property index for the node to add.
	/// </summary>
	public const byte NodeInput = 0;

	private readonly StringName _group = group;

	/// <inheritdoc/>
	public override string Description => "Puts a node into a Godot group.";

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
			node.AddToGroup(_group);
		}
	}
}
