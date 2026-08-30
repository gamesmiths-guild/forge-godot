// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Godot.Core.Statescript.Physics;

/// <summary>
/// Which of a collision object's two bit fields a node reads or writes.
/// </summary>
public enum CollisionBitsTarget
{
	/// <summary>
	/// The layers the body occupies: what other bodies can find it. Clearing these is how a dash passes through
	/// enemies while still colliding with the world.
	/// </summary>
	Layer = 0,

	/// <summary>
	/// The layers the body scans: what it collides with. Clearing these is how a body stops being blocked without
	/// becoming invisible to everyone else.
	/// </summary>
	Mask = 1,
}
