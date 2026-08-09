// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using System.Collections.Generic;
using System.Linq;
using Gamesmiths.Forge.Tags;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Tags;

/// <summary>
/// A search box plus a source picker, used above any tag tree to narrow what it shows.
/// </summary>
/// <remarks>
/// The selected source is remembered by reference rather than by index or by <see cref="SourceEntry"/>, because both
/// of those go stale the moment the registry rebuilds - which happens on every tag edit.
/// </remarks>
[Tool]
internal sealed partial class TagTreeSearchBar : HBoxContainer, ISerializationListener
{
	private const string AllSourcesLabel = "All sources";

	private LineEdit? _searchField;
	private SearchableOptionButton? _sourcePicker;

	private string? _selectedReference;

	/// <summary>
	/// Raised when the search term or the selected source changes.
	/// </summary>
	public event Action? FilterChanged;

	/// <summary>
	/// Gets the current search term.
	/// </summary>
	public string SearchText => _searchField is not null && IsInstanceValid(_searchField)
		? _searchField.Text
		: string.Empty;

	/// <summary>
	/// Gets or sets a single hierarchy this bar filters instead of the project's sources.
	/// </summary>
	/// <remarks>
	/// Set by hosts that show exactly one source, such as the tag source inspector. The source picker is hidden then,
	/// since there is nothing to choose between.
	/// </remarks>
	public TagsManager? FixedTags { get; set; }

	/// <summary>
	/// Gets or sets a value indicating whether the source picker can be used.
	/// </summary>
	/// <remarks>
	/// Hosts that already show every source at once turn it off, since narrowing to one would contradict what they
	/// are showing. It is greyed out rather than hidden so that toggling between such a view and a normal one does not
	/// change the row's height under the user.
	/// </remarks>
	public bool SourcePickerEnabled { get; set; } = true;

	public override void _Ready()
	{
		SizeFlagsHorizontal = SizeFlags.ExpandFill;

		_searchField = new LineEdit
		{
			PlaceholderText = "Filter tags",
			ClearButtonEnabled = true,
			RightIcon = EditorInterface.Singleton.GetEditorTheme().GetIcon("Search", "EditorIcons"),
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
		};

		AddChild(_searchField);

		_sourcePicker = new SearchableOptionButton
		{
			TooltipText = "Show tags from a single source.",
		};

		AddChild(_sourcePicker);

		RefreshSources();

		_searchField.TextChanged += OnSearchTextChanged;
		_sourcePicker.ItemSelected += OnSourceSelected;
	}

	public override void _ExitTree()
	{
		ReleaseUiState();
		base._ExitTree();
	}

	/// <inheritdoc/>
	public override void _Notification(int what)
	{
		base._Notification(what);

		// The search glyph is taken from the editor theme once, so it has to be re-read when that theme changes.
		if (what == NotificationThemeChanged && _searchField is not null && IsInstanceValid(_searchField))
		{
			_searchField.RightIcon = EditorInterface.Singleton.GetEditorTheme().GetIcon("Search", "EditorIcons");
		}
	}

	public void OnBeforeSerialize()
	{
		ReleaseUiState();
	}

	public void OnAfterDeserialize()
	{
		// This method is intentionally left blank.
	}

	/// <summary>
	/// Rebuilds the source list, keeping the current selection when that source still exists.
	/// </summary>
	public void RefreshSources()
	{
		if (_sourcePicker is null || !IsInstanceValid(_sourcePicker))
		{
			return;
		}

		if (FixedTags is not null)
		{
			_sourcePicker.Visible = false;
			return;
		}

		_sourcePicker.Disabled = !SourcePickerEnabled;
		_sourcePicker.Clear();
		_sourcePicker.AddItem(AllSourcesLabel);

		int selectedIndex = 0;
		IReadOnlyList<SourceEntry> sources = ForgeTagsRegistry.Sources;

		for (int i = 0; i < sources.Count; i++)
		{
			SourceEntry entry = sources[i];
			_sourcePicker.AddItem(entry.DisplayName);

			if (entry.Reference == _selectedReference)
			{
				selectedIndex = i + 1;
			}
		}

		if (selectedIndex == 0)
		{
			_selectedReference = null;
		}

		_sourcePicker.Selected = selectedIndex;
		_sourcePicker.Visible = sources.Count > 1;
	}

	/// <summary>
	/// Gets the hierarchy the tree should render, honoring the selected source.
	/// </summary>
	/// <returns>The selected source's hierarchy, or the merged one.</returns>
	public TagsManager ResolveTags()
	{
		if (FixedTags is not null)
		{
			return FixedTags;
		}

		if (_selectedReference is null)
		{
			return ForgeTagsRegistry.MergedTags;
		}

		SourceEntry? entry = ForgeTagsRegistry.Sources
			.FirstOrDefault(source => source.Reference == _selectedReference);

		return entry?.Tags ?? ForgeTagsRegistry.MergedTags;
	}

	/// <summary>
	/// Builds the filter matching the current search term for <paramref name="tags"/>.
	/// </summary>
	/// <param name="tags">The hierarchy being displayed, normally from <see cref="ResolveTags"/>.</param>
	/// <returns>The filter, or <see langword="null"/> when nothing is being searched for.</returns>
	public TagTreeFilter? ResolveFilter(TagsManager tags)
	{
		return TagTreeFilter.Create(tags, SearchText);
	}

	private void OnSearchTextChanged(string newText)
	{
		FilterChanged?.Invoke();
	}

	private void OnSourceSelected(long index)
	{
		IReadOnlyList<SourceEntry> sources = ForgeTagsRegistry.Sources;

		_selectedReference = index > 0 && index <= sources.Count ? sources[(int)index - 1].Reference : null;

		FilterChanged?.Invoke();
	}

	private void ReleaseUiState()
	{
		if (_searchField is not null && IsInstanceValid(_searchField))
		{
			_searchField.TextChanged -= OnSearchTextChanged;
		}

		if (_sourcePicker is not null && IsInstanceValid(_sourcePicker))
		{
			_sourcePicker.ItemSelected -= OnSourceSelected;
		}

		FilterChanged = null;
		_searchField = null;
		_sourcePicker = null;
	}
}
#endif
