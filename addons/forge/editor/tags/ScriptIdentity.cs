// Copyright © Gamesmiths Guild.

#if TOOLS
namespace Gamesmiths.Forge.Godot.Editor.Tags;

/// <summary>
/// Identifies a script by both of the ways a resource file can point at one.
/// </summary>
/// <param name="Uid">The script's <c>uid://</c> reference.</param>
/// <param name="Path">The script's <c>res://</c> path, used as the fallback reference.</param>
internal readonly record struct ScriptIdentity(string Uid, string Path);
#endif
