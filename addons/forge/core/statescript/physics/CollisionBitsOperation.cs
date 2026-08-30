// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Godot.Core.Statescript.Physics;

/// <summary>
/// What a collision-bits node does to the bits it is given.
/// </summary>
/// <remarks>
/// The bits input names which layers to act on, never the whole field, so a node that clears one layer leaves the rest
/// of a body's collision setup exactly as the scene authored it.
/// </remarks>
public enum CollisionBitsOperation
{
	/// <summary>
	/// Turns the given bits off, leaving every other bit alone.
	/// </summary>
	Clear = 0,

	/// <summary>
	/// Turns the given bits on, leaving every other bit alone.
	/// </summary>
	Set = 1,
}
