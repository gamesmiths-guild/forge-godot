// Copyright © Gamesmiths Guild.

#if TOOLS
using Gamesmiths.Forge.Godot.Resources;
using Gamesmiths.Forge.Tags;

namespace Gamesmiths.Forge.Godot.Editor.Tags;

/// <summary>
/// One configured tag source, as the editor sees it.
/// </summary>
/// <param name="Reference">The reference exactly as stored in project settings.</param>
/// <param name="ResourcePath">The resolved resource path, or the reference itself when it cannot be resolved.</param>
/// <param name="DisplayName">A short name for the source, suitable for a header row.</param>
/// <param name="Resource">The loaded source, or <see langword="null"/> when its file is missing.</param>
/// <param name="Tags">The hierarchy built from this source alone, or <see langword="null"/> when it is missing.</param>
internal sealed record SourceEntry(
	string Reference,
	string ResourcePath,
	string DisplayName,
	ForgeTagsSource? Resource,
	TagsManager? Tags)
{
	/// <summary>
	/// Gets a value indicating whether this source's file could not be loaded.
	/// </summary>
	public bool IsMissing => Resource is null;
}
#endif
