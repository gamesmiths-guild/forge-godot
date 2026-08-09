// Copyright © Gamesmiths Guild.

#if TOOLS
namespace Gamesmiths.Forge.Godot.Editor;

/// <summary>
/// A tag-bearing property found in a scene file, located by the line it sits on.
/// </summary>
/// <param name="LineIndex">The zero-based line the property is written on.</param>
/// <param name="PropertyName">The property's name, as written in the file.</param>
/// <param name="Location">Where the property lives, for reporting.</param>
/// <param name="IsContainer">Whether it holds a list of tags rather than a single one.</param>
/// <param name="Tags">The tags the property currently declares.</param>
internal sealed record SceneTagReference(
	int LineIndex,
	string PropertyName,
	string Location,
	bool IsContainer,
	string[] Tags);
#endif
