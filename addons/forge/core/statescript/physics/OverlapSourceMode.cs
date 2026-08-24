// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Godot.Core.Statescript.Physics;

/// <summary>
/// Where a monitored overlap gets its shape from.
/// </summary>
public enum OverlapSourceMode
{
	/// <summary>
	/// An area already in the scene, named by a path. Use this when the shape belongs to the art - a weapon's hitbox,
	/// a room trigger - and for anything whose shape is more elaborate than one primitive.
	/// </summary>
	ExistingArea = 0,

	/// <summary>
	/// A shape the query builds from its own inputs. Use this when the shape is decided at cast time, so a radius can
	/// scale with an attribute or an ability level.
	/// </summary>
	TransientShape = 1,
}
