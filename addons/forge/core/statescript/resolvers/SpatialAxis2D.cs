// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Godot.Core.Statescript.Resolvers;

/// <summary>
/// Which of a 2D node's own axes a direction resolver reports.
/// </summary>
/// <remarks>
/// Separate from <see cref="SpatialAxis"/> because 2D has four of these and not six. Offering Up and Down here would be
/// offering a third axis a plane does not have, and screen-up is already Left or Right of a facing rather than a
/// direction a node carries.
/// </remarks>
public enum SpatialAxis2D
{
	/// <summary>The direction the node faces, which in Godot's 2D is +X at a rotation of zero.</summary>
	Forward = 0,

	/// <summary>Directly behind the node.</summary>
	Back = 1,

	/// <summary>To the node's right, which is +Y: screen down is a character's right when it faces screen right.
	/// </summary>
	Right = 2,

	/// <summary>To the node's left.</summary>
	Left = 3,
}
