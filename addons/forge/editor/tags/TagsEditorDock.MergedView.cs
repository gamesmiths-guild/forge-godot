// Copyright © Gamesmiths Guild.

#if TOOLS
using System.Collections.Generic;
using Gamesmiths.Forge.Tags;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Tags;

/// <summary>
/// The dock's read-only view: every source merged into the hierarchy the runtime actually resolves.
/// </summary>
/// <remarks>
/// Read-only on purpose. A tag here can come from several sources at once, so "remove" would have to ask which source
/// it meant, and "add" would have no obvious destination - both questions the By Source view answers by construction.
/// </remarks>
public partial class TagsEditorDock
{
	private const int TagColumnRatio = 2;
	private const int DeclaredByColumnRatio = 1;
	private const int DeclaredByMinimumWidth = 120;

	private void BuildMergedView(Tree tree, TreeItem root)
	{
		// Both columns expand on a ratio rather than sizing the second to its content: source names are long enough
		// that content sizing either squeezes the tags or pushes the tree into horizontal scrolling.
		tree.SetColumnExpand(0, true);
		tree.SetColumnExpand(1, true);
		tree.SetColumnExpandRatio(0, TagColumnRatio);
		tree.SetColumnExpandRatio(1, DeclaredByColumnRatio);
		tree.SetColumnCustomMinimumWidth(1, DeclaredByMinimumWidth);
		tree.ColumnTitlesVisible = true;
		tree.SetColumnTitle(0, "Tag");
		tree.SetColumnTitle(1, "Declared by");

		TagsManager merged = ForgeTagsRegistry.MergedTags;

		if (merged.RootNode.ChildTags.Count == 0)
		{
			TreeItem empty = tree.CreateItem(root);
			empty.SetText(0, "No tag has been registered yet.");
			empty.SetCustomColor(0, Color.FromHtml("EED202"));
			return;
		}

		// Deliberately not going through the search bar's source picker: this view exists to show every source at
		// once, so the picker is hidden here and its selection is kept for when the user switches back.
		var filter = TagTreeFilter.Create(merged, _searchBar?.SearchText ?? string.Empty);

		TagSourceTreeBuilder.Build(tree, root, merged.RootNode, filter, DecorateMergedRow, _tagRows);
	}

	private void DecorateMergedRow(TreeItem item, string completeTagKey)
	{
		IReadOnlyList<string> declaringSources = ForgeTagsRegistry.GetDeclaringSources(completeTagKey);

		string tooltip = declaringSources.Count switch
		{
			0 => "No source declares this tag.",
			1 => $"Declared by {declaringSources[0]}.",
			_ => "Declared by:\n" + string.Join("\n", declaringSources),
		};

		item.SetText(1, string.Join(", ", declaringSources));
		item.SetTooltipText(1, tooltip);

		item.SetCustomColor(1, Color.FromHtml("8A8A8A"));

		string collapseKey = TagCollapseKey(MergedCollapseScope, completeTagKey);
		_collapseKeys[item] = collapseKey;
		item.Collapsed = _collapsedKeys.Contains(collapseKey);
	}
}
#endif
