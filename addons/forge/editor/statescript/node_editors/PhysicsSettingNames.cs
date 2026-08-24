// Copyright © Gamesmiths Guild.

#if TOOLS
namespace Gamesmiths.Forge.Godot.Editor.Statescript.NodeEditors;

/// <summary>
/// The enum member names the physics node editors offer, shared so a rename in the runtime enums has one place to be
/// mirrored rather than one per editor.
/// </summary>
/// <remarks>
/// These must match the runtime enum member names exactly: the settings section stores the selected member by name, and
/// the graph builder parses that name back into the enum.
/// </remarks>
internal static class PhysicsSettingNames
{
	/// <summary>
	/// Member names of the collision bits target enum.
	/// </summary>
#pragma warning disable IDE1006 // Naming Styles
	internal static readonly string[] CollisionTargets = ["Layer", "Mask"];

	/// <summary>
	/// Member names of the collision bits operation enum.
	/// </summary>
	internal static readonly string[] CollisionOperations = ["Clear", "Set"];

	/// <summary>
	/// Member names of the overlap source mode enum.
	/// </summary>
	internal static readonly string[] OverlapSources = ["ExistingArea", "TransientShape"];

#pragma warning restore IDE1006 // Naming Styles
}
#endif
