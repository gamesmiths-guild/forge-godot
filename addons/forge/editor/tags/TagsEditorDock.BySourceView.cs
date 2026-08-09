// Copyright © Gamesmiths Guild.

#if TOOLS
using System.Collections.Generic;
using Gamesmiths.Forge.Godot.Core;
using Gamesmiths.Forge.Godot.Resources;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Tags;

/// <summary>
/// The dock's editable view: one collapsible section per tag source.
/// </summary>
public partial class TagsEditorDock
{
	private static string SourceCollapseKey(SourceEntry entry)
	{
		return $"src|{entry.ResourcePath}";
	}

	private static string TagCollapseKey(string scope, string completeTagKey)
	{
		return $"{scope}|{completeTagKey}";
	}

	private static void MoveSource(int index, int delta)
	{
		var references = new List<string>(ForgeSettings.GetSourceReferences());
		int target = index + delta;

		if (index < 0 || index >= references.Count || target < 0 || target >= references.Count)
		{
			return;
		}

#pragma warning disable IDE0180 // Use tuple to swap values
		string moved = references[index];
#pragma warning restore IDE0180 // Use tuple to swap values
		references[index] = references[target];
		references[target] = moved;

		ForgeSettings.SetSourceReferences([.. references]);
		ForgeTagsRegistry.Invalidate();
	}

	private static void RemoveSourceReference(int index)
	{
		var references = new List<string>(ForgeSettings.GetSourceReferences());

		if (index < 0 || index >= references.Count)
		{
			return;
		}

		string removed = ForgeSettings.ResolveReference(references[index]);
		references.RemoveAt(index);

		ForgeSettings.SetSourceReferences([.. references]);
		ForgeTagsRegistry.Invalidate();

		// Worth saying out loud, because "remove" on a source row could easily be read as deleting the file.
		GD.Print($"Stopped reading tags from '{removed}'. The file was not deleted.");
	}

	private static void RevealSource(int sourceIndex)
	{
		IReadOnlyList<SourceEntry> sources = ForgeTagsRegistry.Sources;

		if (sourceIndex < 0 || sourceIndex >= sources.Count)
		{
			return;
		}

		SourceEntry entry = sources[sourceIndex];

		if (entry.IsMissing)
		{
			return;
		}

		EditorInterface.Singleton.SelectFile(entry.ResourcePath);
	}

	private void BuildBySourceView(Tree tree, TreeItem root)
	{
		tree.ColumnTitlesVisible = false;
		tree.SetColumnExpand(0, true);

		IReadOnlyList<SourceEntry> sources = ForgeTagsRegistry.Sources;

		if (sources.Count == 0)
		{
			TreeItem empty = tree.CreateItem(root);
			empty.SetText(0, "No tag source yet. Use New Source or Add Existing.");
			empty.SetCustomColor(0, Color.FromHtml("EED202"));
			return;
		}

		for (int i = 0; i < sources.Count; i++)
		{
			BuildSourceSection(tree, root, sources[i], i, sources.Count);
		}
	}

	private void BuildSourceSection(Tree tree, TreeItem root, SourceEntry entry, int index, int sourceCount)
	{
		TagTreeFilter? filter = entry.Tags is null ? null : _searchBar?.ResolveFilter(entry.Tags);
		bool filtering = filter is not null;

		// A source with nothing matching the search would otherwise show as a lone header, which reads like an error.
		if (filtering && entry.Tags is not null && !TagSourceTreeBuilder.HasVisibleRows(entry.Tags.RootNode, filter))
		{
			return;
		}

		TreeItem header = tree.CreateItem(root);

		string headerTooltip = entry.IsMissing
			? $"'{entry.ResourcePath}' could not be loaded. It may have been moved or deleted."
			: entry.ResourcePath;

		header.SetText(0, entry.IsMissing ? $"{entry.DisplayName}  (MISSING)" : entry.DisplayName);
		header.SetTooltipText(0, headerTooltip);

		// Weight rather than a background: Godot will not paint a cell's background under that cell's buttons, so any
		// tint here would stop halfway across the row and read as a rendering fault.
		if (_boldFont is not null)
		{
			header.SetCustomFont(0, _boldFont);
		}

		if (entry.IsMissing)
		{
			header.SetCustomColor(0, Color.FromHtml("FF6B6B"));
		}
		else
		{
			header.SetCustomColor(0, ResolveSourceHeaderColor());

			if (_sourceIcon is not null)
			{
				header.SetIcon(0, _sourceIcon);
			}
		}

		_rowSourceIndex[header] = index;

		string collapseKey = SourceCollapseKey(entry);
		_collapseKeys[header] = collapseKey;
		header.Collapsed = !filtering && _collapsedKeys.Contains(collapseKey);

		AddSourceHeaderButtons(header, entry, index, sourceCount);

		if (entry.Tags is null)
		{
			return;
		}

		if (!TagSourceTreeBuilder.HasVisibleRows(entry.Tags.RootNode, filter))
		{
			TreeItem empty = tree.CreateItem(header);
			empty.SetText(0, "No tags yet.");
			empty.SetCustomColor(0, Color.FromHtml("8A8A8A"));
			return;
		}

		TagSourceTreeBuilder.Build(
			tree,
			header,
			entry.Tags.RootNode,
			filter,
			(item, completeTagKey) => DecorateSourceTagRow(item, completeTagKey, entry, index),
			_tagRows);
	}

