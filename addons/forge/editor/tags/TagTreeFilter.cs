// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using System.Collections.Generic;
using Gamesmiths.Forge.Tags;

namespace Gamesmiths.Forge.Godot.Editor.Tags;

/// <summary>
/// The set of tag rows a search term leaves visible.
/// </summary>
/// <remarks>
/// A match pulls in its ancestors, so the row stays reachable in the tree, and its descendants, so searching for a
/// branch shows the whole branch. The set is computed once per rebuild rather than per row, because a tree walk asks
/// the same question of the same keys repeatedly.
/// </remarks>
internal sealed class TagTreeFilter
{
	private readonly HashSet<string> _visible;

	private TagTreeFilter(HashSet<string> visible)
	{
		_visible = visible;
	}

	/// <summary>
	/// Builds a filter over <paramref name="tags"/>.
	/// </summary>
	/// <param name="tags">The hierarchy being displayed.</param>
	/// <param name="searchText">The search term.</param>
	/// <returns>A filter, or <see langword="null"/> when <paramref name="searchText"/> selects everything.</returns>
	public static TagTreeFilter? Create(TagsManager tags, string searchText)
	{
		if (string.IsNullOrWhiteSpace(searchText))
		{
			return null;
		}

		var visible = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		Collect(tags.RootNode, searchText.Trim(), matchedAncestor: false, visible);

		return new TagTreeFilter(visible);
	}

	/// <summary>
	/// Determines whether a row should be created for <paramref name="completeTagKey"/>.
	/// </summary>
	/// <param name="completeTagKey">The full dotted key of the tag.</param>
	/// <returns><see langword="true"/> when the row is visible under this filter.</returns>
	public bool ShouldShow(string completeTagKey)
	{
		return _visible.Contains(completeTagKey);
	}

	private static bool Collect(TagNode node, string searchText, bool matchedAncestor, HashSet<string> visible)
	{
		bool anyVisible = false;

		foreach (TagNode child in node.ChildTags)
		{
			string completeTagKey = child.CompleteTagKey;

			bool matched = matchedAncestor
				|| completeTagKey.Contains(searchText, StringComparison.OrdinalIgnoreCase);

			bool descendantVisible = Collect(child, searchText, matched, visible);

			if (!matched && !descendantVisible)
			{
				continue;
			}

			visible.Add(completeTagKey);
			anyVisible = true;
		}

		return anyVisible;
	}
}
#endif
