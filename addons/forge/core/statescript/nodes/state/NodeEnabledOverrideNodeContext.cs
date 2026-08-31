// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Statescript.Nodes;
using Node = Godot.Node;

namespace Gamesmiths.Forge.Godot.Core.Statescript.Nodes.State;

/// <summary>
/// The context for a <see cref="NodeEnabledOverrideNode"/>. Holds the node it changed and the setting it found there,
/// so deactivation restores what the scene had rather than what the node assumes.
/// </summary>
/// <remarks>
/// The node is captured rather than re-resolved on deactivate: the node input may have moved on, and putting the old
/// setting back on a different node than it came from is worse than not restoring it at all.
/// </remarks>
public class NodeEnabledOverrideNodeContext : StateNodeContext
{
	/// <summary>
	/// Gets or sets the node the override was applied to.
	/// </summary>
	public Node? Node { get; set; }

	/// <summary>
	/// Gets or sets a value indicating whether the setting was enabled before the override.
	/// </summary>
	public bool OriginalEnabled { get; set; }
}
