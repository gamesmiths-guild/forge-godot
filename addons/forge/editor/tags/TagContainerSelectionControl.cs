// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using System.Collections.Generic;
using Gamesmiths.Forge.Tags;
using Godot;

using GodotStringArray = Godot.Collections.Array<string>;

namespace Gamesmiths.Forge.Godot.Editor.Tags;

[Tool]
public partial class TagContainerSelectionControl : VBoxContainer, ISerializationListener
{
	private readonly Dictionary<TreeItem, string> _treeItemToTag = [];

	private Button? _containerButton;
	private TagTreeSearchBar? _searchBar;
	private ScrollContainer? _scroll;
	private Tree? _tree;
	private Texture2D? _checkedIcon;
	private Texture2D? _uncheckedIcon;
	private GodotStringArray _currentValue = [];

	public event Action<GodotStringArray>? ValueChanged;

	public override void _Ready()
	{
		SizeFlagsHorizontal = SizeFlags.ExpandFill;

		_containerButton = new Button
		{
			ToggleMode = true,
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
		};
		_containerButton.Toggled += OnToggled;
		AddChild(_containerButton);

		_searchBar = new TagTreeSearchBar
		{
			Visible = false,
		};

		_searchBar.FilterChanged += OnFilterChanged;
		AddChild(_searchBar);

		_scroll = new ScrollContainer
		{
			Visible = false,
			CustomMinimumSize = new Vector2(0, 220),
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			SizeFlagsVertical = SizeFlags.ExpandFill,
		};

		_tree = new Tree
		{
			HideRoot = true,
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			SizeFlagsVertical = SizeFlags.ExpandFill,
		};
		_tree.ButtonClicked += OnTreeButtonClicked;

		_scroll.AddChild(_tree);
		AddChild(_scroll);

		_checkedIcon = EditorInterface.Singleton
			.GetEditorTheme()
			.GetIcon("GuiChecked", "EditorIcons");
		_uncheckedIcon = EditorInterface.Singleton
			.GetEditorTheme()
			.GetIcon("GuiUnchecked", "EditorIcons");

		ForgeTagsRegistry.Changed += OnRegisteredTagsChanged;

		RebuildTree();
	}

	public override void _ExitTree()
	{
		ReleaseUiState();
		base._ExitTree();
	}

	public void OnBeforeSerialize()
	{
		// An assembly reload drops every delegate-backed signal connection, so they have to be released here, while
		// they still exist. Doing it from _ExitTree alone means the disconnect runs against connections Godot already
		// took away, which it reports as an error.
		ReleaseUiState();
	}

	public void OnAfterDeserialize()
	{
		// This method is intentionally left blank.
	}

	public void SetValue(GodotStringArray? value)
	{
		_currentValue = [];
		if (value is not null)
		{
			_currentValue.AddRange(value);
		}

		if (_tree is not null)
		{
			RebuildTree();
		}
	}

	private void OnRegisteredTagsChanged()
	{
		RebuildTree();
	}

	private void ReleaseUiState()
	{
		ForgeTagsRegistry.Changed -= OnRegisteredTagsChanged;

		if (_containerButton is not null && IsInstanceValid(_containerButton))
		{
			_containerButton.Toggled -= OnToggled;
		}

		if (_tree is not null && IsInstanceValid(_tree))
		{
			_tree.ButtonClicked -= OnTreeButtonClicked;
		}

		if (_searchBar is not null && IsInstanceValid(_searchBar))
		{
			_searchBar.FilterChanged -= OnFilterChanged;
		}

		ValueChanged = null;
		_treeItemToTag.Clear();
		_containerButton = null;
		_searchBar = null;
		_scroll = null;
		_tree = null;
		_checkedIcon = null;
		_uncheckedIcon = null;
	}

	private void RebuildTree()
	{
		if (_tree is null || _containerButton is null || _checkedIcon is null || _uncheckedIcon is null)
		{
			return;
		}

		_tree.Clear();
		_treeItemToTag.Clear();
		_containerButton.Text = $"Container (size: {_currentValue.Count})";

		_searchBar?.RefreshSources();

		TagsManager tags = _searchBar?.ResolveTags() ?? ForgeTagsRegistry.MergedTags;
		TreeItem root = _tree.CreateItem();

		TagSourceTreeBuilder.Build(
			_tree,
			root,
			tags.RootNode,
			_searchBar?.ResolveFilter(tags),
			DecorateRow,
			_treeItemToTag);
	}

	private void DecorateRow(TreeItem item, string completeTagKey)
	{
		item.AddButton(0, _currentValue.Contains(completeTagKey) ? _checkedIcon : _uncheckedIcon);
	}

	private void OnTreeButtonClicked(
		TreeItem item,
		long column,
		long id,
		long mouseButtonIndex)
	{
		if (_tree is null || !IsInstanceValid(_tree))
		{
			return;
		}

		if (mouseButtonIndex != 1 || id != 0)
		{
			return;
		}

		string tag = _treeItemToTag[item];
		var newValue = new GodotStringArray();
		newValue.AddRange(_currentValue);

		if (!newValue.Remove(tag))
		{
			newValue.Add(tag);
		}

		SetValue(newValue);
		ValueChanged?.Invoke(newValue);
	}

	private void OnToggled(bool toggled)
	{
		if (_scroll is null || _searchBar is null || !IsInstanceValid(_scroll) || !IsInstanceValid(_searchBar))
		{
			return;
		}

		_scroll.Visible = toggled;
		_searchBar.Visible = toggled;
	}

	private void OnFilterChanged()
	{
		RebuildTree();
	}
}
#endif