	/// <summary>
	/// Adds a source header's buttons.
	/// </summary>
	/// <param name="header">The header row.</param>
	/// <param name="entry">The source the row stands for.</param>
	/// <param name="index">Its position in the source list.</param>
	/// <param name="sourceCount">How many sources there are.</param>
	/// <remarks>
	/// Buttons that do not apply are added disabled rather than left out. Tree buttons are laid out right to left in
	/// the order they are added, so omitting one shifts every button after it and the add/remove pair stops lining up
	/// with the tag rows below.
	/// </remarks>
	private void AddSourceHeaderButtons(TreeItem header, SourceEntry entry, int index, int sourceCount)
	{
		header.AddButton(0, _upIcon, (int)TagsTreeButton.MoveSourceUp, index == 0, "Move this source up.");

		header.AddButton(
			0,
			_downIcon,
			(int)TagsTreeButton.MoveSourceDown,
			index == sourceCount - 1,
			"Move this source down.");

		header.AddButton(
			0,
			_revealIcon,
			(int)TagsTreeButton.RevealSource,
			entry.IsMissing,
			"Show this file in the FileSystem.");

		header.AddButton(
			0,
			_addIcon,
			(int)TagsTreeButton.AddTagToSource,
			entry.IsMissing,
			"Add a tag to this source.");

		header.AddButton(
			0,
			_removeIcon,
			(int)TagsTreeButton.RemoveSourceReference,
			false,
			"Stop reading tags from this source. The file itself is not deleted.");
	}

	private void DecorateSourceTagRow(TreeItem item, string completeTagKey, SourceEntry entry, int sourceIndex)
	{
		bool declaredHere = entry.Resource?.DeclaresTag(completeTagKey) == true;

		item.AddButton(0, _addIcon, (int)TagsTreeButton.AddChildTag, false, "Add a child tag here.");

		string removeTooltip = declaredHere
			? "Remove this tag, and its children, from this source."
			: "Implied by the tags below it, so there is nothing to remove here.";

		// An implicit parent exists only because a descendant declares it, so there is nothing here to delete. The
		// button is still added, disabled, so the column stays aligned down the whole tree.
		item.AddButton(0, _removeIcon, (int)TagsTreeButton.RemoveTag, !declaredHere, removeTooltip);

		if (!declaredHere)
		{
			string implicitTooltip =
				$"'{completeTagKey}' is implied by the tags below it. Remove those to make it go away.";

			item.SetCustomColor(0, Color.FromHtml("8A8A8A"));
			item.SetTooltipText(0, implicitTooltip);
		}

		string collapseKey = TagCollapseKey(entry.ResourcePath, completeTagKey);
		_collapseKeys[item] = collapseKey;
		item.Collapsed = _collapsedKeys.Contains(collapseKey);

		_rowSourceIndex.TryAdd(item, sourceIndex);
	}

	private void OnTreeButtonClicked(TreeItem item, long column, long id, long mouseButtonIndex)
	{
		// Every button this dock adds lives in column 0; the Merged view's second column is text only.
		if (mouseButtonIndex != 1 || column != 0)
		{
			return;
		}

		var button = (TagsTreeButton)id;

		if (_rowSourceIndex.TryGetValue(item, out int sourceIndex))
		{
			HandleRowButton(button, item, sourceIndex);
		}
	}

	private void HandleRowButton(TagsTreeButton button, TreeItem item, int sourceIndex)
	{
		bool isTagRow = _tagRows.TryGetValue(item, out string? tagKey);

		switch (button)
		{
			case TagsTreeButton.AddTagToSource:
				PromptAddTag(sourceIndex, string.Empty);
				break;

			case TagsTreeButton.AddChildTag when isTagRow:
				PromptAddTag(sourceIndex, $"{tagKey}.");
				break;

			case TagsTreeButton.RemoveTag when isTagRow:
				RemoveTagFromSource(sourceIndex, tagKey!);
				break;

			case TagsTreeButton.MoveSourceUp:
				MoveSource(sourceIndex, -1);
				break;

			case TagsTreeButton.MoveSourceDown:
				MoveSource(sourceIndex, 1);
				break;

			case TagsTreeButton.RemoveSourceReference:
				RemoveSourceReference(sourceIndex);
				break;

			case TagsTreeButton.RevealSource:
				RevealSource(sourceIndex);
				break;
		}
	}

	private void RemoveTagFromSource(int sourceIndex, string completeTagKey)
	{
		ForgeTagsSource? source = GetSourceAt(sourceIndex);

		if (source is null || _controller is null)
		{
			return;
		}

		_scrollToTagKey = completeTagKey;
		_controller.RemoveTag(source, completeTagKey);
	}
}
#endif
