// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Statescript.Nodes;
using Godot;
using Node = Godot.Node;

namespace Gamesmiths.Forge.Godot.Core.Statescript.Nodes.State;

/// <summary>
/// The context for a <see cref="SignalListenerNode"/>. Remembers what was connected, which is the only way to take the
/// connection back down again: the node it was made on can be reachable through a different resolver result by the time
/// the listener deactivates.
/// </summary>
public class SignalListenerNodeContext : StateNodeContext
{
	/// <summary>
	/// Gets or sets the node the signal was connected on, or <see langword="null"/> when nothing is connected.
	/// </summary>
	public Node? ConnectedNode { get; set; }

	/// <summary>
	/// Gets or sets the callable the signal was connected to.
	/// </summary>
	public Callable ConnectedCallable { get; set; }
}
