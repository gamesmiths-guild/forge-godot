// Copyright © Gamesmiths Guild.

#if TOOLS
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Godot.Core;
using Gamesmiths.Forge.Tags;
using Godot;

using GodotStringArray = Godot.Collections.Array<string>;

namespace Gamesmiths.Forge.Godot.Editor.Tags;

[Tool]
public partial class TagContainerEditor : VBoxContainer
{
	private readonly Dictionary<TreeItem, TagNode> _treeItemToNode = [];

	private Button? _containerButton;
	private Tree? _tree;
	private Texture2D? _checkedIcon;
	private Texture2D? _uncheckedIcon;

	public bool IsPluginInstance { get; set; }

	public GodotStringArray? ContainerTags { get; set; }

	public override void _Ready()
	{
		base._Ready();

		if (!IsPluginInstance)
		{
			return;
		}

		ForgeData forgePluginData = ResourceLoader.Load<ForgeData>("uid://8j4xg16o3qnl");
		var tagsManager = new TagsManager([.. forgePluginData.RegisteredTags]);

		ContainerTags ??= [];

		_containerButton = GetNode<Button>("%ContainerButton");

		_containerButton.ToggleMode = true;
		_containerButton.Toggled += OnButtonToggled;
		_containerButton.Text = $"Container (size: {ContainerTags.Count})";

		_tree = GetNode<Tree>("%Tree");

		_checkedIcon = EditorInterface.Singleton.GetEditorTheme().GetIcon("GuiChecked", "EditorIcons");
		_uncheckedIcon = EditorInterface.Singleton.GetEditorTheme().GetIcon("GuiUnchecked", "EditorIcons");

		TreeItem rootTreeNode = _tree.CreateItem();
		_tree.HideRoot = true;
		_tree.Visible = false;
		_tree.CustomMinimumSize += new Vector2(0, 12);

		if (tagsManager.RootNode.ChildTags.Count == 0)
		{
			AddEmptyTagsWarning(_tree, rootTreeNode);
			return;
		}

		ValidateTags(tagsManager);

		BuildTreeRecursively(_tree, rootTreeNode, tagsManager.RootNode);

		_tree.ItemCollapsed += TreeItemCollapsed;
		_tree.ButtonClicked += TreeButtonClicked;
	}

	private static void TotalChilds(TreeItem item, ref int childCount)
	{
		foreach (TreeItem child in item.GetChildren())
		{
			childCount++;
			TotalChilds(child, ref childCount);
		}
	}

	private void OnButtonToggled(bool toggledOn)
	{
		EnsureInitialized();

		_tree.Visible = toggledOn;
	}

	private void TreeButtonClicked(TreeItem item, long column, long id, long mouseButtonIndex)
	{
		EnsureInitialized();

		StringKey itemName = _treeItemToNode[item].CompleteTagKey;

		if (mouseButtonIndex == 1 && id == 0)
		{
			if (ContainerTags.Contains(itemName))
			{
				item.SetButton((int)column, (int)id, _uncheckedIcon);
				ContainerTags.Remove(itemName);
			}
			else
			{
				item.SetButton((int)column, (int)id, _checkedIcon);
				ContainerTags.Add(itemName);
			}

			_containerButton.Text = $"Container (size: {ContainerTags.Count})";

			NotifyPropertyListChanged();
		}
	}

	private void BuildTreeRecursively(Tree tree, TreeItem currentTreeItem, TagNode currentNode)
	{
		EnsureInitialized();

		foreach (TagNode childTagNode in currentNode.ChildTags)
		{
			TreeItem childTreeNode = tree.CreateItem(currentTreeItem);
			childTreeNode.SetText(0, childTagNode.TagKey);

			if (ContainerTags.Contains(childTagNode.CompleteTagKey))
			{
				childTreeNode.AddButton(0, _checkedIcon);
			}
			else
			{
				childTreeNode.AddButton(0, _uncheckedIcon);
			}

			_tree.CustomMinimumSize += new Vector2(0, 29);

			_treeItemToNode.Add(childTreeNode, childTagNode);

			BuildTreeRecursively(tree, childTreeNode, childTagNode);
		}
	}

	private void AddEmptyTagsWarning(Tree tree, TreeItem currentTreeItem)
	{
		EnsureInitialized();

		TreeItem childTreeNode = tree.CreateItem(currentTreeItem);
		childTreeNode.SetText(0, "No tag has been registered yet.");
		childTreeNode.SetCustomColor(0, Color.FromHtml("EED202"));

		_tree.CustomMinimumSize += new Vector2(0, 29);
	}

	private void TreeItemCollapsed(TreeItem item)
	{
		EnsureInitialized();

		var childCount = 0;
		TotalChilds(item, ref childCount);

		if (item.Collapsed)
		{
			_tree.CustomMinimumSize -= new Vector2(0, childCount * 28);
		}
		else
		{
			_tree.CustomMinimumSize += new Vector2(0, childCount * 28);
		}
	}

	private void ValidateTags(TagsManager tagsManager)
	{
		EnsureInitialized();

		for (var i = ContainerTags.Count - 1; i >= 0; i--)
		{
			var tag = ContainerTags[i];

			try
			{
				Tag.RequestTag(tagsManager, tag);
			}
			catch (TagNotRegisteredException)
			{
				ContainerTags.Remove(tag);
				_containerButton.Text = $"Container (size: {ContainerTags.Count})";
			}
		}
	}

	[MemberNotNull(nameof(_tree), nameof(_containerButton), nameof(ContainerTags))]
	private void EnsureInitialized()
	{
		Debug.Assert(_tree is not null, $"{_tree} should have been initialized on _Ready().");
		Debug.Assert(_containerButton is not null, $"{_containerButton} should have been initialized on _Ready().");
		Debug.Assert(ContainerTags is not null, $"{ContainerTags} should have been initialized on _Ready().");
	}
}
#endif
