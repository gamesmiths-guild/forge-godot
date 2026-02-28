// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using System.Collections.Generic;
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Gamesmiths.Forge.Statescript.Nodes;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Statescript;

/// <summary>
/// A popup dialog for adding Statescript nodes to a graph. Features a search bar, categorized tree view, description
/// panel, and Create/Cancel buttons.
/// </summary>
[Tool]
internal sealed partial class StatescriptAddNodeDialog : ConfirmationDialog
{
	private const int DialogWidth = 244;
	private const int DialogHeight = 400;

	private static readonly string _exitNodeDescription = new ExitNode().Description;

	private LineEdit? _searchBar;
	private MenuButton? _expandCollapseButton;
	private Tree? _tree;
	private Label? _descriptionHeader;
	private RichTextLabel? _descriptionLabel;

	private bool _isFiltering;

	/// <summary>
	/// Raised when the user confirms node creation. The first argument is the selected
	/// <see cref="StatescriptNodeDiscovery.NodeTypeInfo"/> (null for Exit node), the second is the
	/// <see cref="StatescriptNodeType"/>, and the third is the graph-local position to place the node.
	/// </summary>
	public event Action<StatescriptNodeDiscovery.NodeTypeInfo?, StatescriptNodeType, Vector2>? NodeCreationRequested;

	/// <summary>
	/// Gets or sets the graph-local position where the new node should be placed.
	/// </summary>
	public Vector2 SpawnPosition { get; set; }

	public StatescriptAddNodeDialog()
	{
		Title = "Add Statescript Node";
		Exclusive = true;
		Unresizable = false;
		MinSize = new Vector2I(DialogWidth, DialogHeight);
		Size = new Vector2I(DialogWidth, DialogHeight);
		OkButtonText = "Create";
	}

	public override void _Ready()
	{
		base._Ready();

		Transient = true;
		TransientToFocused = true;

		BuildUI();
		PopulateTree();

		GetOkButton().Disabled = true;

		Confirmed += OnConfirmed;
		Canceled += OnCanceled;
	}

	/// <summary>
	/// Shows the dialog at the specified screen position, resets search and selection state.
	/// </summary>
	/// <param name="spawnPosition">The graph-local position where the node should be created.</param>
	/// <param name="screenPosition">The screen position to show the dialog at.</param>
	public void ShowAtPosition(Vector2 spawnPosition, Vector2I screenPosition)
	{
		SpawnPosition = spawnPosition;

		if (_isFiltering)
		{
			_searchBar?.Clear();
			PopulateTree();
		}
		else
		{
			_searchBar?.Clear();
		}

		_tree?.DeselectAll();
		GetOkButton().Disabled = true;
		UpdateDescription(null);

		Position = screenPosition;
		Size = new Vector2I(DialogWidth, DialogHeight);
		Popup();

		_searchBar?.GrabFocus();
	}

	private static void SetAllCollapsed(TreeItem root, bool collapsed)
	{
		TreeItem? child = root.GetFirstChild();
		while (child is not null)
		{
			child.Collapsed = collapsed;
			SetAllCollapsed(child, collapsed);
			child = child.GetNext();
		}
	}

	private void BuildUI()
	{
		var vBox = new VBoxContainer
		{
			SizeFlagsVertical = Control.SizeFlags.ExpandFill,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
		};

		AddChild(vBox);

		var searchHBox = new HBoxContainer();
		vBox.AddChild(searchHBox);

		_searchBar = new LineEdit
		{
			PlaceholderText = "Search...",
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			ClearButtonEnabled = true,
			RightIcon = EditorInterface.Singleton.GetEditorTheme().GetIcon("Search", "EditorIcons"),
		};

		_searchBar.TextChanged += OnSearchTextChanged;
		searchHBox.AddChild(_searchBar);

		_expandCollapseButton = new MenuButton
		{
			Flat = true,
			Icon = EditorInterface.Singleton.GetEditorTheme().GetIcon("Tools", "EditorIcons"),
			TooltipText = "Options",
		};

		PopupMenu expandMenu = _expandCollapseButton.GetPopup();
		expandMenu.AddItem("Expand All", 0);
		expandMenu.AddItem("Collapse All", 1);
		expandMenu.IdPressed += OnExpandCollapseMenuPressed;
		searchHBox.AddChild(_expandCollapseButton);

		_tree = new Tree
		{
			SizeFlagsVertical = Control.SizeFlags.ExpandFill,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			HideRoot = true,
			SelectMode = Tree.SelectModeEnum.Single,
		};

		_tree.ItemSelected += OnTreeItemSelected;
		_tree.ItemActivated += OnTreeItemActivated;
		vBox.AddChild(_tree);

		_descriptionHeader = new Label
		{
			Text = "Description:",
		};

		vBox.AddChild(_descriptionHeader);

		_descriptionLabel = new RichTextLabel
		{
			BbcodeEnabled = true,
			ScrollActive = true,
			CustomMinimumSize = new Vector2(0, 70),
		};

		vBox.AddChild(_descriptionLabel);
	}

