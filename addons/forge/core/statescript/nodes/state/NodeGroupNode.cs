// Copyright © Gamesmiths Guild.

using System.Collections.Generic;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Nodes;
using Godot;
using Node = Godot.Node;

namespace Gamesmiths.Forge.Godot.Core.Statescript.Nodes.State;

/// <summary>
/// State node that keeps a node in a Godot group while active and takes it out when it ends.
/// </summary>
/// <remarks>
/// <para>The held counterpart of Add To Node Group, and what every other write in the layer has: a state that lasts
/// exactly as long as the ability rather than a pair of writes the graph has to remember on each exit. A taunt that
/// makes its caster the group every enemy retargets, a mark that only counts while the debuff runs, a summon that
/// belongs to the squad until it is dismissed. Every deactivation path takes it back out, including an abort - a
/// cancelled taunt must not leave the caster permanently marked.</para>
/// <para><b>Only a membership this node added is one it removes.</b> A node the level already put in the group is left
/// in it, because the membership was never this ability's to take away - the same reasoning the override nodes use for
/// restoring the value they found rather than the one they assume.</para>
/// <para>The consequence is that the membership lasts exactly as long as the hold that <em>added</em> it. A second
/// hold over the same node and group is a no-op in both directions - it adds nothing on activation and removes
/// nothing on deactivation - so the node leaves the group when the adding hold ends, whether that is before or after
/// the other one. This is not a reference count, and it is not the "whichever ends last" the override nodes give. A
/// group is a set, so the second hold has nothing to add and nothing to restore; if two abilities need to mark the
/// same node independently, give them a group each.</para>
/// <para>The membership is not persistent, so it belongs to the running game and is never saved into the scene.</para>
/// </remarks>
/// <param name="group">The group to hold the node in.</param>
[StatescriptCategory("Scene")]
public class NodeGroupNode(string group = "") : StateNode<NodeGroupNodeContext>
{
	/// <summary>
	/// Input property index for the node to hold in the group.
	/// </summary>
	public const byte NodeInput = 0;

	private readonly StringName _group = group;

	/// <inheritdoc/>
	public override string Description =>
		"Keeps a node in a Godot group while active, taking it out on deactivation or abort.";

	/// <inheritdoc/>
	protected override void DefineParameters(List<InputProperty> inputProperties, List<OutputVariable> outputVariables)
	{
		inputProperties.Add(new InputProperty("Node", typeof(Node)));
	}

	/// <inheritdoc/>
	protected override void OnActivate(GraphContext graphContext)
	{
		NodeGroupNodeContext nodeContext = graphContext.GetNodeContext<NodeGroupNodeContext>(NodeID);
		nodeContext.Node = null;

		if (_group.IsEmpty
			|| !graphContext.TryResolveObject(InputProperties[NodeInput].BoundName, out Node? node)
			|| node is null
			|| !GodotObject.IsInstanceValid(node)
			|| node.IsInGroup(_group))
		{
			return;
		}

		nodeContext.Node = node;
		node.AddToGroup(_group);
	}

	/// <inheritdoc/>
	protected override void OnDeactivate(GraphContext graphContext)
	{
		NodeGroupNodeContext nodeContext = graphContext.GetNodeContext<NodeGroupNodeContext>(NodeID);
		Node? node = nodeContext.Node;
		nodeContext.Node = null;

		if (node is not null && GodotObject.IsInstanceValid(node))
		{
			node.RemoveFromGroup(_group);
		}
	}
}
