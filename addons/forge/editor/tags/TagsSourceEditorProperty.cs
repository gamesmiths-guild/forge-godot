// Copyright © Gamesmiths Guild.

#if TOOLS
using System.Collections.Generic;
using System.Linq;
using Gamesmiths.Forge.Godot.Core;
using Gamesmiths.Forge.Godot.Resources;
using Gamesmiths.Forge.Tags;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Tags;

/// <summary>
/// Edits a <see cref="ForgeTagsSource"/>'s tags as a tree, in place of the raw string array.
/// </summary>
/// <remarks>
/// The tree is built from the edited resource alone, not from the project registry, so a source can be authored before
/// it is attached to the project - which is also what makes the Tags dock's "find sources" flow useful.
/// </remarks>
[Tool]
public partial class TagsSourceEditorProperty : EditorProperty, ISerializationListener
{
	private const int AddChildTagButtonId = 0;
	private const int RemoveTagButtonId = 1;

	private readonly Dictionary<TreeItem, string> _treeItemToTag = [];

	private TagSourceEditingController? _controller;

	private VBoxContainer? _root;
	private AddTagBar? _addTagBar;
	private HBoxContainer? _registrationToolbar;
	private Button? _registerButton;
	private Label? _registrationHint;
	private TagTreeSearchBar? _searchBar;
	private Tree? _tree;

	private TagsManager? _sourceTags;

	private Texture2D? _addIcon;
	private Texture2D? _removeIcon;

	/// <summary>
	/// Sets the controller that applies and records tag edits.
	/// </summary>
	/// <param name="controller">The plugin's shared editing controller.</param>
	public void SetEditingController(TagSourceEditingController controller)
	{
		_controller = controller;
	}

	public override void _Ready()
	{
		_addIcon = EditorInterface.Singleton.GetEditorTheme().GetIcon("Add", "EditorIcons");
		_removeIcon = EditorInterface.Singleton.GetEditorTheme().GetIcon("Remove", "EditorIcons");

		_root = new VBoxContainer
		{
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
		};

		// One source is being edited, so there is nothing to choose a destination between.
		_addTagBar = new AddTagBar
		{
			SourcePickerVisible = false,
		};

		// Hidden unless the source is unregistered, so it does not leave a blank gap under the add-tag row.
		_registrationToolbar = new HBoxContainer
		{
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			Visible = false,
		};

		HBoxContainer toolbar = _registrationToolbar;

		_registrationHint = new Label
		{
			Text = string.Empty,
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
		};

		toolbar.AddChild(_registrationHint);

		_registerButton = new Button
		{
			Text = "Add to project",
			TooltipText = "Start reading this source's tags in this project.",
			Visible = false,
		};

		toolbar.AddChild(_registerButton);

		_searchBar = new TagTreeSearchBar();

		_tree = new Tree
		{
			HideRoot = true,
			CustomMinimumSize = new Vector2(0, 220),
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			SizeFlagsVertical = SizeFlags.ExpandFill,
		};

		_root.AddChild(_addTagBar);
		_root.AddChild(toolbar);
		_root.AddChild(_searchBar);
		_root.AddChild(_tree);

		AddChild(_root);
		SetBottomEditor(_root);

		_addTagBar.AddRequested += OnAddTagRequested;
		_registerButton.Pressed += OnRegisterPressed;
		_searchBar.FilterChanged += OnFilterChanged;
		_tree.ButtonClicked += OnTreeButtonClicked;
		ForgeTagsRegistry.Changed += OnRegisteredTagsChanged;
	}

	public override void _UpdateProperty()
	{
		RebuildTree();
	}

	public override void _ExitTree()
	{
		ReleaseUiState();
		FreeAllChildren();
		base._ExitTree();
	}

	public void OnBeforeSerialize()
	{
		ReleaseUiState();
		FreeAllChildren();
	}

	public void OnAfterDeserialize()
	{
		// This method is intentionally left blank.
	}

	private static string DescribeSource(ForgeTagsSource source)
	{
		return string.IsNullOrEmpty(source.ResourcePath)
			? "this source"
			: source.ResourcePath.GetFile();
	}

	private ForgeTagsSource? GetEditedSource()
	{
		return GetEditedObject() as ForgeTagsSource;
	}

