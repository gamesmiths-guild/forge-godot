// Copyright © Gamesmiths Guild.

#if TOOLS
using System.Collections.Generic;
using Gamesmiths.Forge.Godot.Core;
using Gamesmiths.Forge.Tags;
using Godot;

using GodotStringArray = Godot.Collections.Array<string>;

namespace Gamesmiths.Forge.Godot.Editor.Tags;

[Tool]
public partial class TagContainerEditorProperty : EditorProperty, ISerializationListener
{
	private readonly Dictionary<TreeItem, TagNode> _treeItemToNode = [];

	private VBoxContainer? _root;
	private Button? _containerButton;
	private ScrollContainer? _scroll;
	private Tree? _tree;

	private Texture2D? _checkedIcon;
	private Texture2D? _uncheckedIcon;

	private GodotStringArray _currentValue = [];

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
		_root.AddChild(_scroll);

		AddChild(_root);
		SetBottomEditor(_root);

		_checkedIcon = EditorInterface.Singleton
			.GetEditorTheme()
			.GetIcon("GuiChecked", "EditorIcons");

		_uncheckedIcon = EditorInterface.Singleton
			.GetEditorTheme()
			.GetIcon("GuiUnchecked", "EditorIcons");

		_tree.ButtonClicked += OnTreeButtonClicked;
	}

	public override void _UpdateProperty()
	{
		if (_tree is null || _containerButton is null || !IsInstanceValid(_tree) || !IsInstanceValid(_containerButton))
		{
			return;
		}

		GodotObject obj = GetEditedObject();
		string propertyName = GetEditedProperty();

		_currentValue = obj.Get(propertyName).AsGodotArray<string>() ?? [];

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
		if (_tree is null || _containerButton is null || _checkedIcon is null || _uncheckedIcon is null)
		{
			return;
		}

		_tree.Clear();
		_treeItemToNode.Clear();

		_containerButton.Text = $"Container (size: {_currentValue.Count})";

		TreeItem root = _tree.CreateItem();

		ForgeData forgePluginData = ResourceLoader.Load<ForgeData>(ForgeData.ForgeDataResourcePath);

		var tagsManager = new TagsManager([.. forgePluginData.RegisteredTags]);

		BuildTreeRecursive(root, tagsManager.RootNode);

		UpdateMinimumSize();
		NotifyPropertyListChanged();
	}

	private void BuildTreeRecursive(TreeItem parent, TagNode node)
	{
		if (_tree is null)
		{
			return;
		}

		foreach (TagNode child in node.ChildTags)
		{
			TreeItem item = _tree.CreateItem(parent);
			item.SetText(0, child.TagKey);

			bool checkedState = _currentValue.Contains(child.CompleteTagKey);

			item.AddButton(0, checkedState ? _checkedIcon : _uncheckedIcon);

			_treeItemToNode[item] = child;
			BuildTreeRecursive(item, child);
		}
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

		string tag = _treeItemToNode[item].CompleteTagKey;

		var newValue = new GodotStringArray();
		newValue.AddRange(_currentValue);

		if (!newValue.Remove(tag))
		{
			newValue.Add(tag);
		}

		EmitChanged(GetEditedProperty(), newValue);
	}

	private void OnToggled(bool toggled)
	{
		if (_scroll is null || !IsInstanceValid(_scroll))
		{
			return;
		}

		_scroll.Visible = toggled;

		UpdateMinimumSize();
		NotifyPropertyListChanged();
	}

	private void ReleaseUiState()
	{
		if (_containerButton is not null && IsInstanceValid(_containerButton))
		{
			_containerButton.Toggled -= OnToggled;
		}

		if (_tree is not null && IsInstanceValid(_tree))
		{
			_tree.ButtonClicked -= OnTreeButtonClicked;
		}

		_treeItemToNode.Clear();
		_root = null;
		_containerButton = null;
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
