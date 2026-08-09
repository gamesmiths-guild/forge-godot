// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using System.Collections.Generic;
using Gamesmiths.Forge.Tags;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Tags;

/// <summary>
/// Turns a tag hierarchy into <see cref="TreeItem"/> rows.
/// </summary>
/// <remarks>
/// The dock, the tag source inspector and the tag pickers all render the same hierarchy and differ only in what each
/// row carries - buttons, check marks, nothing - so the walk lives here and the host supplies a decorator. Sharing a
/// base control instead would not work: one host is an <see cref="EditorProperty"/> and another a plain container.
/// </remarks>
internal static class TagSourceTreeBuilder
{
	/// <summary>
	/// Determines whether <paramref name="node"/> has any descendant the filter would keep, so a host can skip
	/// rendering a section that would come out empty.
	/// </summary>
	/// <param name="node">The hierarchy node to test.</param>
	/// <param name="filter">The active search filter, or <see langword="null"/> to show everything.</param>
	/// <returns><see langword="true"/> when at least one row would be created.</returns>
	public static bool HasVisibleRows(TagNode node, TagTreeFilter? filter)
	{
		foreach (TagNode child in node.ChildTags)
		{
			if (filter?.ShouldShow(child.CompleteTagKey) != false || HasVisibleRows(child, filter))
			{
				return true;
			}
		}

		return false;
	}

	/// <summary>
	/// Creates a row for every visible descendant of <paramref name="node"/>, under <paramref name="parent"/>.
	/// </summary>
	/// <param name="tree">The tree being populated.</param>
	/// <param name="parent">The item to attach rows to.</param>
	/// <param name="node">The hierarchy node whose children become rows.</param>
	/// <param name="filter">The active search filter, or <see langword="null"/> to show everything.</param>
	/// <param name="decorateRow">Adds whatever the host wants on a row, given its full tag key.</param>
	/// <param name="rowMap">Receives the row-to-tag-key mapping the host needs to handle clicks.</param>
	public static void Build(
		Tree tree,
		TreeItem parent,
		TagNode node,
		TagTreeFilter? filter,
		Action<TreeItem, string> decorateRow,
		IDictionary<TreeItem, string> rowMap)
	{
		foreach (TagNode child in node.ChildTags)
		{
			string completeTagKey = child.CompleteTagKey;

			if (filter?.ShouldShow(completeTagKey) == false)
			{
				continue;
			}

			TreeItem item = tree.CreateItem(parent);
			item.SetText(0, child.TagKey);

			rowMap[item] = completeTagKey;
			decorateRow(item, completeTagKey);

			if (filter is not null)
			{
				// Every surviving row is either a match or on the path to one, so leaving any of them collapsed would
				// hide what the search just found.
				item.Collapsed = false;
			}

			Build(tree, item, child, filter, decorateRow, rowMap);
		}
	}
}
#endif