	private void RebuildTree()
	{
		if (_tree is null || _searchBar is null || !IsInstanceValid(_tree))
		{
			return;
		}

		ForgeTagsSource? source = GetEditedSource();

		if (source is null)
		{
			return;
		}

		_tree.Clear();
		_treeItemToTag.Clear();

		_sourceTags?.DestroyTagTree();
		_sourceTags = new TagsManager([.. source.RegisteredTags]);

		_searchBar.FixedTags = _sourceTags;
		_searchBar.RefreshSources();

		TreeItem root = _tree.CreateItem();

		if (_sourceTags.RootNode.ChildTags.Count == 0)
		{
			TreeItem emptyRow = _tree.CreateItem(root);
			emptyRow.SetText(0, "This source declares no tags yet.");
			emptyRow.SetCustomColor(0, Color.FromHtml("EED202"));
		}
		else
		{
			TagSourceTreeBuilder.Build(
				_tree,
				root,
				_sourceTags.RootNode,
				_searchBar.ResolveFilter(_sourceTags),
				DecorateRow,
				_treeItemToTag);
		}

		UpdateRegistrationHint(source);
		UpdateMinimumSize();
	}

	private void DecorateRow(TreeItem item, string completeTagKey)
	{
		item.AddButton(0, _addIcon, AddChildTagButtonId, tooltipText: "Add a child tag here.");
		item.AddButton(0, _removeIcon, RemoveTagButtonId, tooltipText: "Remove this tag and its children.");
	}

	private void UpdateRegistrationHint(ForgeTagsSource source)
	{
		if (_registrationHint is null || _registerButton is null)
		{
			return;
		}

		bool registered = ForgeTagsRegistry.Sources.Any(entry => entry.Resource == source);

		_registrationHint.Text = registered ? string.Empty : "Not one of this project's tag sources.";
		_registerButton.Visible = !registered && !string.IsNullOrEmpty(source.ResourcePath);

		if (_registrationToolbar is not null && IsInstanceValid(_registrationToolbar))
		{
			_registrationToolbar.Visible = !registered;
		}
	}

	private void OnRegisterPressed()
	{
		ForgeTagsSource? source = GetEditedSource();

		if (source is null || string.IsNullOrEmpty(source.ResourcePath))
		{
			return;
		}

		var references = new List<string>(ForgeSettings.GetSourceReferences())
		{
			ForgeSettings.ToPreferredReference(source.ResourcePath),
		};

		ForgeSettings.SetSourceReferences([.. references]);
		ForgeTagsRegistry.Invalidate();

		GD.Print($"Added tag source '{source.ResourcePath}' to this project.");
	}

	private void OnAddTagRequested(int sourceIndex, string tagKey)
	{
		ForgeTagsSource? source = GetEditedSource();

		if (source is null || _controller is null)
		{
			return;
		}

		if (_controller.AddTag(source, tagKey))
		{
			_addTagBar?.Clear();
			RebuildTree();
		}
	}

	private void OnTreeButtonClicked(TreeItem item, long column, long id, long mouseButtonIndex)
	{
		ForgeTagsSource? source = GetEditedSource();

		if (source is null || mouseButtonIndex != 1 || !_treeItemToTag.TryGetValue(item, out string? tagKey))
		{
			return;
		}

		if (id == AddChildTagButtonId)
		{
			_addTagBar?.PrepareFor(-1, $"{tagKey}.");
			return;
		}

		if (id != RemoveTagButtonId || _controller is null)
		{
			return;
		}

		_controller.RemoveTag(source, tagKey);
		RebuildTree();
	}

	private void OnFilterChanged()
	{
		RebuildTree();
	}

	private void OnRegisteredTagsChanged()
	{
		RebuildTree();
	}

	private void ReleaseUiState()
	{
		ForgeTagsRegistry.Changed -= OnRegisteredTagsChanged;

		if (_addTagBar is not null && IsInstanceValid(_addTagBar))
		{
			_addTagBar.AddRequested -= OnAddTagRequested;
		}

		if (_registerButton is not null && IsInstanceValid(_registerButton))
		{
			_registerButton.Pressed -= OnRegisterPressed;
		}

		if (_searchBar is not null && IsInstanceValid(_searchBar))
		{
			_searchBar.FilterChanged -= OnFilterChanged;
		}

		if (_tree is not null && IsInstanceValid(_tree))
		{
			_tree.ButtonClicked -= OnTreeButtonClicked;
		}

		_sourceTags?.DestroyTagTree();
		_sourceTags = null;

		_treeItemToTag.Clear();
		_root = null;
		_addTagBar = null;
		_registrationToolbar = null;
		_registerButton = null;
		_registrationHint = null;
		_searchBar = null;
		_tree = null;
		_addIcon = null;
		_removeIcon = null;
	}

	private void FreeAllChildren()
	{
		for (int i = GetChildCount() - 1; i >= 0; i--)
		{
			Node child = GetChild(i);
			RemoveChild(child);
			child.Free();
		}
	}
}
#endif
