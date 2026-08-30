// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Godot.Core.Statescript.Resolvers;

/// <summary>
/// How the mouse cursor is turned into a point in the world.
/// </summary>
public enum MouseWorldMode
{
	/// <summary>
	/// Cast the cursor's ray into physics and take what it hits. This follows the terrain, so a cursor over a hill
	/// resolves onto the hill, and it is what a game whose ground is not flat wants.
	/// </summary>
	PhysicsRay = 0,

	/// <summary>
	/// Intersect the cursor's ray with the horizontal plane the caster is standing on. Nothing between the camera and
	/// the ground can block it, so a cursor over a wall, a tree or another character still resolves to the ground under
	/// it - which is what a top-down game usually means by "where the player clicked".
	/// </summary>
	PlaneIntersect = 1,
}
