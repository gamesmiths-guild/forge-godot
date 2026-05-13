// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using System.Collections.Generic;
using Gamesmiths.Forge.Godot.Core;
using Gamesmiths.Forge.Tags;
using Godot;

using GodotStringArray = Godot.Collections.Array<string>;

namespace Gamesmiths.Forge.Godot.Editor.Tags;

[Tool]
public partial class TagContainerSelectionControl : VBoxContainer
{
	private readonly Dictionary<TreeItem, TagNode> _treeItemToNode = [];

	private Button? _containerButton;
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

		RebuildTree();
	}

	public override void _ExitTree()
	{
		if (_containerButton is not null && IsInstanceValid(_containerButton))
		{
			_containerButton.Toggled -= OnToggled;
		}

		if (_tree is not null && IsInstanceValid(_tree))
		{
			_tree.ButtonClicked -= OnTreeButtonClicked;
		}

		ValueChanged = null;
		_treeItemToNode.Clear();
		_containerButton = null;
		_scroll = null;
		_tree = null;
		_checkedIcon = null;
		_uncheckedIcon = null;
		base._ExitTree();
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

	private void RebuildTree()
	{
		if (_tree is null || _containerButton is null || _checkedIcon is null || _uncheckedIcon is null)
		{
			return;
		}

		_tree.Clear();
		_treeItemToNode.Clear();
		_containerButton.Text = $"Container (size: {_currentValue.Count})";

		ForgeData forgePluginData = ResourceLoader.Load<ForgeData>(ForgeData.ForgeDataResourcePath);
		var tagsManager = new TagsManager([.. forgePluginData.RegisteredTags]);
		TreeItem root = _tree.CreateItem();
		BuildTreeRecursive(root, tagsManager.RootNode);
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
			item.AddButton(0, _currentValue.Contains(child.CompleteTagKey) ? _checkedIcon : _uncheckedIcon);
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

		SetValue(newValue);
		ValueChanged?.Invoke(newValue);
	}

	private void OnToggled(bool toggled)
	{
		if (_scroll is null || !IsInstanceValid(_scroll))
		{
			return;
		}

		_scroll.Visible = toggled;
	}
}
#endif
