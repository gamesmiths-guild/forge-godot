// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Godot.Core.Statescript.Resolvers;

/// <summary>
/// Which of a character body's motion readings a Character Motion resolver reports.
/// </summary>
public enum CharacterMotionValue
{
	/// <summary>
	/// The velocity the body actually achieved on its last move, after sliding and collisions. This is the one Entity
	/// Velocity cannot give: that reads the velocity the game <em>asked</em> for, which for a character walking into a
	/// wall is not the one it got.
	/// </summary>
	RealVelocity = 0,

	/// <summary>The normal of the floor the body is standing on. Zero when it is not on one.</summary>
	FloorNormal = 1,

	/// <summary>The normal of the wall the body is touching, which is the direction a wall-jump pushes off in.
	/// </summary>
	WallNormal = 2,

	/// <summary>How far the body moved on its last move, after sliding.</summary>
	LastMotion = 3,

	/// <summary>How far the body's position changed on its last move, including any collision recovery.</summary>
	PositionDelta = 4,

	/// <summary>The velocity of the moving platform the body is standing on. Zero when it is not on one.</summary>
	PlatformVelocity = 5,
}