	private void PopulateTree(string filter = "")
	{
		if (_tree is null)
		{
			return;
		}

		_isFiltering = !string.IsNullOrWhiteSpace(filter);
		_tree.Clear();
		TreeItem root = _tree.CreateItem();

		IReadOnlyList<StatescriptNodeDiscovery.NodeTypeInfo> discoveredTypes =
			StatescriptNodeDiscovery.GetDiscoveredNodeTypes();

		var filterLower = filter.ToLowerInvariant();

		TreeItem? actionCategory = null;
		TreeItem? conditionCategory = null;
		TreeItem? stateCategory = null;

		foreach (StatescriptNodeDiscovery.NodeTypeInfo typeInfo in discoveredTypes)
		{
			if (_isFiltering && !typeInfo.DisplayName.Contains(filterLower, StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}

			TreeItem categoryItem;

			switch (typeInfo.NodeType)
			{
				case StatescriptNodeType.Action:
					actionCategory ??= CreateCategoryItem(root, "Action");
					categoryItem = actionCategory;
					break;
				case StatescriptNodeType.Condition:
					conditionCategory ??= CreateCategoryItem(root, "Condition");
					categoryItem = conditionCategory;
					break;
				case StatescriptNodeType.State:
					stateCategory ??= CreateCategoryItem(root, "State");
					categoryItem = stateCategory;
					break;
				default:
					continue;
			}

			TreeItem item = _tree.CreateItem(categoryItem);
			item.SetText(0, typeInfo.DisplayName);
			item.SetMetadata(0, typeInfo.RuntimeTypeName);
		}

		if (!_isFiltering || "exit".Contains(filterLower, StringComparison.OrdinalIgnoreCase)
			|| "exit node".Contains(filterLower, StringComparison.OrdinalIgnoreCase))
		{
			TreeItem exitItem = _tree.CreateItem(root);
			exitItem.SetText(0, "Exit");
			exitItem.SetMetadata(0, "__exit__");
		}

		SetAllCollapsed(root, !_isFiltering);

		UpdateDescription(null);
	}

	private TreeItem CreateCategoryItem(TreeItem parent, string name)
	{
		TreeItem item = _tree!.CreateItem(parent);
		item.SetText(0, name);
		item.SetSelectable(0, false);
		return item;
	}

	private void OnSearchTextChanged(string newText)
	{
		PopulateTree(newText);
		GetOkButton().Disabled = true;
	}

	private void OnExpandCollapseMenuPressed(long id)
	{
		if (_tree is null)
		{
			return;
		}

		TreeItem? root = _tree.GetRoot();
		if (root is null)
		{
			return;
		}

		SetAllCollapsed(root, id != 0);
	}

	private void OnTreeItemSelected()
	{
		if (_tree is null)
		{
			return;
		}

		TreeItem? selected = _tree.GetSelected();
		if (selected?.IsSelectable(0) != true)
		{
			GetOkButton().Disabled = true;
			UpdateDescription(null);
			return;
		}

		GetOkButton().Disabled = false;

		var metadata = selected.GetMetadata(0).AsString();
		UpdateDescription(metadata);
	}

	private void OnTreeItemActivated()
	{
		if (_tree?.GetSelected() is not null && !GetOkButton().Disabled)
		{
			OnConfirmed();
			Hide();
		}
	}

	private void OnConfirmed()
	{
		if (_tree is null)
		{
			return;
		}

		TreeItem? selected = _tree.GetSelected();
		if (selected?.IsSelectable(0) != true)
		{
			return;
		}

		var metadata = selected.GetMetadata(0).AsString();

		if (metadata == "__exit__")
		{
			NodeCreationRequested?.Invoke(null, StatescriptNodeType.Exit, SpawnPosition);
		}
		else
		{
			StatescriptNodeDiscovery.NodeTypeInfo? typeInfo =
				StatescriptNodeDiscovery.FindByRuntimeTypeName(metadata);

			if (typeInfo is not null)
			{
				NodeCreationRequested?.Invoke(typeInfo, typeInfo.NodeType, SpawnPosition);
			}
		}
	}

	private void OnCanceled()
	{
		// Method intentionally left blank, no action needed on cancel.
	}

	private void UpdateDescription(string? runtimeTypeName)
	{
		if (_descriptionLabel is null)
		{
			return;
		}

		if (runtimeTypeName is null)
		{
			_descriptionLabel.Text = string.Empty;
			return;
		}

		if (runtimeTypeName == "__exit__")
		{
			_descriptionLabel.Text = _exitNodeDescription;
			return;
		}

		StatescriptNodeDiscovery.NodeTypeInfo? typeInfo =
			StatescriptNodeDiscovery.FindByRuntimeTypeName(runtimeTypeName);

		_descriptionLabel.Text = typeInfo?.Description ?? string.Empty;
	}
}
#endif
