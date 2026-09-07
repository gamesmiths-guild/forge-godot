// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Godot.Core.Statescript.Physics;

/// <summary>
/// Which set of physics layer names a collision layer or mask is authored against.
/// </summary>
/// <remarks>
/// A 2D and a 3D world number their thirty-two layers independently and name them under separate project settings, so
/// a grid that shows those names has to be told which of the two worlds it is picking layers in.
/// </remarks>
public enum CollisionLayerSpace
{
	/// <summary>
	/// Not a collision layer field. The value is an ordinary integer.
	/// </summary>
	None = 0,

	/// <summary>
	/// The 2D physics layers, named under <c>layer_names/2d_physics</c>.
	/// </summary>
	Physics2D = 1,

	/// <summary>
	/// The 3D physics layers, named under <c>layer_names/3d_physics</c>.
	/// </summary>
	Physics3D = 2,
}
