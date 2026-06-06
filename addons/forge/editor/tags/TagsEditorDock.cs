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

/// <summary>
/// Editor dock for managing gameplay tags.
/// </summary>
[Tool]
public partial class TagsEditorDock : EditorDock, ISerializationListener
{
	private readonly Dictionary<TreeItem, TagNode> _treeItemToNode = [];

	private TagsManager? _tagsManager;

	private ForgeData? _forgePluginData;

	private EditorUndoRedoManager? _undoRedo;

	private Tree? _tree;
	private LineEdit? _tagNameTextField;
	private Button? _addTagButton;

	private Texture2D? _addIcon;
	private Texture2D? _removeIcon;

	public TagsEditorDock()
	{
		Title = "Tags";
		DockIcon = GD.Load<Texture2D>("uid://cu6ncpuumjo20");
		DefaultSlot = DockSlot.RightUl;
	}

	/// <summary>
	/// Sets the <see cref="EditorUndoRedoManager"/> used for undo/redo support.
	/// </summary>
	/// <param name="undoRedo">The undo/redo manager from the editor plugin.</param>
	public void SetUndoRedo(EditorUndoRedoManager undoRedo)
	{
		_undoRedo = undoRedo;
	}

	public override void _Ready()
	{
		base._Ready();

		_forgePluginData = ResourceLoader.Load<ForgeData>(ForgeData.ForgeDataResourcePath);
		_tagsManager = new TagsManager([.. _forgePluginData.RegisteredTags]);

		_addIcon = EditorInterface.Singleton.GetEditorTheme().GetIcon("Add", "EditorIcons");
		_removeIcon = EditorInterface.Singleton.GetEditorTheme().GetIcon("Remove", "EditorIcons");

		BuildUI();
		ConstructTagTree();

		_tree!.ButtonClicked += TreeButtonClicked;
		_addTagButton!.Pressed += AddTagButton_Pressed;
	}

	public void OnBeforeSerialize()
	{
		if (_tree is not null)
		{
			_tree.ButtonClicked -= TreeButtonClicked;
		}

		if (_addTagButton is not null)
		{
			_addTagButton.Pressed -= AddTagButton_Pressed;
		}
	}

	public void OnAfterDeserialize()
	{
		if (_tree is not null)
		{
			_tree.ButtonClicked += TreeButtonClicked;
		}

		if (_addTagButton is not null)
		{
			_addTagButton.Pressed += AddTagButton_Pressed;
		}

		Debug.Assert(
			_forgePluginData is not null, $"{nameof(_forgePluginData)} should have been initialized on _Ready().");

		_tagsManager = new TagsManager([.. _forgePluginData.RegisteredTags]);
		ReconstructTreeNode();
	}

	private void BuildUI()
	{
		var vBox = new VBoxContainer
		{
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			SizeFlagsVertical = SizeFlags.ExpandFill,
		};

		AddChild(vBox);

		var hBox = new HBoxContainer
		{
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
		};

		vBox.AddChild(hBox);

		var label = new Label
		{
			Text = "Tag Name:",
		};

		hBox.AddChild(label);

		_tagNameTextField = new LineEdit
		{
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
		};

		hBox.AddChild(_tagNameTextField);

		_addTagButton = new Button
		{
			Text = "Add Tag",
		};

		hBox.AddChild(_addTagButton);

		_tree = new Tree
		{
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			SizeFlagsVertical = SizeFlags.ExpandFill,
		};

		vBox.AddChild(_tree);
	}

	private void AddTagButton_Pressed()
	{
		EnsureInitialized();
		Debug.Assert(
			_forgePluginData.RegisteredTags is not null,
			$"{_forgePluginData.RegisteredTags} should have been initialized by the Forge plugin.");

		if (!Tag.IsValidKey(_tagNameTextField.Text, out string _, out string? fixedTag))
		{
			_tagNameTextField.Text = fixedTag;
		}

		if (_forgePluginData.RegisteredTags.Contains(_tagNameTextField.Text))
		{
			GD.PushWarning($"Tag [{_tagNameTextField.Text}] is already present in the manager.");
			return;
		}

		string[] oldTags = [.. _forgePluginData.RegisteredTags];
		string[] newTags = [.. oldTags, _tagNameTextField.Text];

		RecordRegisteredTagsChange($"Add Tag '{_tagNameTextField.Text}'", oldTags, newTags);
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

				string[] oldTags = [.. _forgePluginData.RegisteredTags];
				var newTags = new List<string>(oldTags);

				for (int i = newTags.Count - 1; i >= 0; i--)
				{
					string tag = newTags[i];

					if (string.Equals(tag, selectedTag.CompleteTagKey, StringComparison.OrdinalIgnoreCase) ||
						tag.StartsWith(selectedTag.CompleteTagKey + ".", StringComparison.InvariantCultureIgnoreCase))
					{
						newTags.RemoveAt(i);
					}
				}

				if (selectedTag.ParentTagNode is not null
					&& !newTags.Contains(selectedTag.ParentTagNode.CompleteTagKey))
				{
					newTags.Add(selectedTag.ParentTagNode.CompleteTagKey);
				}

				RecordRegisteredTagsChange($"Remove Tag '{selectedTag.CompleteTagKey}'", oldTags, [.. newTags]);
			}
		}
	}

	private void RecordRegisteredTagsChange(string actionName, string[] oldTags, string[] newTags)
	{
		EditorUndoRedoUtils.Record(
			_undoRedo,
			actionName,
			_forgePluginData,
			undo =>
			{
				undo.AddDoMethod(this, MethodName.ApplyRegisteredTags, newTags);
				undo.AddUndoMethod(this, MethodName.ApplyRegisteredTags, oldTags);
			},
			execute: true,
			fallback: () => ApplyRegisteredTags(newTags));
	}

	private void ApplyRegisteredTags(string[] tags)
	{
		EnsureInitialized();

		_forgePluginData.RegisteredTags.Clear();
		_forgePluginData.RegisteredTags.AddRange(tags);
		ResourceSaver.Save(_forgePluginData);

		ReconstructTreeNode();
	}

	[MemberNotNull(nameof(_tagsManager), nameof(_tree), nameof(_tagNameTextField), nameof(_forgePluginData))]
	private void EnsureInitialized()
	{
		Debug.Assert(
			_tagsManager is not null, $"{nameof(_tagsManager)} should have been initialized on _Ready().");
		Debug.Assert(
			_tree is not null, $"{nameof(_tree)} should have been initialized on _Ready().");
		Debug.Assert(
			_tagNameTextField is not null, $"{nameof(_tagNameTextField)} should have been initialized on _Ready().");
		Debug.Assert(
			_forgePluginData is not null, $"{nameof(_forgePluginData)} should have been initialized on _Ready().");
	}
}
#endif
