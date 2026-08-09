// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using System.Collections.Generic;
using System.Linq;
using Gamesmiths.Forge.Godot.Core;
using Gamesmiths.Forge.Godot.Resources;
using Godot;
using static Godot.FileDialog;

namespace Gamesmiths.Forge.Godot.Editor.Tags;

/// <summary>
/// Editor dock for managing every gameplay tag source in the project.
/// </summary>
/// <remarks>
/// <para>
/// Sources are peers. Each one gets a header with its own tags below it, and a tag is added to the source whose header
/// or row it was added from.
/// </para>
/// <para>
/// The same tag may be declared by several sources; that is harmless, since the runtime registry is their union. A tag
/// only stops existing once every source has dropped it. The Merged view shows that union, read-only, as the game
/// will see it.
/// </para>
/// </remarks>
[Tool]
public partial class TagsEditorDock : EditorDock, ISerializationListener
{
	private const string MergedCollapseScope = "<merged>";

	/// <summary>
	/// Which of the dock's two trees is being shown.
	/// </summary>
	private enum ViewMode
	{
		/// <summary>Each source's tags under its own header. This is where tags are edited.</summary>
		BySource = 0,

		/// <summary>The union of every source, read-only, as the runtime resolves it.</summary>
		Merged = 1,
	}

	/// <summary>
	/// Ids for the buttons the dock puts on tree rows.
	/// </summary>
	/// <remarks>
	/// Source-header buttons start well past the tag-row ones so that a mis-routed click cannot silently trigger the
	/// wrong action: the ids simply do not overlap.
	/// </remarks>
	private enum TagsTreeButton
	{
		AddChildTag = 0,
		RemoveTag = 1,
		AddTagToSource = 10,
		MoveSourceUp = 11,
		MoveSourceDown = 12,
		RemoveSourceReference = 13,
		RevealSource = 14,
	}

	private readonly Dictionary<TreeItem, string> _tagRows = [];
	private readonly Dictionary<TreeItem, int> _rowSourceIndex = [];
	private readonly Dictionary<TreeItem, string> _collapseKeys = [];

	// Kept for the session only. Persisting it through the dock's layout hooks is possible, but a bad value there
	// stops the editor from restoring the dock at all, which is a poor trade for remembering which rows were folded.
	private readonly HashSet<string> _collapsedKeys = [];

	private TagSourceEditingController? _controller;

	private Tree? _tree;
	private TabBar? _viewTabs;
	private PanelContainer? _viewPanel;
	private TagTreeSearchBar? _searchBar;
	private Button? _newSourceButton;
	private Button? _addExistingButton;
	private Button? _findSourcesButton;
	private Label? _readOnlyHint;

	private AddTagBar? _addTagBar;
	private EditorFileDialog? _newSourceDialog;
	private EditorFileDialog? _addExistingDialog;

	private string? _scrollToTagKey;

	private Texture2D? _addIcon;
	private Texture2D? _removeIcon;
	private Texture2D? _upIcon;
	private Texture2D? _downIcon;
	private Texture2D? _revealIcon;
	private Texture2D? _sourceIcon;
	private Font? _boldFont;

	private ViewMode CurrentView => _viewTabs is not null && IsInstanceValid(_viewTabs)
		? (ViewMode)_viewTabs.CurrentTab
		: ViewMode.BySource;

	public TagsEditorDock()
	{
		Name = "Tags";
		Title = "Tags";
		DockIcon = GD.Load<Texture2D>("uid://cu6ncpuumjo20");
		DefaultSlot = DockSlot.RightUl;
	}

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
		base._Ready();

		LoadThemeResources();

		BuildUI();
		RebuildTree();

