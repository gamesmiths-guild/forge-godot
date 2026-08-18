// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Godot.Core.Statescript.Resolvers;

/// <summary>
/// Which of a node's own axes a direction resolver reports.
/// </summary>
/// <remarks>
/// Named for what the direction means rather than for the underlying basis column, because Godot's forward is −Z and
/// authoring against that sign directly is a reliable source of backwards projectiles.
/// </remarks>
public enum SpatialAxis
{
	/// <summary>The direction the node faces: −Z in 3D, +X in 2D.</summary>
	Forward = 0,

	/// <summary>Directly behind the node.</summary>
	Back = 1,

	/// <summary>To the node's right.</summary>
	Right = 2,

	/// <summary>To the node's left.</summary>
	Left = 3,

	/// <summary>Out of the top of the node.</summary>
	Up = 4,

	/// <summary>Out of the bottom of the node.</summary>
	Down = 5,
}
