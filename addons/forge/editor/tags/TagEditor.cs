// Copyright © Gamesmiths Guild.

#if TOOLS
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Godot.Core;
using Gamesmiths.Forge.Tags;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Tags;

[Tool]
public partial class TagEditor : VBoxContainer
{
	private readonly Dictionary<TreeItem, TagNode> _treeItemToNode = [];

	private Button? _containerButton;
	private Tree? _tree;
	private Texture2D? _checkedIcon;
	private Texture2D? _uncheckedIcon;

	[Signal]
	public delegate void TagChangedEventHandler(string tag);

	public bool IsPluginInstance { get; set; }

	public string? Tag { get; set; }

	public override void _Ready()
	{
		base._Ready();

		if (!IsPluginInstance)
		{
			return;
		}

		ForgeData forgePluginData = ResourceLoader.Load<ForgeData>("uid://8j4xg16o3qnl");
		var tagsManager = new TagsManager([.. forgePluginData.RegisteredTags]);

		_containerButton = GetNode<Button>("%ContainerButton");
		_containerButton.ToggleMode = true;
		_containerButton.Toggled += OnButtonToggled;

		_tree = GetNode<Tree>("%Tree");
		_checkedIcon = EditorInterface.Singleton.GetEditorTheme().GetIcon("GuiRadioChecked", "EditorIcons");
		_uncheckedIcon = EditorInterface.Singleton.GetEditorTheme().GetIcon("GuiRadioUnchecked", "EditorIcons");

		TreeItem rootTreeNode = _tree.CreateItem();
		_tree.HideRoot = true;
		_tree.Visible = false;
		_tree.CustomMinimumSize += new Vector2(0, 12);

		if (tagsManager.RootNode.ChildTags.Count == 0)
		{
			AddEmptyTagsWarning(_tree, rootTreeNode);
			_containerButton.Text = "(none)";
			return;
		}

		Tag ??= string.Empty;
		ValidateTags(tagsManager);

		_containerButton.Text = !string.IsNullOrEmpty(Tag) ? Tag : "(none)";

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
			if (Tag.Equals(itemName, System.StringComparison.Ordinal))
			{
				item.SetButton((int)column, (int)id, _uncheckedIcon);
				Tag = string.Empty;
				_containerButton.Text = "(none)";
			}
			else
			{
				item.SetButton((int)column, (int)id, _checkedIcon);
				Tag = itemName;
				_containerButton.Text = Tag;
				CleanRadioButtons();
			}

			EmitSignal(SignalName.TagChanged, Tag);
			NotifyPropertyListChanged();
		}
	}

	private void CleanRadioButtons()
	{
		EnsureInitialized();

		foreach (KeyValuePair<TreeItem, TagNode> kvp in _treeItemToNode)
		{
			TreeItem item = kvp.Key;
			TagNode node = kvp.Value;

			if (!Tag.Equals(node.CompleteTagKey, System.StringComparison.Ordinal))
			{
				item.SetButton(0, 0, _uncheckedIcon);
			}
		}
	}

	private void BuildTreeRecursively(Tree tree, TreeItem currentTreeItem, TagNode currentNode)
	{
		EnsureInitialized();

		foreach (TagNode childTagNode in currentNode.ChildTags)
		{
			TreeItem childTreeNode = tree.CreateItem(currentTreeItem);
			childTreeNode.SetText(0, childTagNode.TagKey);

			if (Tag.Equals(childTagNode.CompleteTagKey, System.StringComparison.Ordinal))
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

		var tag = Tag;

		try
		{
			Forge.Tags.Tag.RequestTag(tagsManager, tag);
		}
		catch (TagNotRegisteredException)
		{
			_containerButton.Text = "(none)";
		}
	}

	[MemberNotNull(nameof(_tree), nameof(_containerButton), nameof(Tag))]
	private void EnsureInitialized()
	{
		Debug.Assert(_tree is not null, $"{_tree} should have been initialized on _Ready().");
		Debug.Assert(_containerButton is not null, $"{_containerButton} should have been initialized on _Ready().");
		Debug.Assert(Tag is not null, $"{Tag} should have been initialized on _Ready().");
	}
}
#endif
