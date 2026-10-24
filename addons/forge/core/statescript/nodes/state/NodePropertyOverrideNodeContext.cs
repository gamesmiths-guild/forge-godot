// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Statescript.Nodes;
using Godot;
using Node = Godot.Node;

namespace Gamesmiths.Forge.Godot.Core.Statescript.Nodes.State;

/// <summary>
/// The context for a <see cref="NodePropertyOverrideNode"/>. Holds the node and the value the activation resolved, for
/// the presentation pass to write.
/// </summary>
/// <remarks>
/// They are resolved once, when the node activates, because resolving them in the pass could draw from the graph's
/// random, which presentation must not touch.
/// </remarks>
public class NodePropertyOverrideNodeContext : StateNodeContext
{
	/// <summary>
	/// Gets or sets the node the override acts on, or <see langword="null"/> when there was nothing to override.
	/// </summary>
	public Node? Node { get; set; }

	/// <summary>
	/// Gets or sets the value to hold on the property.
	/// </summary>
	public Variant Value { get; set; }
}
