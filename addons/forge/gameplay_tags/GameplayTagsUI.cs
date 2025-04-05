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
		if (!GameplayTag.IsValidKey(_tagNameTextField.Text, out var _, out var fixedTag))
		{
			_tagNameTextField.Text = fixedTag;
		}

		if (_registeredTags.Tags.Contains(_tagNameTextField.Text))
		{
			GD.PushWarning($"Tag [{_tagNameTextField.Text}] is already present in the manager.");
			return;
		}

		_registeredTags.Tags.Add(_tagNameTextField.Text);
		ResourceSaver.Save(_registeredTags);

		ReconstructTreeNode();
	}

	private void ReconstructTreeNode()
	{
		Forge.TagsManager?.DestroyTagTree();
		Forge.TagsManager = new GameplayTagsManager([.. _registeredTags.Tags]);

		_tree.Clear();
		ConstructTagTree();
	}

	private void ConstructTagTree()
	{
		TreeItem rootTreeNode = _tree.CreateItem();
		_tree.HideRoot = true;

		if (Forge.TagsManager.RootNode.ChildTags.Count == 0)
		{
			TreeItem childTreeNode = _tree.CreateItem(rootTreeNode);
			childTreeNode.SetText(0, "No tag has been registered yet.");
			childTreeNode.SetCustomColor(0, Color.FromHtml("EED202"));
			return;
		}

		BuildTreeRecursively(_tree, rootTreeNode, Forge.TagsManager.RootNode);
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

				if (selectedTag.ParentTagNode is not null
					&& !_registeredTags.Tags.Contains(selectedTag.ParentTagNode.CompleteTagKey))
				{
					_registeredTags.Tags.Add(selectedTag.ParentTagNode.CompleteTagKey);
				}

				ResourceSaver.Save(_registeredTags);
				ReconstructTreeNode();
			}
		}
	}
}
#endif
