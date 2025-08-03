// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Gamesmiths.Forge.Godot.Core;
using Gamesmiths.Forge.Tags;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Tags;

[Tool]
public partial class TagsEditor : VBoxContainer, ISerializationListener
{
	private readonly Dictionary<TreeItem, TagNode> _treeItemToNode = [];

	private TagsManager _tagsManager = null!;

	private ForgeData? _forgePluginData;

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

		_forgePluginData = ResourceLoader.Load<ForgeData>("uid://8j4xg16o3qnl");
		_tagsManager = new TagsManager([.. _forgePluginData.RegisteredTags]);

		_addIcon = EditorInterface.Singleton.GetEditorTheme().GetIcon("Add", "EditorIcons");
		_removeIcon = EditorInterface.Singleton.GetEditorTheme().GetIcon("Remove", "EditorIcons");

		_tree = GetNode<Tree>("%Tree");
		_tagNameTextField = GetNode<LineEdit>("%TagNameField");
		_addTagButton = GetNode<Button>("%AddTagButton");

		ConstructTagTree();

		_tree.ButtonClicked += TreeButtonClicked;
		_addTagButton.Pressed += AddTagButton_Pressed;
	}

	public void OnBeforeSerialize()
	{
		// This method was intentionally left empty.
	}

	public void OnAfterDeserialize()
	{
		EnsureInitialized();

		_tagsManager = new TagsManager([.. _forgePluginData.RegisteredTags]);
		ReconstructTreeNode();
	}

	private void AddTagButton_Pressed()
	{
		EnsureInitialized();
		Debug.Assert(
			_forgePluginData.RegisteredTags is not null,
			$"{_forgePluginData.RegisteredTags} should have been initialized by the Forge plugin.");

		if (!Tag.IsValidKey(_tagNameTextField.Text, out var _, out var fixedTag))
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

		_tagsManager.DestroyTagTree();
		_tagsManager = new TagsManager([.. _forgePluginData.RegisteredTags]);

		_tree.Clear();
		ConstructTagTree();
	}

	private void ConstructTagTree()
	{
		EnsureInitialized();

		TreeItem rootTreeNode = _tree.CreateItem();
		_tree.HideRoot = true;

		if (_tagsManager.RootNode.ChildTags.Count == 0)
		{
			TreeItem childTreeNode = _tree.CreateItem(rootTreeNode);
			childTreeNode.SetText(0, "No tag has been registered yet.");
			childTreeNode.SetCustomColor(0, Color.FromHtml("EED202"));
			return;
		}

		BuildTreeRecursively(_tree, rootTreeNode, _tagsManager.RootNode);
	}

	private void BuildTreeRecursively(Tree tree, TreeItem currentTreeItem, TagNode currentNode)
	{
		foreach (TagNode childTagNode in currentNode.ChildTags)
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
				TagNode selectedTag = _treeItemToNode[item];

				for (var i = _forgePluginData.RegisteredTags.Count - 1; i >= 0; i--)
				{
					var tag = _forgePluginData.RegisteredTags[i];

					if (string.Equals(tag, selectedTag.CompleteTagKey, StringComparison.OrdinalIgnoreCase) ||
						tag.StartsWith(selectedTag.CompleteTagKey + ".", StringComparison.InvariantCultureIgnoreCase))
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
