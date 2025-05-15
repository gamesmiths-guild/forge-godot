// Copyright © Gamesmiths Guild.

#if TOOLS
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Gamesmiths.Forge.GameplayTags;
using Gamesmiths.Forge.Godot.Core;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor;

[Tool]
public partial class CueKeysEditor : VBoxContainer
{
	private ForgeData? _forgePluginData;

	private Tree? _tree;
	private LineEdit? _cueKeyTextField;
	private Button? _addKeyButton;

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

		_removeIcon = EditorInterface.Singleton.GetEditorTheme().GetIcon("Remove", "EditorIcons");

		_tree = GetNode<Tree>("%Tree");
		_cueKeyTextField = GetNode<LineEdit>("%KeyField");
		_addKeyButton = GetNode<Button>("%AddKeyButton");

		ConstructTagTree();

		_tree.ButtonClicked += TreeButtonClicked;
		_addKeyButton.Pressed += AddTagButton_Pressed;
	}

	private void AddTagButton_Pressed()
	{
		EnsureInitialized();
		Debug.Assert(
			_forgePluginData.RegisteredCues is not null,
			$"{_forgePluginData.RegisteredCues} should have been initialized by the Forge plugin.");

		if (!GameplayTag.IsValidKey(_cueKeyTextField.Text, out var _, out var fixedTag))
		{
			_cueKeyTextField.Text = fixedTag;
		}

		if (_forgePluginData.RegisteredCues.Contains(_cueKeyTextField.Text))
		{
			GD.PushWarning($"Tag [{_cueKeyTextField.Text}] is already present in the manager.");
			return;
		}

		_forgePluginData.RegisteredCues.Add(_cueKeyTextField.Text);
		ResourceSaver.Save(_forgePluginData);

		ReconstructTreeNode();
	}

	private void ReconstructTreeNode()
	{
		EnsureInitialized();

		_tree.Clear();
		ConstructTagTree();
	}

	private void ConstructTagTree()
	{
		EnsureInitialized();
		Debug.Assert(
			_forgePluginData.RegisteredCues is not null,
			$"{_forgePluginData.RegisteredCues} should have been initialized by the Forge plugin.");

		TreeItem rootTreeNode = _tree.CreateItem();
		_tree.HideRoot = true;

		if (_forgePluginData.RegisteredCues.Count == 0)
		{
			TreeItem childTreeNode = _tree.CreateItem(rootTreeNode);
			childTreeNode.SetText(0, "No cues has been registered yet.");
			childTreeNode.SetCustomColor(0, Color.FromHtml("EED202"));
			return;
		}

		foreach (var key in _forgePluginData.RegisteredCues)
		{
			TreeItem childTreeNode = _tree.CreateItem(rootTreeNode);
			childTreeNode.SetText(0, key);
			childTreeNode.AddButton(0, _removeIcon);
		}
	}

	private void BuildTreeRecursively(Tree tree, TreeItem currentTreeItem, GameplayTagNode currentNode)
	{
		foreach (GameplayTagNode childTagNode in currentNode.ChildTags)
		{
			TreeItem childTreeNode = tree.CreateItem(currentTreeItem);
			childTreeNode.SetText(0, childTagNode.TagKey);
			childTreeNode.AddButton(0, _removeIcon);

			BuildTreeRecursively(tree, childTreeNode, childTagNode);
		}
	}

	private void TreeButtonClicked(TreeItem item, long column, long id, long mouseButtonIndex)
	{
		EnsureInitialized();
		Debug.Assert(
			_forgePluginData.RegisteredCues is not null,
			$"{_forgePluginData.RegisteredCues} should have been initialized by the Forge plugin.");

		if (mouseButtonIndex == 1 && id == 0)
		{
			var selectedTag = item.GetText(0);

			for (var i = _forgePluginData.RegisteredCues.Count - 1; i >= 0; i--)
			{
				var tag = _forgePluginData.RegisteredCues[i];

				if (tag == selectedTag)
				{
					_forgePluginData.RegisteredCues.Remove(tag);
				}
			}

			ResourceSaver.Save(_forgePluginData);
			ReconstructTreeNode();
		}
	}

	[MemberNotNull(nameof(_tree), nameof(_cueKeyTextField), nameof(_forgePluginData))]
	private void EnsureInitialized()
	{
		Debug.Assert(_tree is not null, $"{_tree} should have been initialized on _Ready().");
		Debug.Assert(_cueKeyTextField is not null, $"{_cueKeyTextField} should have been initialized on _Ready().");
		Debug.Assert(_forgePluginData is not null, $"{_forgePluginData} should have been initialized on _Ready().");
	}
}
#endif
