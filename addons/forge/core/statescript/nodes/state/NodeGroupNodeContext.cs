// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Statescript.Nodes;
using Node = Godot.Node;

namespace Gamesmiths.Forge.Godot.Core.Statescript.Nodes.State;

/// <summary>
/// The context for a <see cref="NodeGroupNode"/>. Holds the node it added, which is what tells a membership this
/// node is responsible for from one it merely found.
/// </summary>
/// <remarks>
/// The node is captured rather than re-resolved on deactivate: the node input may have moved on, and taking a
/// different node out of the group than the one that was put in is worse than not tidying up at all. It stays null
/// when the node was already a member, which is how a membership the level authored survives the ability ending.
/// </remarks>
public class NodeGroupNodeContext : StateNodeContext
{
	/// <summary>
	/// Gets or sets the node this activation added to the group, or <see langword="null"/> when it added none.
	/// </summary>
	public Node? Node { get; set; }
}