		ConnectSignals();
	}

	/// <inheritdoc/>
	public override void _Notification(int what)
	{
		base._Notification(what);

		// Everything here is derived from the editor theme, and none of it is re-read on its own. Without this,
		// switching theme leaves the dock painted in the old colours until the editor is restarted.
		if (what == NotificationThemeChanged)
		{
			ApplyEditorTheme();
		}
	}

	public void OnBeforeSerialize()
	{
		DisconnectSignals();
	}

	public void OnAfterDeserialize()
	{
		ConnectSignals();
		RebuildTree();
	}

	/// <summary>
	/// Picks the colour shared by the view panel and the tab that owns it.
	/// </summary>
	/// <returns>A shade recessed from the dock background.</returns>
	/// <remarks>
	/// Derived from the editor theme rather than hardcoded, so it tracks whichever theme the user runs. The named
	/// colour is checked first because custom themes are not obliged to define it.
	/// </remarks>
	private static Color ResolveViewColor()
	{
		Theme theme = EditorInterface.Singleton.GetEditorTheme();

		return theme.HasColor("base_color", "Editor")
			? theme.GetColor("base_color", "Editor").Darkened(0.25f)
			: new Color(0, 0, 0, 0.12f);
	}

	/// <summary>
	/// Picks the text colour that marks a source header out from the tags under it.
	/// </summary>
	/// <returns>A shade brighter than ordinary row text.</returns>
	/// <remarks>
	/// Taken from the theme's own font colour rather than a fixed value, so headers stay legible under a light theme
	/// as well as a dark one.
	/// </remarks>
	private static Color ResolveSourceHeaderColor()
	{
		Theme theme = EditorInterface.Singleton.GetEditorTheme();

		return theme.HasColor("font_color", "Editor")
			? theme.GetColor("font_color", "Editor").Lightened(0.3f)
			: Color.FromHtml("FFFFFF");
	}

	/// <summary>
	/// Builds the background for the view below the tabs.
	/// </summary>
	/// <param name="viewColor">The shared panel colour.</param>
	/// <returns>The panel stylebox.</returns>
	private static StyleBoxFlat BuildViewStylebox(Color viewColor)
	{
		return new StyleBoxFlat
		{
			BgColor = viewColor,
			ContentMarginLeft = 4,
			ContentMarginTop = 4,
			ContentMarginRight = 4,
			ContentMarginBottom = 4,
			CornerRadiusTopRight = 3,
			CornerRadiusBottomLeft = 3,
			CornerRadiusBottomRight = 3,
		};
	}

	/// <summary>
	/// Builds the active tab's background, matching the view so the two join up.
	/// </summary>
	/// <param name="viewColor">The shared panel colour.</param>
	/// <returns>The selected-tab stylebox.</returns>
	/// <remarks>
	/// Only the top corners are rounded, and there is no bottom edge, so the tab runs straight into the panel beneath
	/// it instead of looking like a separate button sitting above it.
	/// </remarks>
	private static StyleBoxFlat BuildSelectedTabStylebox(Color viewColor)
	{
		return new StyleBoxFlat
		{
			BgColor = viewColor,
			ContentMarginLeft = 10,
			ContentMarginTop = 4,
			ContentMarginRight = 10,
			ContentMarginBottom = 4,
			CornerRadiusTopLeft = 3,
			CornerRadiusTopRight = 3,
		};
	}

	/// <summary>
	/// Builds the inactive tab's background, which is nothing at all.
	/// </summary>
	/// <returns>The unselected-tab stylebox.</returns>
	/// <remarks>
	/// Transparent rather than a darker fill: with the active tab now carrying the panel colour, anything drawn behind
	/// an inactive tab only competes with it. The margins match the selected tab so switching does not shift the row.
	/// </remarks>
	private static StyleBoxFlat BuildUnselectedTabStylebox()
	{
		return new StyleBoxFlat
		{
			BgColor = new Color(0, 0, 0, 0),
			ContentMarginLeft = 10,
			ContentMarginTop = 4,
			ContentMarginRight = 10,
			ContentMarginBottom = 4,
		};
	}

	private static void ApplyTabStyleboxes(TabBar tabs, Color viewColor)
	{
		tabs.AddThemeStyleboxOverride("tab_selected", BuildSelectedTabStylebox(viewColor));
		tabs.AddThemeStyleboxOverride("tab_unselected", BuildUnselectedTabStylebox());
		tabs.AddThemeStyleboxOverride("tab_hovered", BuildUnselectedTabStylebox());
	}

	private static ForgeTagsSource? GetSourceAt(int index)
	{
		IReadOnlyList<SourceEntry> sources = ForgeTagsRegistry.Sources;

		return index >= 0 && index < sources.Count ? sources[index].Resource : null;
	}

	private static void AddSourceReference(string path)
	{
		if (ResourceLoader.Load(path) is not ForgeTagsSource)
		{
			GD.PushError($"'{path}' is not a Forge tag source.");
			return;
		}

		var references = new List<string>(ForgeSettings.GetSourceReferences());

		if (references.Exists(existing =>
			string.Equals(ForgeSettings.ResolveReference(existing), path, StringComparison.OrdinalIgnoreCase)))
		{
			GD.PushWarning($"'{path}' is already one of this project's tag sources.");
			return;
		}

		references.Add(ForgeSettings.ToPreferredReference(path));

		ForgeSettings.SetSourceReferences([.. references]);
		ForgeTagsRegistry.Invalidate();

		GD.Print($"Added tag source '{path}'.");
	}

	private void LoadThemeResources()
	{
		Theme theme = EditorInterface.Singleton.GetEditorTheme();

		_addIcon = theme.GetIcon("Add", "EditorIcons");
		_removeIcon = theme.GetIcon("Remove", "EditorIcons");
		_upIcon = theme.GetIcon("ArrowUp", "EditorIcons");
		_downIcon = theme.GetIcon("ArrowDown", "EditorIcons");
		_revealIcon = theme.GetIcon("Filesystem", "EditorIcons");
		_sourceIcon = theme.GetIcon("ResourcePreloader", "EditorIcons");
		_boldFont = theme.GetFont("bold", "EditorFonts");
	}

	private void ApplyEditorTheme()
	{
		if (_viewPanel is null || !IsInstanceValid(_viewPanel))
		{
			return;
		}

		LoadThemeResources();

		Color viewColor = ResolveViewColor();
		_viewPanel.AddThemeStyleboxOverride("panel", BuildViewStylebox(viewColor));

		if (_viewTabs is not null && IsInstanceValid(_viewTabs))
		{
			ApplyTabStyleboxes(_viewTabs, viewColor);
		}

		// The row icons come from the theme too, so the tree has to be rebuilt for them to change with it.
		RebuildTree();
	}

	private void BuildUI()
	{
		var vBox = new VBoxContainer
		{
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			SizeFlagsVertical = SizeFlags.ExpandFill,
		};

		AddChild(vBox);

		// The toolbar keeps the dock's own background; the view below it is the surface that stands out. That only
		// works if the active tab is painted the same colour as that surface, so the two read as one piece of paper
		// with the inactive tab sitting behind it.
		var headerBox = new VBoxContainer
		{
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
		};

		vBox.AddChild(headerBox);

		var sourceActions = new HBoxContainer
		{
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
		};

		headerBox.AddChild(sourceActions);

		_newSourceButton = new Button
		{
			Text = "New Source",
			Icon = EditorInterface.Singleton.GetEditorTheme().GetIcon("New", "EditorIcons"),
			TooltipText = "Create a tag source and start reading tags from it.",
		};

		sourceActions.AddChild(_newSourceButton);

		_addExistingButton = new Button
		{
			Text = "Add Existing",
			Icon = EditorInterface.Singleton.GetEditorTheme().GetIcon("Load", "EditorIcons"),
			TooltipText = "Read tags from a tag source that already exists, such as the sample tags.",
		};

		sourceActions.AddChild(_addExistingButton);

		_findSourcesButton = new Button
		{
			Text = "Find Sources",
			Icon = EditorInterface.Singleton.GetEditorTheme().GetIcon("Search", "EditorIcons"),
			TooltipText = "Look for tag sources in this project that are not being read yet.",
		};

		sourceActions.AddChild(_findSourcesButton);

		_addTagBar = new AddTagBar();
		headerBox.AddChild(_addTagBar);

		_searchBar = new TagTreeSearchBar();
		headerBox.AddChild(_searchBar);

		Color viewColor = ResolveViewColor();

		// The tab and the panel it belongs to have to be siblings with no separation between them: the container's
		// default spacing is otherwise drawn straight through the seam, which is what makes the tab look detached.
		var viewSection = new VBoxContainer
		{
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			SizeFlagsVertical = SizeFlags.ExpandFill,
		};

		viewSection.AddThemeConstantOverride("separation", 0);
		vBox.AddChild(viewSection);

		_viewTabs = new TabBar();
		_viewTabs.AddTab("By Source");
		_viewTabs.AddTab("Merged");
		ApplyTabStyleboxes(_viewTabs, viewColor);
		viewSection.AddChild(_viewTabs);

		_readOnlyHint = new Label
		{
			Text = "Read-only. Switch to By Source to edit.",
			Visible = false,
		};

		_viewPanel = new PanelContainer
		{
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			SizeFlagsVertical = SizeFlags.ExpandFill,
		};

		_viewPanel.AddThemeStyleboxOverride("panel", BuildViewStylebox(viewColor));
		viewSection.AddChild(_viewPanel);

		var viewBox = new VBoxContainer
		{
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			SizeFlagsVertical = SizeFlags.ExpandFill,
		};

		_viewPanel.AddChild(viewBox);
		viewBox.AddChild(_readOnlyHint);

		_tree = new Tree
		{
			HideRoot = true,
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			SizeFlagsVertical = SizeFlags.ExpandFill,
		};

		// Cleared so the panel's colour shows through rather than the tree painting its own over it.
		_tree.AddThemeStyleboxOverride("panel", new StyleBoxEmpty());

		viewBox.AddChild(_tree);

		BuildDialogs();
	}

	private void BuildDialogs()
	{
		_newSourceDialog = new EditorFileDialog
		{
			FileMode = FileModeEnum.SaveFile,
			Access = AccessEnum.Resources,
			Title = "Create Tag Source",
			CurrentFile = "tags.tres",
		};

		_newSourceDialog.AddFilter("*.tres", "Tag Source");
		AddChild(_newSourceDialog);

		_addExistingDialog = new EditorFileDialog
		{
			FileMode = FileModeEnum.OpenFile,
			Access = AccessEnum.Resources,
			Title = "Add Tag Source",
		};

		_addExistingDialog.AddFilter("*.tres", "Tag Source");
		AddChild(_addExistingDialog);

		BuildScanDialog();
	}

	private void ConnectSignals()
	{
		if (_tree is not null)
		{
			_tree.ButtonClicked += OnTreeButtonClicked;
			_tree.ItemCollapsed += OnItemCollapsed;
		}

		if (_viewTabs is not null)
		{
			_viewTabs.TabChanged += OnViewTabChanged;
		}

		if (_searchBar is not null)
		{
			_searchBar.FilterChanged += OnFilterChanged;
		}

		if (_newSourceButton is not null)
		{
			_newSourceButton.Pressed += OnNewSourcePressed;
		}

		if (_addExistingButton is not null)
		{
			_addExistingButton.Pressed += OnAddExistingPressed;
		}

		if (_findSourcesButton is not null)
		{
			_findSourcesButton.Pressed += OnFindSourcesPressed;
		}

		if (_scanDialog is not null)
		{
			_scanDialog.Confirmed += OnScanConfirmed;
		}

		if (_addTagBar is not null)
		{
			_addTagBar.AddRequested += OnAddTagRequested;
		}

		if (_newSourceDialog is not null)
		{
			_newSourceDialog.FileSelected += OnNewSourceFileSelected;
		}

		if (_addExistingDialog is not null)
		{
			_addExistingDialog.FileSelected += OnAddExistingFileSelected;
		}

		ForgeTagsRegistry.Changed += OnRegisteredTagsChanged;
	}

	private void DisconnectSignals()
	{
		if (_tree is not null)
		{
			_tree.ButtonClicked -= OnTreeButtonClicked;
			_tree.ItemCollapsed -= OnItemCollapsed;
		}

		if (_viewTabs is not null)
		{
			_viewTabs.TabChanged -= OnViewTabChanged;
		}

		if (_searchBar is not null)
		{
			_searchBar.FilterChanged -= OnFilterChanged;
		}

		if (_newSourceButton is not null)
		{
			_newSourceButton.Pressed -= OnNewSourcePressed;
		}

		if (_addExistingButton is not null)
		{
			_addExistingButton.Pressed -= OnAddExistingPressed;
		}

		if (_findSourcesButton is not null)
		{
			_findSourcesButton.Pressed -= OnFindSourcesPressed;
		}

		if (_scanDialog is not null)
		{
			_scanDialog.Confirmed -= OnScanConfirmed;
		}

		if (_addTagBar is not null)
		{
			_addTagBar.AddRequested -= OnAddTagRequested;
		}

		if (_newSourceDialog is not null)
		{
			_newSourceDialog.FileSelected -= OnNewSourceFileSelected;
		}

		if (_addExistingDialog is not null)
		{
			_addExistingDialog.FileSelected -= OnAddExistingFileSelected;
		}

		ForgeTagsRegistry.Changed -= OnRegisteredTagsChanged;
	}

	private void OnNewSourcePressed()
	{
		_newSourceDialog?.PopupCentered(new Vector2I(700, 500));
	}

	private void OnAddExistingPressed()
	{
		_addExistingDialog?.PopupCentered(new Vector2I(700, 500));
	}

	// The dialog handlers stay instance methods on purpose: the delegate's target is what binds the connection to this
	// dock's lifetime, so a static handler would leave the signal connected to nothing after the dock is freed.
