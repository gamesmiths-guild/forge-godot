// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Godot.Core.Statescript.Resolvers;

/// <summary>
/// Which coordinate space a spatial resolver or node reads and writes.
/// </summary>
public enum TransformSpace
{
	/// <summary>
	/// World space. This is what almost every gameplay question means: distances, directions to a target, spawn points.
	/// </summary>
	Global = 0,

	/// <summary>
	/// Space relative to the node's parent.
	/// </summary>
	Local = 1,
}
