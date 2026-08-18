// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Godot.Core.Statescript.Nodes.State;

/// <summary>
/// How a Move To node interprets its value input.
/// </summary>
public enum MoveToMode
{
	/// <summary>
	/// The value is how long the move takes, in seconds, whatever the distance. Use this when the timing has to line up
	/// with an animation or a cue.
	/// </summary>
	Duration = 0,

	/// <summary>
	/// The value is how fast to travel, in units per second, so the duration follows from the distance. Use this when
	/// the speed is what should feel consistent.
	/// </summary>
	Speed = 1,
}
