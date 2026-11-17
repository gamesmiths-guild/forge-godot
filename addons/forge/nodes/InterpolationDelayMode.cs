// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Godot.Nodes;

/// <summary>
/// How a client picks how far behind the newest state it holds it shows the server's entities.
/// </summary>
public enum InterpolationDelayMode
{
	/// <summary>
	/// A fixed delay, the session's Interpolation Delay Ms.
	/// </summary>
	Fixed = 0,

	/// <summary>
	/// A delay worked out from how the state packets arrive: one send interval on a steady link, more for jitter and
	/// loss, within the session's bounds.
	/// </summary>
	Adaptive = 1,
}
