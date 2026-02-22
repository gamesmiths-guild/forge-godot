// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using System.Collections.Generic;
using Gamesmiths.Forge.Godot.Core;
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Tags;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers;

/// <summary>
/// Resolver editor that selects a single tag. Reuses the tag tree UI pattern from <c>TagEditorProperty</c>.
/// </summary>
[Tool]
internal sealed partial class TagResolverEditor : NodeEditorProperty
{
	private readonly Dictionary<TreeItem, TagNode> _treeItemToNode = [];

	private Button? _tagButton;
	private ScrollContainer? _scroll;
	private Tree? _tree;
	private string _selectedTag = string.Empty;
	private Texture2D? _checkedIcon;
	private Texture2D? _uncheckedIcon;

	/// <inheritdoc/>
	public override string DisplayName => "Tag";

	/// <inheritdoc/>
	public override Type ValueType => typeof(bool);

	/// <inheritdoc/>
	public override string ResolverTypeId => "Tag";

	/// <inheritdoc/>
	public override bool IsCompatibleWith(Type expectedType)
	{
		return expectedType == typeof(bool) || expectedType == typeof(Variant128);
	}

	/// <inheritdoc/>
	public override void Setup(
		StatescriptGraph graph,
		StatescriptNodeProperty? property,
		Type expectedType,
		Action onChanged)
	{
		SizeFlagsHorizontal = SizeFlags.ExpandFill;
		var vBox = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		AddChild(vBox);

		// Restore from existing binding.
		if (property?.Resolver is TagResolverResource tagRes)
		{
			_selectedTag = tagRes.Tag;
		}

		_checkedIcon = EditorInterface.Singleton
			.GetEditorTheme()
			.GetIcon("GuiRadioChecked", "EditorIcons");

		_uncheckedIcon = EditorInterface.Singleton
			.GetEditorTheme()
			.GetIcon("GuiRadioUnchecked", "EditorIcons");

		_tagButton = new Button
		{
			ToggleMode = true,
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			Text = string.IsNullOrEmpty(_selectedTag) ? "(select tag)" : _selectedTag,
		};

		_tagButton.Toggled += x =>
		{
			if (_scroll is not null)
			{
				_scroll.Visible = x;
				RaiseLayoutSizeChanged();
			}
		};

		vBox.AddChild(_tagButton);

		_scroll = new ScrollContainer
		{
			Visible = false,
			CustomMinimumSize = new Vector2(0, 180),
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
		vBox.AddChild(_scroll);

		_tree.ButtonClicked += (item, _, id, mouseButton) =>
		{
			if (mouseButton != 1 || id != 0)
			{
				return;
			}

			if (!_treeItemToNode.TryGetValue(item, out TagNode? tagNode))
			{
				return;
			}

			Forge.Core.StringKey newValue = tagNode.CompleteTagKey;

			if (newValue == _selectedTag)
			{
				newValue = string.Empty;
			}

			_selectedTag = newValue;

			if (_tagButton is not null)
			{
				_tagButton.Text = string.IsNullOrEmpty(_selectedTag) ? "(select tag)" : _selectedTag;
			}

			RebuildTree();
			onChanged();
		};

		RebuildTree();
	}

	/// <inheritdoc/>
	public override void SaveTo(StatescriptNodeProperty property)
	{
		property.Resolver = new TagResolverResource
		{
			Tag = _selectedTag,
		};
	}

	private void RebuildTree()
	{
		if (_tree is null || _checkedIcon is null || _uncheckedIcon is null)
		{
			return;
		}

		_tree.Clear();
		_treeItemToNode.Clear();

		TreeItem root = _tree.CreateItem();

		ForgeData forgePluginData = ResourceLoader.Load<ForgeData>("uid://8j4xg16o3qnl");
		var tagsManager = new TagsManager([.. forgePluginData.RegisteredTags]);

		BuildTreeRecursive(root, tagsManager.RootNode);
	}

	private void BuildTreeRecursive(TreeItem parent, TagNode node)
	{
		if (_tree is null || _checkedIcon is null || _uncheckedIcon is null)
		{
			return;
		}

		foreach (TagNode child in node.ChildTags)
		{
			TreeItem item = _tree.CreateItem(parent);
			item.SetText(0, child.TagKey);

			var selected = _selectedTag == child.CompleteTagKey;
			item.AddButton(0, selected ? _checkedIcon : _uncheckedIcon);

			_treeItemToNode[item] = child;
			BuildTreeRecursive(item, child);
		}
	}
}
#endif
