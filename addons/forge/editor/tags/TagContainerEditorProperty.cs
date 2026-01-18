// Copyright © Gamesmiths Guild.

#if TOOLS
using System.Collections.Generic;
using Gamesmiths.Forge.Godot.Core;
using Gamesmiths.Forge.Tags;
using Godot;

using GodotStringArray = Godot.Collections.Array<string>;

namespace Gamesmiths.Forge.Godot.Editor.Tags;

[Tool]
public partial class TagContainerEditorProperty : EditorProperty
{
	private readonly Dictionary<TreeItem, TagNode> _treeItemToNode = [];

	private VBoxContainer _root = null!;
	private Button _containerButton = null!;
	private ScrollContainer _scroll = null!;
	private Tree _tree = null!;

	private Texture2D _checkedIcon = null!;
	private Texture2D _uncheckedIcon = null!;

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
		GodotObject obj = GetEditedObject();
		string propertyName = GetEditedProperty();

		_currentValue =
			obj.Get(propertyName).AsGodotArray<string>() ?? [];

		RebuildTree();
	}

	private void RebuildTree()
	{
		_tree.Clear();
		_treeItemToNode.Clear();

		_containerButton.Text =
			$"Container (size: {_currentValue.Count})";

		TreeItem root = _tree.CreateItem();

		ForgeData forgePluginData =
			ResourceLoader.Load<ForgeData>("uid://8j4xg16o3qnl");

		var tagsManager =
			new TagsManager([.. forgePluginData.RegisteredTags]);

		BuildTreeRecursive(root, tagsManager.RootNode);

		UpdateMinimumSize();
		NotifyPropertyListChanged();
	}

	private void BuildTreeRecursive(TreeItem parent, TagNode node)
	{
		foreach (TagNode child in node.ChildTags)
		{
			TreeItem item = _tree.CreateItem(parent);
			item.SetText(0, child.TagKey);

			var checkedState =
				_currentValue.Contains(child.CompleteTagKey);

			item.AddButton(
				0,
				checkedState ? _checkedIcon : _uncheckedIcon);

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
		_scroll.Visible = toggled;

		UpdateMinimumSize();
		NotifyPropertyListChanged();
	}
}
#endif
