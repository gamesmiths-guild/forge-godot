// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Godot.Resources.Statescript;

/// <summary>
/// Indicates the direction of a node property binding.
/// </summary>
public enum StatescriptPropertyDirection
{
	/// <summary>
	/// An input property that feeds a value into the node.
	/// </summary>
	Input = 0,

	/// <summary>
	/// An output variable that the node writes a value to.
	/// </summary>
	Output = 1,
}
