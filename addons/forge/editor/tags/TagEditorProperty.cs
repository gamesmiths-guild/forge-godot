// Copyright © Gamesmiths Guild.

#if TOOLS
using System.Collections.Generic;
using Gamesmiths.Forge.Tags;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Tags;

[Tool]
public partial class TagEditorProperty : EditorProperty, ISerializationListener
{
	private readonly Dictionary<TreeItem, string> _treeItemToTag = [];

	private VBoxContainer? _root;
	private Button? _containerButton;
	private TagTreeSearchBar? _searchBar;
	private ScrollContainer? _scroll;
	private Tree? _tree;

	private Texture2D? _checkedIcon;
	private Texture2D? _uncheckedIcon;

	private string _currentValue = string.Empty;

	public override void _Ready()
	{
		_root = new VBoxContainer
		{
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
		};

		_containerButton = new Button
		{
			ToggleMode = true,
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
		};
		_containerButton.Toggled += OnToggled;

		_searchBar = new TagTreeSearchBar
		{
			Visible = false,
		};

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

		_scroll.AddChild(_tree);

		_root.AddChild(_containerButton);
		_root.AddChild(_searchBar);
		_root.AddChild(_scroll);

		AddChild(_root);
		SetBottomEditor(_root);

		_checkedIcon = EditorInterface.Singleton
			.GetEditorTheme()
			.GetIcon("GuiRadioChecked", "EditorIcons");

		_uncheckedIcon = EditorInterface.Singleton
			.GetEditorTheme()
			.GetIcon("GuiRadioUnchecked", "EditorIcons");

		_tree.ButtonClicked += OnTreeButtonClicked;
		_searchBar.FilterChanged += OnFilterChanged;
		ForgeTagsRegistry.Changed += OnRegisteredTagsChanged;
	}

	public override void _UpdateProperty()
	{
		if (_tree is null || _containerButton is null || !IsInstanceValid(_tree) || !IsInstanceValid(_containerButton))
		{
			return;
		}

		GodotObject obj = GetEditedObject();
		string propertyName = GetEditedProperty();

		_currentValue = obj.Get(propertyName).AsString();
		RebuildTree();
	}

	public override void _ExitTree()
	{
		ReleaseUiState();
		FreeAllChildren();
		base._ExitTree();
	}

	public void OnBeforeSerialize()
	{
		ReleaseUiState();
		FreeAllChildren();
	}

	public void OnAfterDeserialize()
	{
	}

	private void RebuildTree()
	{
		if (_tree is null || _containerButton is null || _searchBar is null
			|| _checkedIcon is null || _uncheckedIcon is null)
		{
			return;
		}

		_tree.Clear();
		_treeItemToTag.Clear();

		_containerButton.Text = string.IsNullOrEmpty(_currentValue) ? "(none)" : _currentValue;

		_searchBar.RefreshSources();

		TagsManager tags = _searchBar.ResolveTags();
		TreeItem root = _tree.CreateItem();

		TagSourceTreeBuilder.Build(
			_tree,
			root,
			tags.RootNode,
			_searchBar.ResolveFilter(tags),
			DecorateRow,
			_treeItemToTag);

		UpdateMinimumSize();
		NotifyPropertyListChanged();
	}

	private void DecorateRow(TreeItem item, string completeTagKey)
	{
		item.AddButton(0, _currentValue == completeTagKey ? _checkedIcon : _uncheckedIcon);
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

		string newValue = _treeItemToTag[item];

		if (newValue == _currentValue)
		{
			newValue = string.Empty;
		}

		EmitChanged(GetEditedProperty(), newValue);
	}

	private void OnToggled(bool toggled)
	{
		if (_scroll is null || _searchBar is null || !IsInstanceValid(_scroll) || !IsInstanceValid(_searchBar))
		{
			return;
		}

		_scroll.Visible = toggled;
		_searchBar.Visible = toggled;

		UpdateMinimumSize();
		NotifyPropertyListChanged();
	}

	private void OnFilterChanged()
	{
		RebuildTree();
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

		_treeItemToTag.Clear();
		_root = null;
		_containerButton = null;
		_searchBar = null;
		_scroll = null;
		_tree = null;
		_checkedIcon = null;
		_uncheckedIcon = null;
	}

	private void FreeAllChildren()
	{
		for (int i = GetChildCount() - 1; i >= 0; i--)
		{
			Node child = GetChild(i);
			RemoveChild(child);
			child.Free();
		}
	}
}
#endif
