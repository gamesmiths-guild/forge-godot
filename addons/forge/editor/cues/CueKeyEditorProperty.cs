// Copyright © Gamesmiths Guild.

#if TOOLS
using System.Collections.Generic;
using Gamesmiths.Forge.Godot.Editor.Tags;
using Gamesmiths.Forge.Tags;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Cues;

[Tool]
public partial class CueKeyEditorProperty : EditorProperty, ISerializationListener
{
	private const int ButtonSize = 26;
	private const int PopupSize = 300;

	private readonly Dictionary<TreeItem, string> _treeItemToTag = [];

	private Label? _label;
	private Button? _button;
	private Popup? _popup;
	private TagTreeSearchBar? _searchBar;
	private Tree? _tree;

	public override void _Ready()
	{
		Texture2D dropdownIcon = EditorInterface.Singleton
			.GetEditorTheme()
			.GetIcon("GuiDropdown", "EditorIcons");

		var hbox = new HBoxContainer();
		_label = new Label { Text = "None", SizeFlagsHorizontal = SizeFlags.ExpandFill };
		_button = new Button { Icon = dropdownIcon, CustomMinimumSize = new Vector2(ButtonSize, 0) };

		hbox.AddChild(_label);
		hbox.AddChild(_button);
		AddChild(hbox);

		_popup = new Popup { Size = new Vector2I(PopupSize, PopupSize) };

		var popupBox = new VBoxContainer
		{
			AnchorRight = 1,
			AnchorBottom = 1,
		};

		_searchBar = new TagTreeSearchBar();

		_tree = new Tree
		{
			HideRoot = true,
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			SizeFlagsVertical = SizeFlags.ExpandFill,
		};

		popupBox.AddChild(_searchBar);
		popupBox.AddChild(_tree);
		_popup.AddChild(popupBox);

		var backgroundStyle = new StyleBoxFlat
		{
			BgColor = EditorInterface.Singleton.GetEditorTheme().GetColor("base_color", "Editor"),
		};
		_tree.AddThemeStyleboxOverride("panel", backgroundStyle);
		_popup.AddThemeStyleboxOverride("panel", backgroundStyle);

		AddChild(_popup);

		RebuildTree();

		_button.Pressed += OnButtonPressed;
		_tree.ItemActivated += OnTreeItemActivated;
		_searchBar.FilterChanged += OnRegisteredTagsChanged;
		ForgeTagsRegistry.Changed += OnRegisteredTagsChanged;
	}

	public override void _UpdateProperty()
	{
		string property = GetEditedObject().Get(GetEditedProperty()).AsString();

		if (_label is not null && IsInstanceValid(_label))
		{
			_label.Text = string.IsNullOrEmpty(property) ? "None" : property;
		}
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

	private static void CollapseRow(TreeItem item, string completeTagKey)
	{
		item.Collapsed = true;
	}

	private void RebuildTree()
	{
		if (_tree is null || !IsInstanceValid(_tree))
		{
			return;
		}

		_tree.Clear();
		_treeItemToTag.Clear();

		_searchBar?.RefreshSources();

		TagsManager tags = _searchBar?.ResolveTags() ?? ForgeTagsRegistry.MergedTags;
		TreeItem root = _tree.CreateItem();

		TagSourceTreeBuilder.Build(
			_tree,
			root,
			tags.RootNode,
			_searchBar?.ResolveFilter(tags),
			CollapseRow,
			_treeItemToTag);
	}

	private void OnRegisteredTagsChanged()
	{
		RebuildTree();
	}

	private void OnButtonPressed()
	{
		if (_button is null || _popup is null || !IsInstanceValid(_button) || !IsInstanceValid(_popup))
		{
			return;
		}

		Window win = GetWindow();
		_popup.Position = (Vector2I)_button.GlobalPosition
			+ win.Position
			- new Vector2I(PopupSize - ButtonSize, -30);
		_popup.Popup();
	}

	private void OnTreeItemActivated()
	{
		if (_tree is null || _popup is null || _label is null
			|| !IsInstanceValid(_tree) || !IsInstanceValid(_popup) || !IsInstanceValid(_label))
		{
			return;
		}

		TreeItem item = _tree.GetSelected();

		if (item is null || !_treeItemToTag.TryGetValue(item, out string? cueKey))
		{
			return;
		}

		_label.Text = cueKey;
		EmitChanged(GetEditedProperty(), cueKey);
		_popup.Hide();
	}

	private void ReleaseUiState()
	{
		ForgeTagsRegistry.Changed -= OnRegisteredTagsChanged;

		if (_button is not null && IsInstanceValid(_button))
		{
			_button.Pressed -= OnButtonPressed;
		}

		if (_tree is not null && IsInstanceValid(_tree))
		{
			_tree.ItemActivated -= OnTreeItemActivated;
		}

		if (_searchBar is not null && IsInstanceValid(_searchBar))
		{
			_searchBar.FilterChanged -= OnRegisteredTagsChanged;
		}

		_treeItemToTag.Clear();
		_label = null;
		_button = null;
		_popup = null;
		_searchBar = null;
		_tree = null;
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
