// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Godot.Core.Statescript.Resolvers;

/// <summary>
/// Which of a character body's contact states a Character State resolver reports.
/// </summary>
/// <remarks>
/// The "only" variants are not redundant with the plain ones. A character wedged into a corner is on the floor and on
/// a wall at once, and a wall-jump that fires whenever it is on a wall would also fire while the player is simply
/// standing next to one.
/// </remarks>
public enum CharacterStateQuery
{
	/// <summary>The body is standing on something the game counts as floor.</summary>
	OnFloor = 0,

	/// <summary>The body is on the floor and touching nothing else.</summary>
	OnFloorOnly = 1,

	/// <summary>The body is touching something the game counts as a wall.</summary>
	OnWall = 2,

	/// <summary>The body is on a wall and touching nothing else, which is the wall-jump state.</summary>
	OnWallOnly = 3,

	/// <summary>The body has hit something above it.</summary>
	OnCeiling = 4,

	/// <summary>The body is on the ceiling and touching nothing else.</summary>
	OnCeilingOnly = 5,
}
