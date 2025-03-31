// Copyright © 2025 Gamesmiths Guild.

#if TOOLS
using System.Collections.Generic;
using Gamesmiths.Forge.GameplayTags;
using Godot;

namespace Gamesmiths.Forge.Godot.GameplayTags;

[Tool]
public partial class GameplayTagsUI : VBoxContainer
{
	private readonly Dictionary<TreeItem, GameplayTagNode> _treeItemToNode = [];

	private RegisteredTags _registeredTags;

	private Tree _tree;
	private LineEdit _tagNameTextField;
	private Button _addTagButton;

	private Texture2D _addIcon;
	private Texture2D _removeIcon;

	public bool IsPluginInstance { get; set; }

	public override void _Ready()
	{
		base._Ready();

		if (!IsPluginInstance)
		{
			return;
		}

		_registeredTags = ResourceLoader.Load<RegisteredTags>("res://addons/forge/gameplay_tags/registered_tags.tres");

		GD.Print("Initialize Tag Tree");
		Forge.TagsManager = new GameplayTagsManager([.. _registeredTags.Tags]);

		_addIcon = EditorInterface.Singleton.GetEditorTheme().GetIcon("Add", "EditorIcons");
		_removeIcon = EditorInterface.Singleton.GetEditorTheme().GetIcon("Remove", "EditorIcons");

		_tree = GetNode<Tree>("%Tree");
		_tagNameTextField = GetNode<LineEdit>("%TagNameField");
		_addTagButton = GetNode<Button>("%AddTagButton");

		TreeItem rootTreeNode = _tree.CreateItem();
		_tree.HideRoot = true;
		ConstructTreeNode(_tree, rootTreeNode, Forge.TagsManager.RootNode);

		_tree.ButtonClicked += TreeButtonClicked;
		_addTagButton.Pressed += AddTagButton_Pressed;
	}

	public override void _ExitTree()
	{
		base._ExitTree();

		if (!IsPluginInstance)
		{
			return;
		}

		Forge.TagsManager.DestroyTagTree();
	}

	private void AddTagButton_Pressed()
	{
		if (!GameplayTag.IsValidKey(_tagNameTextField.Text, out var _, out var fixedTag))
		{
			_tagNameTextField.Text = fixedTag;
		}

		_registeredTags.Tags.Add(_tagNameTextField.Text);

		ReconstructTreeNode();
	}

	private void ReconstructTreeNode()
	{
		Forge.TagsManager.DestroyTagTree();
		Forge.TagsManager = new GameplayTagsManager([.. _registeredTags.Tags]);

		_tree.Clear();
		TreeItem rootTreeNode = _tree.CreateItem();
		_tree.HideRoot = true;
		ConstructTreeNode(_tree, rootTreeNode, Forge.TagsManager.RootNode);
	}

	private void ConstructTreeNode(Tree tree, TreeItem currentTreeItem, GameplayTagNode currentNode)
	{
		foreach (GameplayTagNode childTagNode in currentNode.ChildTags)
		{
			TreeItem childTreeNode = tree.CreateItem(currentTreeItem);
			childTreeNode.SetText(0, childTagNode.TagKey);
			childTreeNode.AddButton(0, _addIcon);
			childTreeNode.AddButton(0, _removeIcon);

			_treeItemToNode.Add(childTreeNode, childTagNode);

			ConstructTreeNode(tree, childTreeNode, childTagNode);
		}
	}

	private void TreeButtonClicked(TreeItem item, long column, long id, long mouseButtonIndex)
	{
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

				for (var i = _registeredTags.Tags.Count - 1; i >= 0; i--)
				{
					var tag = _registeredTags.Tags[i];

					if (tag.StartsWith(selectedTag.CompleteTagKey, System.StringComparison.InvariantCultureIgnoreCase))
					{
						_registeredTags.Tags.Remove(tag);
					}
				}

				if (selectedTag.ParentTagNode is not null)
				{
					_registeredTags.Tags.Add(selectedTag.ParentTagNode.CompleteTagKey);
				}

				ReconstructTreeNode();
			}
		}
	}
}
#endif
