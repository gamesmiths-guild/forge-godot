// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Statescript.Nodes;

namespace Gamesmiths.Forge.Godot.Core.Statescript.Nodes.State;

/// <summary>
/// The context for an <see cref="InputActionNode"/>. Remembers whether the button was down, which is what turns a
/// per-tick read into the presses and releases the node reports.
/// </summary>
public class InputActionNodeContext : StateNodeContext
{
	/// <summary>
	/// Gets or sets whether the button was down on the last read. Null before the first one, which is what keeps a
	/// button already held when the node activated from counting as a press.
	/// </summary>
	public bool? LastPressed { get; set; }
}
