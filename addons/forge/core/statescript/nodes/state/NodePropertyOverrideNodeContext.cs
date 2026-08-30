// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Statescript.Nodes;
using Godot;
using Node = Godot.Node;

namespace Gamesmiths.Forge.Godot.Core.Statescript.Nodes.State;

/// <summary>
/// The context for a <see cref="NodePropertyOverrideNode"/>. Holds the node it wrote to and the value it found there,
/// so deactivation restores what the scene had rather than what the node assumes.
/// </summary>
/// <remarks>
/// The original is kept as the raw Godot value the property gave back, not as a graph value converted to the authored
/// type and back. A round trip through the value lane would quantise a float, drop a colour, or turn an unset
/// reference into something else - and none of that belongs in a restore whose whole job is to leave no trace.
/// </remarks>
public class NodePropertyOverrideNodeContext : StateNodeContext
{
	/// <summary>
	/// Gets or sets the node the override was applied to.
	/// </summary>
	public Node? Node { get; set; }

	/// <summary>
	/// Gets or sets the property value as it was before the override.
	/// </summary>
	public Variant OriginalValue { get; set; }
}
