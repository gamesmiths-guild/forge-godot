// Copyright © 2025 Gamesmiths Guild.

#if TOOLS
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Godot;
using static Gamesmiths.Forge.Godot.Forge;

namespace Gamesmiths.Forge.GameplayTags.Godot;

[Tool]
public partial class GameplayTagsUI : VBoxContainer
{
	private readonly Dictionary<TreeItem, GameplayTagNode> _treeItemToNode = [];

	private ForgePluginData? _forgePluginData;

	private Tree? _tree;
	private LineEdit? _tagNameTextField;
	private Button? _addTagButton;

	private Texture2D? _addIcon;
	private Texture2D? _removeIcon;

	public bool IsPluginInstance { get; set; }

	public override void _Ready()
	{
		base._Ready();

		if (!IsPluginInstance)
		{
			return;
		}

		_forgePluginData = ResourceLoader.Load<ForgePluginData>("uid://8j4xg16o3qnl");

		_addIcon = EditorInterface.Singleton.GetEditorTheme().GetIcon("Add", "EditorIcons");
		_removeIcon = EditorInterface.Singleton.GetEditorTheme().GetIcon("Remove", "EditorIcons");

		_tree = GetNode<Tree>("%Tree");
		_tagNameTextField = GetNode<LineEdit>("%TagNameField");
		_addTagButton = GetNode<Button>("%AddTagButton");

		ConstructTagTree();

		_tree.ButtonClicked += TreeButtonClicked;
		_addTagButton.Pressed += AddTagButton_Pressed;
	}

	private void AddTagButton_Pressed()
	{
		EnsureInitialized();
		Debug.Assert(
			_forgePluginData.RegisteredTags is not null,
			$"{_forgePluginData.RegisteredTags} should have been initialized by the Forge plugin.");

		if (!GameplayTag.IsValidKey(_tagNameTextField.Text, out var _, out var fixedTag))
		{
			_tagNameTextField.Text = fixedTag;
		}

		if (_forgePluginData.RegisteredTags.Contains(_tagNameTextField.Text))
		{
			GD.PushWarning($"Tag [{_tagNameTextField.Text}] is already present in the manager.");
			return;
		}

		_forgePluginData.RegisteredTags.Add(_tagNameTextField.Text);
		ResourceSaver.Save(_forgePluginData);

		ReconstructTreeNode();
	}

	private void ReconstructTreeNode()
	{
		EnsureInitialized();
		Debug.Assert(
			_forgePluginData.RegisteredTags is not null,
			$"{_forgePluginData.RegisteredTags} should have been initialized by the Forge plugin.");

		TagsManager?.DestroyTagTree();
		TagsManager = new GameplayTagsManager([.. _forgePluginData.RegisteredTags]);

		_tree.Clear();
		ConstructTagTree();
	}

	private void ConstructTagTree()
	{
		EnsureInitialized();
		Debug.Assert(TagsManager is not null, $"{TagsManager} should have been initialized by the Forge plugin.");

		TreeItem rootTreeNode = _tree.CreateItem();
		_tree.HideRoot = true;

		if (TagsManager.RootNode.ChildTags.Count == 0)
		{
			TreeItem childTreeNode = _tree.CreateItem(rootTreeNode);
			childTreeNode.SetText(0, "No tag has been registered yet.");
			childTreeNode.SetCustomColor(0, Color.FromHtml("EED202"));
			return;
		}

		BuildTreeRecursively(_tree, rootTreeNode, TagsManager.RootNode);
	}

	private void BuildTreeRecursively(Tree tree, TreeItem currentTreeItem, GameplayTagNode currentNode)
	{
		foreach (GameplayTagNode childTagNode in currentNode.ChildTags)
		{
			TreeItem childTreeNode = tree.CreateItem(currentTreeItem);
			childTreeNode.SetText(0, childTagNode.TagKey);
			childTreeNode.AddButton(0, _addIcon);
			childTreeNode.AddButton(0, _removeIcon);

			_treeItemToNode.Add(childTreeNode, childTagNode);

			BuildTreeRecursively(tree, childTreeNode, childTagNode);
		}
	}

	private void TreeButtonClicked(TreeItem item, long column, long id, long mouseButtonIndex)
	{
		EnsureInitialized();
		Debug.Assert(
			_forgePluginData.RegisteredTags is not null,
			$"{_forgePluginData.RegisteredTags} should have been initialized by the Forge plugin.");

		if (mouseButtonIndex == 1)
		{
			if (id == 0)
			{
				_tagNameTextField.Text = $"{_treeItemToNode[item].CompleteTagKey}.";
				_tagNameTextField.GrabFocus();
				_tagNameTextField.CaretColumn = _tagNameTextField.Text.Length;
			}

			if (id == 1)
			{
				GameplayTagNode selectedTag = _treeItemToNode[item];

				for (var i = _forgePluginData.RegisteredTags.Count - 1; i >= 0; i--)
				{
					var tag = _forgePluginData.RegisteredTags[i];

					if (tag.StartsWith(selectedTag.CompleteTagKey, System.StringComparison.InvariantCultureIgnoreCase))
					{
						_forgePluginData.RegisteredTags.Remove(tag);
					}
				}

				if (selectedTag.ParentTagNode is not null
					&& !_forgePluginData.RegisteredTags.Contains(selectedTag.ParentTagNode.CompleteTagKey))
				{
					_forgePluginData.RegisteredTags.Add(selectedTag.ParentTagNode.CompleteTagKey);
				}

				ResourceSaver.Save(_forgePluginData);
				ReconstructTreeNode();
			}
		}
	}

	[MemberNotNull(nameof(_tree), nameof(_tagNameTextField), nameof(_forgePluginData))]
	private void EnsureInitialized()
	{
		Debug.Assert(_tree is not null, $"{_tree} should have been initialized on _Ready().");
		Debug.Assert(_tagNameTextField is not null, $"{_tagNameTextField} should have been initialized on _Ready().");
		Debug.Assert(_forgePluginData is not null, $"{_forgePluginData} should have been initialized on _Ready().");
	}
}
#endif
