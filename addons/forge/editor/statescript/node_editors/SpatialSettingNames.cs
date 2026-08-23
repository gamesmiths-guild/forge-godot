// Copyright © Gamesmiths Guild.

#if TOOLS
namespace Gamesmiths.Forge.Godot.Editor.Statescript.NodeEditors;

/// <summary>
/// The enum member names the spatial node editors offer, shared so a rename in the runtime enums has one place to be
/// mirrored rather than one per editor.
/// </summary>
/// <remarks>
/// These must match the runtime enum member names exactly: the settings section stores the selected member by name, and
/// the graph builder parses that name back into the enum.
/// </remarks>
internal static class SpatialSettingNames
{
	/// <summary>
	/// Member names of the transform space enum.
	/// </summary>
#pragma warning disable IDE1006 // Naming Styles
	internal static readonly string[] Spaces = ["Global", "Local"];

	/// <summary>
	/// Member names of the move-to mode enum.
	/// </summary>
	internal static readonly string[] MoveModes = ["Duration", "Speed"];

	/// <summary>
	/// Member names of the move-to easing enum.
	/// </summary>
	internal static readonly string[] Easings = ["Linear", "EaseIn", "EaseOut", "EaseInOut"];
#pragma warning restore IDE1006 // Naming Styles
}
#endif