#pragma warning disable CA1822, S2325
	private void OnNewSourceFileSelected(string path)
	{
		var source = new ForgeTagsSource();
		Error error = ResourceSaver.Save(source, path);

		if (error != Error.Ok)
		{
			GD.PushError($"Failed to create tag source at {path}: {error}");
			return;
		}

		// UpdateFile registers the new file - and its UID - synchronously, which Scan does not guarantee. Without it
		// the reference below would be stored as a plain path and break the moment the file is moved.
		EditorInterface.Singleton.GetResourceFilesystem().UpdateFile(path);

		AddSourceReference(path);
	}

	private void OnAddExistingFileSelected(string path)
	{
		AddSourceReference(path);
	}
#pragma warning restore CA1822, S2325

	private void OnViewTabChanged(long tab)
	{
		RebuildTree();
	}

	private void OnFilterChanged()
	{
		RebuildTree();
	}

	private void OnRegisteredTagsChanged()
	{
		_searchBar?.RefreshSources();
		_addTagBar?.RefreshSources();
		RebuildTree();
	}

	private void OnItemCollapsed(TreeItem item)
	{
		if (!_collapseKeys.TryGetValue(item, out string? key))
		{
			return;
		}

		if (item.Collapsed)
		{
			_collapsedKeys.Add(key);
		}
		else
		{
			_collapsedKeys.Remove(key);
		}
	}

	private void OnAddTagRequested(int sourceIndex, string tagKey)
	{
		ForgeTagsSource? source = GetSourceAt(sourceIndex);

		if (source is null || _controller is null)
		{
			return;
		}

		if (_controller.AddTag(source, tagKey))
		{
			_addTagBar?.Clear();
		}
	}

	private void PromptAddTag(int sourceIndex, string prefill)
	{
		IReadOnlyList<SourceEntry> sources = ForgeTagsRegistry.Sources;

		if (_addTagBar is null || sourceIndex < 0 || sourceIndex >= sources.Count)
		{
			return;
		}

		SourceEntry entry = sources[sourceIndex];

		if (entry.IsMissing)
		{
			GD.PushWarning($"'{entry.ResourcePath}' is missing, so tags cannot be added to it.");
			return;
		}

		_addTagBar.PrepareFor(sourceIndex, prefill);
	}

	private void RebuildTree()
	{
		if (_tree is null || !IsInstanceValid(_tree))
		{
			return;
		}

		_tagRows.Clear();
		_rowSourceIndex.Clear();
		_collapseKeys.Clear();
		_tree.Clear();

		bool merged = CurrentView == ViewMode.Merged;

		if (_readOnlyHint is not null)
		{
			_readOnlyHint.Visible = merged;
		}

		if (_searchBar is not null)
		{
			// The Merged view already shows every source at once, so narrowing to one would contradict it.
			_searchBar.SourcePickerEnabled = !merged;
			_searchBar.RefreshSources();
		}

		_tree.Columns = merged ? 2 : 1;

		TreeItem root = _tree.CreateItem();

		if (merged)
		{
			BuildMergedView(_tree, root);
		}
		else
		{
			BuildBySourceView(_tree, root);
		}

		RestoreScrollTarget();
	}

	private void RestoreScrollTarget()
	{
		if (_tree is null || _scrollToTagKey is null)
		{
			return;
		}

		// Tree exposes no scroll setter, so the best that can be done is to bring the row that was just acted on back
		// into view. That covers the case that matters: clicking something and staying put.
		TreeItem? target = _tagRows
			.FirstOrDefault(row => string.Equals(row.Value, _scrollToTagKey, StringComparison.OrdinalIgnoreCase))
			.Key;

		if (target is not null)
		{
			_tree.ScrollToItem(target);
		}

		_scrollToTagKey = null;
	}
}
#endif
