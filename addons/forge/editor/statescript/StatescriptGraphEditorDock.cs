// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Godot;
using GodotCollections = Godot.Collections;

namespace Gamesmiths.Forge.Godot.Editor.Statescript;

/// <summary>
/// Main editor panel for Statescript graphs. Supports editing multiple graphs via tabs.
/// Designed to be shown in the bottom panel area of the Godot editor.
/// </summary>
[Tool]
public partial class StatescriptGraphEditorDock : EditorDock, ISerializationListener
{
	private readonly List<GraphTab> _openTabs = [];
	private readonly Dictionary<StringName, Vector2> _preMovePositions = [];

	private PanelContainer? _tabBarBackground;
	private TabBar? _tabBar;
	private PanelContainer? _contentPanel;
	private GraphEdit? _graphEdit;
	private Label? _emptyLabel;
	private Button? _addNodeButton;
	private StatescriptAddNodeDialog? _addNodeDialog;
	private StatescriptVariablePanel? _variablePanel;
	private HSplitContainer? _splitContainer;

	private MenuButton? _fileMenuButton;
	private PopupMenu? _fileMenuPopup;
	private Button? _variablesToggleButton;
	private Button? _onlineDocsButton;

	private AcceptDialog? _newStatescriptDialog;
	private LineEdit? _newStatescriptPathEdit;

	private EditorUndoRedoManager? _undoRedo;

	private int _nextNodeId;
	private bool _isLoadingGraph;

	private string? _pendingConnectionNode;
	private int _pendingConnectionPort;
	private bool _pendingConnectionIsOutput;

	private EditorFileSystem? _fileSystem;
	private Callable _filesystemChangedCallable;

	private string[]? _serializedTabPaths;
	private int _serializedActiveTab = -1;
	private bool[]? _serializedVariablesStates;
	private string?[]? _serializedSelectedVariables;
	private string[]? _serializedConnections;
	private int[]? _serializedConnectionCounts;
	private bool _persistedVariablesPanelVisible = true;
	private bool _sharedVariableHighlightSubscribed;

	/// <summary>
	/// Gets the currently active graph resource, if any.
	/// </summary>
	public StatescriptGraph? CurrentGraph =>
		_openTabs.Count > 0 && _tabBar is not null && _tabBar.CurrentTab < _openTabs.Count
			? _openTabs[_tabBar.CurrentTab].GraphResource
			: null;

	public StatescriptGraphEditorDock()
	{
		Title = "Statescript";
		DefaultSlot = DockSlot.Bottom;
		DockIcon = GD.Load<Texture2D>("uid://b6yrjb46fluw3");

		AvailableLayouts = DockLayout.Horizontal | DockLayout.Floating;
	}

	public override void _Ready()
	{
		base._Ready();

		StyleBox bottomPanelStyleBox = EditorInterface.Singleton.GetBaseControl()
			.GetThemeStylebox("BottomPanel", "EditorStyles");

		AddThemeConstantOverride("margin_top", -(int)bottomPanelStyleBox.ContentMarginTop);
		AddThemeConstantOverride("margin_left", -(int)bottomPanelStyleBox.ContentMarginLeft);
		AddThemeConstantOverride("margin_right", -(int)bottomPanelStyleBox.ContentMarginRight);

		BuildUI();
		UpdateVisibility();

		_fileSystem = EditorInterface.Singleton.GetResourceFilesystem();
		_filesystemChangedCallable = new Callable(this, nameof(OnFilesystemChanged));

		_fileSystem.Connect(EditorFileSystem.SignalName.ResourcesReimported, _filesystemChangedCallable);
		SubscribeSharedVariableHighlightState();
	}

	public override void _ExitTree()
	{
		base._ExitTree();
		UnsubscribeSharedVariableHighlightState();

		ClearGraphEditor();
		DisposeCachedGraphVisuals();
		_openTabs.Clear();

		if (_fileSystem?.IsConnected(EditorFileSystem.SignalName.ResourcesReimported, _filesystemChangedCallable)
			== true)
		{
			_fileSystem.Disconnect(EditorFileSystem.SignalName.ResourcesReimported, _filesystemChangedCallable);
		}

		DisconnectUISignals();
		_fileSystem = null;
		_filesystemChangedCallable = default;
	}

	public void OnBeforeSerialize()
	{
		UnsubscribeSharedVariableHighlightState();

		if (_fileSystem?.IsConnected(EditorFileSystem.SignalName.ResourcesReimported, _filesystemChangedCallable)
			== true)
		{
			_fileSystem.Disconnect(EditorFileSystem.SignalName.ResourcesReimported, _filesystemChangedCallable);
		}

		_serializedTabPaths = GetOpenResourcePaths();
		_serializedActiveTab = GetActiveTabIndex();
		_serializedVariablesStates = GetVariablesPanelStates();
		_serializedSelectedVariables = GetSelectedVariableStates();
		_persistedVariablesPanelVisible = _variablePanel?.Visible ?? _persistedVariablesPanelVisible;

		SyncVisualNodePositionsToGraph();
		SyncConnectionsToCurrentGraph();

		if (CurrentGraph is not null && _graphEdit is not null)
		{
			CurrentGraph.ScrollOffset = _graphEdit.ScrollOffset;
			CurrentGraph.Zoom = _graphEdit.Zoom;
		}

		var allConnections = new List<string>();
		_serializedConnectionCounts = new int[_openTabs.Count];
		for (int i = 0; i < _openTabs.Count; i++)
		{
			StatescriptGraph graph = _openTabs[i].GraphResource;
			int count = 0;
			foreach (StatescriptConnection c in graph.Connections)
			{
				allConnections.Add($"{c.FromNode},{c.OutputPort},{c.ToNode},{c.InputPort}");
				count++;
			}

			_serializedConnectionCounts[i] = count;
		}

		_serializedConnections = [.. allConnections];

		DisconnectUISignals();
		ClearGraphEditor();

		if (_tabBar is not null)
		{
			while (_tabBar.GetTabCount() > 0)
			{
				_tabBar.RemoveTab(0);
			}
		}

		DisposeCachedGraphVisuals();

		_openTabs.Clear();
	}

	public void OnAfterDeserialize()
	{
		_filesystemChangedCallable = new Callable(this, nameof(OnFilesystemChanged));

		if (_fileSystem?.
			IsConnected(EditorFileSystem.SignalName.ResourcesReimported, _filesystemChangedCallable) == false)
		{
			_fileSystem.Connect(EditorFileSystem.SignalName.ResourcesReimported, _filesystemChangedCallable);
		}

		ConnectUISignals();
		SubscribeSharedVariableHighlightState();

		if (_serializedTabPaths?.Length > 0)
		{
			_ = RestoreTabsDeferred();
		}
	}

	public override void _Notification(int what)
	{
		base._Notification(what);

		if (what == NotificationThemeChanged)
		{
			UpdateTheme();
		}
	}

	/// <summary>
	/// Sets the <see cref="EditorUndoRedoManager"/> used for undo/redo support.
	/// </summary>
	/// <param name="undoRedo">The undo/redo manager from the editor plugin.</param>
	public void SetUndoRedo(EditorUndoRedoManager undoRedo)
	{
		_undoRedo = undoRedo;
	}

	/// <summary>
	/// Opens a graph resource for editing. If already open, switches to its tab.
	/// </summary>
	/// <param name="graph">The graph resource to edit.</param>
	public void OpenGraph(StatescriptGraph graph)
	{
		if (_tabBar is null || _graphEdit is null)
		{
			return;
		}

		PersistCurrentVariablePanelState();

		for (int i = 0; i < _openTabs.Count; i++)
		{
			if (_openTabs[i].GraphResource == graph || (!string.IsNullOrEmpty(graph.ResourcePath)
				&& _openTabs[i].ResourcePath == graph.ResourcePath))
			{
				SetCurrentTabWithoutLoading(i);
				ApplyVariablesPanelState(i);
				return;
			}
		}

		graph.EnsureEntryNode();

		var tab = new GraphTab(graph)
		{
			VariablesPanelOpen = _variablePanel?.Visible ?? false,
		};

		_openTabs.Add(tab);

		_tabBar.AddTab(graph.StatescriptName);
		SetCurrentTabWithoutLoading(_openTabs.Count - 1);

		LoadGraphIntoEditor(graph);
		UpdateVisibility();
	}

	/// <summary>
	/// Closes the currently active graph tab.
	/// </summary>
	public void CloseCurrentTab()
	{
		if (_tabBar is null || _openTabs.Count == 0)
		{
			return;
		}

		int currentTab = _tabBar.CurrentTab;
		if (currentTab < 0 || currentTab >= _openTabs.Count)
		{
			return;
		}

		CloseTabByIndex(currentTab);
	}

	/// <summary>
	/// Returns the resource paths of all open tabs for state persistence.
	/// </summary>
	/// <returns>An array of resource paths.</returns>
	public string[] GetOpenResourcePaths()
	{
		return [.. GetPersistedTabs().Select(x => x.ResourcePath)];
	}

	/// <summary>
	/// Returns the currently active tab index.
	/// </summary>
	/// <returns>The active tab index, or -1 if no tabs are open.</returns>
	public int GetActiveTabIndex()
	{
		PersistCurrentVariablePanelState();

		if (_tabBar is null)
		{
			return -1;
		}

		int currentTab = _tabBar.CurrentTab;
		if (currentTab < 0 || currentTab >= _openTabs.Count)
		{
			return -1;
		}

		GraphTab current = _openTabs[currentTab];
		if (string.IsNullOrEmpty(current.ResourcePath))
		{
			return -1;
		}

		GraphTab[] persistedTabs = GetPersistedTabs();
		for (int i = 0; i < persistedTabs.Length; i++)
		{
			if (persistedTabs[i].ResourcePath == current.ResourcePath)
			{
				return i;
			}
		}

		return -1;
	}

	/// <summary>
	/// Returns per-tab variables panel visibility states.
	/// </summary>
	/// <returns><see langword="true"/> for tabs with the variables panel open, <see langword="false"/> otherwise.
	/// </returns>
	public bool[] GetVariablesPanelStates()
	{
		PersistCurrentVariablePanelState();
		return [.. GetPersistedTabs().Select(x => x.VariablesPanelOpen)];
	}

	public string?[] GetSelectedVariableStates()
	{
		PersistCurrentVariablePanelState();
		return [.. GetPersistedTabs().Select(x => x.SelectedVariableName)];
	}

	/// <summary>
	/// Saves all open graphs that have a resource path. Called by the plugin's _SaveExternalData
	/// so that Ctrl+S persists statescript graphs alongside scenes.
	/// </summary>
	public void SaveAllOpenGraphs()
	{
		if (_graphEdit is null)
		{
			return;
		}

		SyncVisualNodePositionsToGraph();
		SyncConnectionsToCurrentGraph();

		if (CurrentGraph is not null)
		{
			CurrentGraph.ScrollOffset = _graphEdit.ScrollOffset;
			CurrentGraph.Zoom = _graphEdit.Zoom;
		}

		foreach (StatescriptGraph graph in _openTabs.Select(x => x.GraphResource))
		{
			if (string.IsNullOrEmpty(graph.ResourcePath))
			{
				continue;
			}

			SaveGraphResource(graph);
		}
	}

	/// <summary>
	/// Restores tabs from paths and active index, used by EditorPlugin _SetWindowLayout.
	/// </summary>
	/// <param name="paths">The resource paths of the tabs to restore.</param>
	/// <param name="activeIndex">The index of the tab to make active.</param>
	/// <param name="variablesStates">The visibility states of the variables panel for each tab.</param>
	public void RestoreFromPaths(string[] paths, int activeIndex, bool[]? variablesStates = null)
	{
		if (_tabBar is null || _graphEdit is null)
		{
			return;
		}

		_isLoadingGraph = true;

		_openTabs.Clear();
		while (_tabBar.GetTabCount() > 0)
		{
			_tabBar.RemoveTab(0);
		}

		int skippedTabs = 0;
		for (int i = 0; i < paths.Length; i++)
		{
			string path = paths[i];

			StatescriptGraph? graph = LoadGraphFromPath(path);
			if (graph is null)
			{
				skippedTabs++;
				continue;
			}

			graph.EnsureEntryNode();
			var tab = new GraphTab(graph);

			int currentTab = i - skippedTabs;
			if (variablesStates is not null && currentTab < variablesStates.Length)
			{
				tab.VariablesPanelOpen = variablesStates[currentTab];
			}

			_openTabs.Add(tab);
			_tabBar.AddTab(graph.StatescriptName);
		}

		_isLoadingGraph = false;

		if (activeIndex >= 0 && activeIndex < _openTabs.Count)
		{
			_openTabs[activeIndex].VariablesPanelOpen = _persistedVariablesPanelVisible;
			SetCurrentTabWithoutLoading(activeIndex);
			LoadGraphIntoEditor(_openTabs[activeIndex].GraphResource);
			ApplyVariablesPanelState(activeIndex);
		}
		else if (_openTabs.Count > 0)
		{
			SetCurrentTabWithoutLoading(0);
			LoadGraphIntoEditor(_openTabs[0].GraphResource);
			ApplyVariablesPanelState(0);
		}

		UpdateVisibility();
	}

	private static void SyncNodePositionsToResource(
		StatescriptGraph graph,
		GodotCollections.Dictionary<StringName, Vector2> positions)
	{
		foreach (StatescriptNode node in graph.Nodes)
		{
			if (positions.TryGetValue(node.NodeId, out Vector2 pos))
			{
				node.PositionOffset = pos;
			}
		}
	}

	private static void OnOnlineDocsPressed()
	{
		OS.ShellOpen("https://github.com/gamesmiths-guild/forge-godot/tree/main/docs");
	}

	private static string GetBaseFilePath(string resourcePath)
	{
		int separatorIndex = resourcePath.IndexOf("::", StringComparison.Ordinal);
		return separatorIndex >= 0 ? resourcePath[..separatorIndex] : resourcePath;
	}

	private static bool IsSubResourcePath(string resourcePath)
	{
		return resourcePath.Contains("::", StringComparison.Ordinal);
	}

	private static StatescriptGraph? LoadGraphFromPath(string path)
	{
		if (IsSubResourcePath(path))
		{
			string basePath = GetBaseFilePath(path);
			if (!ResourceLoader.Exists(basePath))
			{
				return null;
			}

			Resource? parentResource = ResourceLoader.Load(basePath);
			if (parentResource is null)
			{
				return null;
			}

			return FindSubResourceGraph(parentResource, path);
		}

		if (!ResourceLoader.Exists(path))
		{
			return null;
		}

		return ResourceLoader.Load<StatescriptGraph>(path);
	}

	private static StatescriptGraph? FindSubResourceGraph(Resource parentResource, string subResourcePath)
	{
		foreach (string? propertyName in parentResource.GetPropertyList()
			.Select(p => p["name"].AsString()))
		{
			Variant value = parentResource.Get(propertyName);
			if (value.Obj is StatescriptGraph graph && graph.ResourcePath == subResourcePath)
			{
				return graph;
			}

			if (value.Obj is Resource nestedResource)
			{
				StatescriptGraph? found = FindSubResourceInNested(nestedResource, subResourcePath);
				if (found is not null)
				{
					return found;
				}
			}
		}

		return null;
	}

	private static StatescriptGraph? FindSubResourceInNested(Resource resource, string subResourcePath)
	{
		if (resource is StatescriptGraph graph && graph.ResourcePath == subResourcePath)
		{
			return graph;
		}

		foreach (string? propertyName in resource.GetPropertyList()
			.Select(p => p["name"].AsString()))
		{
			Variant value = resource.Get(propertyName);
			if (value.Obj is StatescriptGraph nestedGraph && nestedGraph.ResourcePath == subResourcePath)
			{
				return nestedGraph;
			}

			if (value.Obj is Resource nestedResource && nestedResource != resource)
			{
				StatescriptGraph? found = FindSubResourceInNested(nestedResource, subResourcePath);
				if (found is not null)
				{
					return found;
				}
			}
		}

		return null;
	}

	private static void SaveGraphResource(StatescriptGraph graph)
	{
		string path = graph.ResourcePath;

		if (IsSubResourcePath(path))
		{
			string basePath = GetBaseFilePath(path);
			Resource? parentResource = ResourceLoader.Load(basePath);
			if (parentResource is not null)
			{
				ResourceSaver.Save(parentResource);
			}
		}
		else
		{
			ResourceSaver.Save(graph);
		}
	}

	private async Task RestoreTabsDeferred()
	{
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

		if (_serializedTabPaths is null || _serializedTabPaths.Length == 0)
		{
			return;
		}

		string[] paths = _serializedTabPaths;
		int activeTab = _serializedActiveTab;
		bool[]? varStates = _serializedVariablesStates;
		string?[]? selectedVariables = _serializedSelectedVariables;
		string[]? savedConnections = _serializedConnections;
		int[]? connectionCounts = _serializedConnectionCounts;

		_serializedTabPaths = null;
		_serializedActiveTab = -1;
		_serializedVariablesStates = null;
		_serializedSelectedVariables = null;
		_serializedConnections = null;
		_serializedConnectionCounts = null;

		if (_tabBar is null || _graphEdit is null)
		{
			return;
		}

		_isLoadingGraph = true;

		_openTabs.Clear();
		while (_tabBar.GetTabCount() > 0)
		{
			_tabBar.RemoveTab(0);
		}

		int skippedTabs = 0;
		for (int i = 0; i < paths.Length; i++)
		{
			if (!ResourceLoader.Exists(paths[i]))
			{
				skippedTabs++;
				continue;
			}

			StatescriptGraph? graph = ResourceLoader.Load<StatescriptGraph>(paths[i]);
			if (graph is null)
			{
				skippedTabs++;
				continue;
			}

			graph.EnsureEntryNode();
			var tab = new GraphTab(graph);

			int currentTab = i - skippedTabs;
			if (varStates is not null && currentTab < varStates.Length)
			{
				tab.VariablesPanelOpen = varStates[currentTab];
			}

			if (selectedVariables is not null && currentTab < selectedVariables.Length)
			{
				tab.SelectedVariableName = selectedVariables[currentTab];
			}

			_openTabs.Add(tab);
			_tabBar.AddTab(graph.StatescriptName);
		}

		_isLoadingGraph = false;

		if (savedConnections is not null && connectionCounts is not null)
		{
			int offset = 0;
			for (int i = 0; i < _openTabs.Count && i < connectionCounts.Length; i++)
			{
				StatescriptGraph graph = _openTabs[i].GraphResource;
				graph.Connections.Clear();

				for (int j = 0; j < connectionCounts[i] && offset < savedConnections.Length; j++, offset++)
				{
					string[] parts = savedConnections[offset].Split(',');
					if (parts.Length != 4
						|| !int.TryParse(parts[1], out int outPort)
						|| !int.TryParse(parts[3], out int inPort))
					{
						continue;
					}

					graph.Connections.Add(new StatescriptConnection
					{
						FromNode = parts[0],
						OutputPort = outPort,
						ToNode = parts[2],
						InputPort = inPort,
					});
				}
			}
		}

		if (activeTab >= 0 && activeTab < _openTabs.Count)
		{
			_openTabs[activeTab].VariablesPanelOpen = _persistedVariablesPanelVisible;
			SetCurrentTabWithoutLoading(activeTab);
			LoadGraphIntoEditor(_openTabs[activeTab].GraphResource);
			ApplyVariablesPanelState(activeTab);
		}
		else if (_openTabs.Count > 0)
		{
			SetCurrentTabWithoutLoading(0);
			LoadGraphIntoEditor(_openTabs[0].GraphResource);
			ApplyVariablesPanelState(0);
		}

		UpdateVisibility();
	}

	private void CloseTabByIndex(int tabIndex)
	{
		if (_tabBar is null || tabIndex < 0 || tabIndex >= _openTabs.Count)
		{
			return;
		}

		DisposeCachedGraphVisuals(_openTabs[tabIndex]);

		_openTabs.RemoveAt(tabIndex);
		_tabBar.RemoveTab(tabIndex);

		if (_openTabs.Count > 0)
		{
			int newTab = Mathf.Min(tabIndex, _openTabs.Count - 1);
			SetCurrentTabWithoutLoading(newTab);
			LoadGraphIntoEditor(_openTabs[newTab].GraphResource);
			ApplyVariablesPanelState(newTab);
		}
		else
		{
			ClearGraphEditor();
		}

		UpdateVisibility();
	}

	private void BuildUI()
	{
		var vBox = new VBoxContainer
		{
			SizeFlagsVertical = SizeFlags.ExpandFill,
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
		};

		AddChild(vBox);

		_tabBarBackground = new PanelContainer();
		vBox.AddChild(_tabBarBackground);

		var tabBarHBox = new HBoxContainer
		{
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
		};

		_tabBarBackground.AddChild(tabBarHBox);

		_tabBar = new TabBar
		{
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			TabCloseDisplayPolicy = TabBar.CloseButtonDisplayPolicy.ShowActiveOnly,
			DragToRearrangeEnabled = true,
		};

		_tabBar.TabChanged += OnTabChanged;
		_tabBar.TabClosePressed += OnTabClosePressed;
		tabBarHBox.AddChild(_tabBar);

		_contentPanel = new PanelContainer
		{
			SizeFlagsVertical = SizeFlags.ExpandFill,
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
		};

		vBox.AddChild(_contentPanel);

		_splitContainer = new HSplitContainer
		{
			SizeFlagsVertical = SizeFlags.ExpandFill,
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
		};

		_contentPanel.AddChild(_splitContainer);

		_graphEdit = new GraphEdit
		{
			SizeFlagsVertical = SizeFlags.ExpandFill,
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			ShowZoomLabel = true,
			RightDisconnects = true,
			GridPattern = GraphEdit.GridPatternEnum.Dots,
		};

		_graphEdit.ConnectionRequest += OnConnectionRequest;
		_graphEdit.DisconnectionRequest += OnDisconnectionRequest;
		_graphEdit.DeleteNodesRequest += OnDeleteNodesRequest;
		_graphEdit.BeginNodeMove += OnBeginNodeMove;
		_graphEdit.EndNodeMove += OnEndNodeMove;
		_graphEdit.PopupRequest += OnGraphEditPopupRequest;
		_graphEdit.ConnectionToEmpty += OnConnectionToEmpty;
		_graphEdit.ConnectionFromEmpty += OnConnectionFromEmpty;
		_graphEdit.GuiInput += OnGraphEditGuiInput;
		_splitContainer.AddChild(_graphEdit);

		_variablePanel = new StatescriptVariablePanel
		{
			Visible = true,
		};

		_variablePanel.VariablesChanged += OnGraphVariablesChanged;
		_variablePanel.VariableUndoRedoPerformed += OnVariableUndoRedoPerformed;
		_variablePanel.VariableHighlightChanged += OnVariableHighlightChanged;
		_splitContainer.AddChild(_variablePanel);

		if (_undoRedo is not null)
		{
			_variablePanel.SetUndoRedo(_undoRedo);
		}

		HBoxContainer menuHBox = _graphEdit.GetMenuHBox();

		menuHBox.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		var parent = (PanelContainer)menuHBox.GetParent();
		parent.SetAnchorsAndOffsetsPreset(LayoutPreset.TopWide, LayoutPresetMode.Minsize, 10);

		_fileMenuButton = new MenuButton
		{
			Text = "File",
			Flat = false,
			SwitchOnHover = true,
			ThemeTypeVariation = "FlatMenuButton",
		};

		_fileMenuPopup = _fileMenuButton.GetPopup();
#pragma warning disable RCS1130, S3265 // Bitwise operation on enum without Flags attribute
		_fileMenuPopup.AddItem("New Statescript...", 0, Key.N | (Key)KeyModifierMask.MaskCtrl);
		_fileMenuPopup.AddItem("Load Statescript File...", 1, Key.O | (Key)KeyModifierMask.MaskCtrl);
		_fileMenuPopup.AddSeparator();
		_fileMenuPopup.AddItem("Save", 2, Key.S | (Key)KeyModifierMask.MaskCtrl | (Key)KeyModifierMask.MaskAlt);
		_fileMenuPopup.AddItem("Save As...", 3);
		_fileMenuPopup.AddSeparator();
		_fileMenuPopup.AddItem("Close", 4, Key.W | (Key)KeyModifierMask.MaskCtrl);
#pragma warning restore RCS1130, S3265 // Bitwise operation on enum without Flags attribute
		_fileMenuPopup.IdPressed += OnFileMenuIdPressed;

		menuHBox.AddChild(_fileMenuButton);
		menuHBox.MoveChild(_fileMenuButton, 0);

		var separator1 = new VSeparator();
		menuHBox.AddChild(separator1);
		menuHBox.MoveChild(separator1, 1);

		_addNodeButton = new Button
		{
			Text = "Add Node...",
			ThemeTypeVariation = "FlatButton",
		};

		_addNodeButton.Pressed += OnAddNodeButtonPressed;

		menuHBox.AddChild(_addNodeButton);
		menuHBox.MoveChild(_addNodeButton, 2);

		var separator2 = new VSeparator();
		menuHBox.AddChild(separator2);

		_variablesToggleButton = new Button
		{
			Text = "Variables",
			ToggleMode = true,
			ThemeTypeVariation = "FlatButton",
			Icon = EditorInterface.Singleton.GetEditorTheme().GetIcon("SubViewport", "EditorIcons"),
		};

		_variablesToggleButton.Toggled += OnVariablesToggled;
		menuHBox.AddChild(_variablesToggleButton);

		var spacer = new Control
		{
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
		};

		menuHBox.AddChild(spacer);

		_onlineDocsButton = new Button
		{
			Text = "Online Docs",
			ThemeTypeVariation = "FlatButton",
			Icon = EditorInterface.Singleton.GetEditorTheme().GetIcon("ExternalLink", "EditorIcons"),
		};

		_onlineDocsButton.Pressed += OnOnlineDocsPressed;
		menuHBox.AddChild(_onlineDocsButton);

		_emptyLabel = new Label
		{
			Text = "Select a Statescript resource to begin editing.",
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center,
			SizeFlagsVertical = SizeFlags.ExpandFill,
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
		};

		_emptyLabel.AddThemeColorOverride("font_color", new Color(0.6f, 0.6f, 0.6f));
		_contentPanel.AddChild(_emptyLabel);

		_addNodeDialog = new StatescriptAddNodeDialog();
		_addNodeDialog.NodeCreationRequested += OnDialogNodeCreationRequested;
		_addNodeDialog.Canceled += OnDialogCanceled;
		AddChild(_addNodeDialog);

		UpdateTheme();
	}

	private void UpdateTheme()
	{
		if (_tabBarBackground is null || _contentPanel is null)
		{
			return;
		}

		Control baseControl = EditorInterface.Singleton.GetBaseControl();

		StyleBox tabBarStyle = baseControl.GetThemeStylebox("tabbar_background", "TabContainer");
		_tabBarBackground.AddThemeStyleboxOverride("panel", tabBarStyle);

		StyleBox panelStyle = baseControl.GetThemeStylebox("panel", "TabContainer");
		_contentPanel.AddThemeStyleboxOverride("panel", panelStyle);
	}

	private void UpdateVisibility()
	{
		bool hasOpenGraph = _openTabs.Count > 0;

		if (_splitContainer is not null)
		{
			_splitContainer.Visible = hasOpenGraph;
		}

		if (_tabBarBackground is not null)
		{
			_tabBarBackground.Visible = hasOpenGraph;
		}

		if (_emptyLabel is not null)
		{
			_emptyLabel.Visible = !hasOpenGraph;
		}

		if (!hasOpenGraph)
		{
			if (_variablePanel is not null)
			{
				_variablePanel.Visible = false;
			}

			_variablesToggleButton?.SetPressedNoSignal(false);
		}
	}

	private void LoadGraphIntoEditor(StatescriptGraph graph)
	{
		if (_graphEdit is null)
		{
			return;
		}

		GraphTab? tab = FindTab(graph);
		if (tab is null)
		{
			return;
		}

		bool wasLoading = _isLoadingGraph;
		_isLoadingGraph = true;

		DetachVisibleGraphNodes();

		_graphEdit.Zoom = graph.Zoom;

		UpdateNextNodeId(graph);

		tab.CachedGraphNodes.RemoveAll(x => !IsInstanceValid(x));

		if (tab.CachedGraphNodes.Count == 0)
		{
			foreach (StatescriptNode nodeResource in graph.Nodes)
			{
				StatescriptGraphNode graphNode = AddGraphNodeVisual(nodeResource, graph);
				tab.CachedGraphNodes.Add(graphNode);
			}
		}
		else
		{
			AttachCachedGraphNodes(tab);
		}

		_graphEdit.ClearConnections();

		foreach (StatescriptConnection connection in graph.Connections)
		{
			_graphEdit.ConnectNode(
				connection.FromNode,
				ToVisualOutputPort(connection.FromNode, connection.OutputPort),
				connection.ToNode,
				connection.InputPort);
		}

		ReapplyCurrentNodeHighlights();

		_isLoadingGraph = wasLoading;

		_ = ApplyScrollNextFrame(graph.ScrollOffset);
	}

	private async Task ApplyScrollNextFrame(Vector2 offset)
	{
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

		if (_graphEdit is not null)
		{
			_graphEdit.ScrollOffset = offset;
		}
	}

	private void ClearGraphEditor()
	{
		if (_graphEdit is null)
		{
			return;
		}

		_graphEdit.ClearConnections();

		var toRemove = new List<Node>();
		toRemove.AddRange(_graphEdit.GetChildren().Where(x => x is GraphNode));

		foreach (Node node in toRemove)
		{
			if (node is StatescriptGraphNode graphNode)
			{
				RemoveGraphNodeVisual(graphNode);
			}
		}
	}

	private void DetachVisibleGraphNodes()
	{
		if (_graphEdit is null)
		{
			return;
		}

		_graphEdit.ClearConnections();

		var toDetach = new List<Node>();
		toDetach.AddRange(_graphEdit.GetChildren().Where(x => x is GraphNode));

		foreach (Node node in toDetach)
		{
			_graphEdit.RemoveChild(node);
		}
	}

	private StatescriptGraphNode AddGraphNodeVisual(StatescriptNode nodeResource, StatescriptGraph graph)
	{
		var graphNode = new StatescriptGraphNode();
		_graphEdit!.AddChild(graphNode);
		graphNode.Initialize(nodeResource, graph);
		graphNode.SetUndoRedo(_undoRedo);
		graphNode.PropertyBindingChanged += OnGraphNodePropertyBindingChanged;
		return graphNode;
	}

	private void AttachCachedGraphNodes(GraphTab tab)
	{
		if (_graphEdit is null)
		{
			return;
		}

		foreach (StatescriptGraphNode graphNode in tab.CachedGraphNodes)
		{
			if (!IsInstanceValid(graphNode))
			{
				continue;
			}

			if (graphNode.GetParent() is Node parent)
			{
				parent.RemoveChild(graphNode);
			}

			_graphEdit.AddChild(graphNode);
		}
	}

	private void RemoveGraphNodeVisual(StatescriptGraphNode graphNode)
	{
		graphNode.PropertyBindingChanged -= OnGraphNodePropertyBindingChanged;
		graphNode.OnBeforeSerialize();

		if (graphNode.GetParent() is Node parent)
		{
			parent.RemoveChild(graphNode);
		}

		for (int i = 0; i < _openTabs.Count; i++)
		{
			_openTabs[i].CachedGraphNodes.Remove(graphNode);
		}

		graphNode.Free();
	}

	private void DisposeCachedGraphVisuals()
	{
		for (int i = 0; i < _openTabs.Count; i++)
		{
			DisposeCachedGraphVisuals(_openTabs[i]);
		}
	}

	private void DisposeCachedGraphVisuals(GraphTab tab)
	{
		for (int i = tab.CachedGraphNodes.Count - 1; i >= 0; i--)
		{
			StatescriptGraphNode graphNode = tab.CachedGraphNodes[i];
			if (!IsInstanceValid(graphNode))
			{
				tab.CachedGraphNodes.RemoveAt(i);
				continue;
			}

			RemoveGraphNodeVisual(graphNode);
		}
	}

	private GraphTab? FindTab(StatescriptGraph graph)
	{
		for (int i = 0; i < _openTabs.Count; i++)
		{
			if (_openTabs[i].GraphResource == graph || (!string.IsNullOrEmpty(graph.ResourcePath)
				&& _openTabs[i].ResourcePath == graph.ResourcePath))
			{
				return _openTabs[i];
			}
		}

		return null;
	}

	private void SetCurrentTabWithoutLoading(int tabIndex)
	{
		if (_tabBar is null)
		{
			return;
		}

		bool wasLoading = _isLoadingGraph;
		_isLoadingGraph = true;
		_tabBar.CurrentTab = tabIndex;
		_isLoadingGraph = wasLoading;
	}

	private void UpdateNextNodeId(StatescriptGraph graph)
	{
		int maxId = 0;
		foreach (string? nodeId in graph.Nodes.Select(x => x.NodeId))
		{
			if (nodeId.StartsWith("node_", StringComparison.InvariantCultureIgnoreCase)
				&& int.TryParse(nodeId["node_".Length..], out int id)
				&& id >= maxId)
			{
				maxId = id + 1;
			}
		}

		if (maxId > _nextNodeId)
		{
			_nextNodeId = maxId;
		}
	}

	private void RefreshTabTitles()
	{
		if (_tabBar is null)
		{
			return;
		}

		for (int i = 0; i < _openTabs.Count; i++)
		{
			_tabBar.SetTabTitle(i, _openTabs[i].GraphResource.StatescriptName);
		}
	}

	private void SaveGraphStateByIndex(int tabIndex)
	{
		if (tabIndex < 0 || tabIndex >= _openTabs.Count || _graphEdit is null)
		{
			return;
		}

		StatescriptGraph graph = _openTabs[tabIndex].GraphResource;

		graph.ScrollOffset = _graphEdit.ScrollOffset;
		graph.Zoom = _graphEdit.Zoom;

		SyncVisualNodePositionsToGraph();
		SyncConnectionsToGraph(graph);
	}

	private void SaveOutgoingTabState(int newTabIndex)
	{
		if (_graphEdit is null || _openTabs.Count <= 1)
		{
			return;
		}

		StatescriptGraphNode? firstNode = null;
		foreach (Node child in _graphEdit.GetChildren())
		{
			if (child is StatescriptGraphNode statescriptNode)
			{
				firstNode = statescriptNode;
				break;
			}
		}

		if (firstNode?.NodeResource is null)
		{
			return;
		}

		for (int i = 0; i < _openTabs.Count; i++)
		{
			if (i == newTabIndex)
			{
				continue;
			}

			StatescriptGraph graph = _openTabs[i].GraphResource;
			foreach (StatescriptNode node in graph.Nodes)
			{
				if (node == firstNode.NodeResource)
				{
					SaveGraphStateByIndex(i);

					if (_variablePanel is not null)
					{
						_openTabs[i].VariablesPanelOpen = _variablePanel.Visible;
						_openTabs[i].SelectedVariableName = _variablePanel.GetSelectedVariableName();
					}

					return;
				}
			}
		}
	}

	private void OnTabChanged(long tab)
	{
		if (_isLoadingGraph)
		{
			return;
		}

		if (tab >= 0 && tab < _openTabs.Count)
		{
			SaveOutgoingTabState((int)tab);
			LoadGraphIntoEditor(_openTabs[(int)tab].GraphResource);

			ApplyVariablesPanelState((int)tab);
		}
	}

	private void OnTabClosePressed(long tab)
	{
		if (tab >= 0 && tab < _openTabs.Count)
		{
			SaveOutgoingTabState(-1);
			CloseTabByIndex((int)tab);
		}
	}

	private void SyncConnectionsToGraph(StatescriptGraph graph)
	{
		if (_graphEdit is null || _isLoadingGraph)
		{
			return;
		}

		graph.Connections.Clear();
		foreach (GodotCollections.Dictionary connection in _graphEdit.GetConnectionList())
		{
			string fromNode = connection["from_node"].AsString();
			var connectionResource = new StatescriptConnection
			{
				FromNode = fromNode,
				OutputPort = ToRuntimeOutputPort(fromNode, connection["from_port"].AsInt32()),
				ToNode = connection["to_node"].AsString(),
				InputPort = connection["to_port"].AsInt32(),
			};

			graph.Connections.Add(connectionResource);
		}

		graph.EmitChanged();
	}

	private void SyncConnectionsToCurrentGraph()
	{
		StatescriptGraph? graph = CurrentGraph;
		if (graph is not null)
		{
			SyncConnectionsToGraph(graph);
		}
	}

	private void SyncVisualNodePositionsToGraph()
	{
		if (_graphEdit is null)
		{
			return;
		}

		StatescriptGraph? graph = CurrentGraph;
		bool changed = false;

		foreach (Node child in _graphEdit.GetChildren())
		{
			if (child is not StatescriptGraphNode sgn || sgn.NodeResource is null)
			{
				continue;
			}

			if (sgn.NodeResource.PositionOffset == sgn.PositionOffset)
			{
				continue;
			}

			sgn.NodeResource.PositionOffset = sgn.PositionOffset;
			sgn.NodeResource.EmitChanged();
			changed = true;
		}

		if (changed)
		{
			graph?.EmitChanged();
		}
	}

	private void OnVariablesToggled(bool pressed)
	{
		if (_variablePanel is null || _tabBar is null || _openTabs.Count == 0)
		{
			return;
		}

		_variablePanel.Visible = pressed;

		int current = _tabBar.CurrentTab;
		if (current >= 0 && current < _openTabs.Count)
		{
			_openTabs[current].VariablesPanelOpen = pressed;
			_persistedVariablesPanelVisible = pressed;

			if (!pressed)
			{
				_openTabs[current].SelectedVariableName = null;
			}
		}

		if (pressed)
		{
			StatescriptGraph? graph = CurrentGraph;
			if (graph is not null)
			{
				_variablePanel.SetGraph(graph);
				if (current >= 0 && current < _openTabs.Count)
				{
					_variablePanel.RestoreSelectedVariable(_openTabs[current].SelectedVariableName);
				}
			}
		}
		else
		{
			_variablePanel.ClearSelectedVariable();
		}
	}

	private void OnGraphVariablesChanged()
	{
		StatescriptGraph? graph = CurrentGraph;
		if (graph is null)
		{
			return;
		}

		graph.EmitChanged();
		InvalidateCachedGraphVisuals(graph);
		LoadGraphIntoEditor(graph);
	}

	private void OnVariableUndoRedoPerformed()
	{
		EnsureVariablesPanelVisible();
	}

	private void OnVariableHighlightChanged(string? variableName)
	{
		int current = _tabBar?.CurrentTab ?? -1;
		if (current >= 0 && current < _openTabs.Count)
		{
			_openTabs[current].SelectedVariableName = variableName;
		}

		if (!string.IsNullOrEmpty(variableName))
		{
			SharedVariableHighlightState.SetSelection(null, null);
		}

		ReapplyCurrentNodeHighlights();
	}

	private void OnGraphNodePropertyBindingChanged()
	{
		ReapplyCurrentNodeHighlights();
	}

	private void SubscribeSharedVariableHighlightState()
	{
		if (_sharedVariableHighlightSubscribed)
		{
			return;
		}

		SharedVariableHighlightState.Changed += OnSharedVariableHighlightChanged;
		_sharedVariableHighlightSubscribed = true;
	}

	private void UnsubscribeSharedVariableHighlightState()
	{
		if (!_sharedVariableHighlightSubscribed)
		{
			return;
		}

		SharedVariableHighlightState.Changed -= OnSharedVariableHighlightChanged;
		_sharedVariableHighlightSubscribed = false;
	}

	private void OnSharedVariableHighlightChanged()
	{
		if (SharedVariableHighlightState.HasAnySelection())
		{
			ClearGraphVariableSelections();
			return;
		}

		ApplySharedVariableHighlightToNodes();
	}

	private void ApplySharedVariableHighlightToNodes()
	{
		if (_graphEdit is null)
		{
			return;
		}

		bool hasSelection = SharedVariableHighlightState.TryGetActiveSelection(
			out string sharedVariableSetPath,
			out string sharedVariableName);

		foreach (Node child in _graphEdit.GetChildren())
		{
			if (child is StatescriptGraphNode graphNode)
			{
				graphNode.SetHighlightedSharedVariable(
					hasSelection ? sharedVariableSetPath : null,
					hasSelection ? sharedVariableName : null);
			}
		}
	}

	private void ReapplyCurrentNodeHighlights()
	{
		if (_graphEdit is null)
		{
			return;
		}

		string? selectedVariableName = null;
		int current = _tabBar?.CurrentTab ?? -1;
		if (current >= 0 && current < _openTabs.Count)
		{
			selectedVariableName = _openTabs[current].SelectedVariableName;
		}

		foreach (Node child in _graphEdit.GetChildren())
		{
			if (child is StatescriptGraphNode graphNode)
			{
				graphNode.SetHighlightedVariable(selectedVariableName);
			}
		}

		ApplySharedVariableHighlightToNodes();
	}

	private void ClearGraphVariableSelections()
	{
		for (int i = 0; i < _openTabs.Count; i++)
		{
			_openTabs[i].SelectedVariableName = null;
		}

		if (_variablePanel is not null)
		{
			_variablePanel.ClearSelectedVariable();
			return;
		}

		ReapplyCurrentNodeHighlights();
	}

	private void EnsureVariablesPanelVisible()
	{
		if (_variablePanel is null || _variablesToggleButton is null)
		{
			return;
		}

		_variablePanel.Visible = true;
		_variablesToggleButton.SetPressedNoSignal(true);

		int current = _tabBar?.CurrentTab ?? -1;
		if (current >= 0 && current < _openTabs.Count)
		{
			_openTabs[current].VariablesPanelOpen = true;
		}

		StatescriptGraph? graph = CurrentGraph;
		if (graph is not null)
		{
			_variablePanel.SetGraph(graph);
		}
	}

	private void InvalidateCachedGraphVisuals(StatescriptGraph graph)
	{
		GraphTab? tab = FindTab(graph);
		if (tab is null)
		{
			return;
		}

		for (int i = tab.CachedGraphNodes.Count - 1; i >= 0; i--)
		{
			StatescriptGraphNode graphNode = tab.CachedGraphNodes[i];
			if (!IsInstanceValid(graphNode))
			{
				tab.CachedGraphNodes.RemoveAt(i);
				continue;
			}

			RemoveGraphNodeVisual(graphNode);
		}
	}

	private void OnFilesystemChanged()
	{
		for (int i = 0; i < _openTabs.Count; i++)
		{
			_openTabs[i].UpdateCachedPathIfMissing();
		}

		for (int i = _openTabs.Count - 1; i >= 0; i--)
		{
			string path = _openTabs[i].ResourcePath;

			if (string.IsNullOrEmpty(path))
			{
				continue;
			}

			string filePath = GetBaseFilePath(path);
			if (!FileAccess.FileExists(filePath))
			{
				CloseTabByIndex(i);
			}
		}

		RefreshTabTitles();
	}

	private void ApplyVariablesPanelState(int tabIndex)
	{
		if (_variablePanel is null || _variablesToggleButton is null
			|| tabIndex < 0 || tabIndex >= _openTabs.Count)
		{
			return;
		}

		bool shouldShow = _openTabs[tabIndex].VariablesPanelOpen;

		if ((_tabBar?.CurrentTab ?? -1) == tabIndex)
		{
			shouldShow = _persistedVariablesPanelVisible;
			_openTabs[tabIndex].VariablesPanelOpen = shouldShow;
		}

		_variablePanel.Visible = shouldShow;
		_variablesToggleButton.SetPressedNoSignal(shouldShow);

		if (shouldShow)
		{
			_variablePanel.SetGraph(_openTabs[tabIndex].GraphResource);
			_variablePanel.RestoreSelectedVariable(_openTabs[tabIndex].SelectedVariableName);
		}
		else
		{
			_variablePanel.ClearSelectedVariable();
		}
	}

	private void PersistCurrentVariablePanelState()
	{
		if (_variablePanel is null || _tabBar is null)
		{
			return;
		}

		int current = _tabBar.CurrentTab;
		if (current < 0 || current >= _openTabs.Count)
		{
			return;
		}

		_openTabs[current].VariablesPanelOpen = _variablePanel.Visible;
		_openTabs[current].SelectedVariableName = _variablePanel.GetSelectedVariableName();
		_persistedVariablesPanelVisible = _variablePanel.Visible;
	}

	private GraphTab[] GetPersistedTabs()
	{
		PersistCurrentVariablePanelState();
		return [.. _openTabs.Where(x => !string.IsNullOrEmpty(x.ResourcePath))];
	}

	private void OnGraphEditGuiInput(InputEvent @event)
	{
		if (@event is InputEventKey { Pressed: true, Keycode: Key.D, CtrlPressed: true })
		{
			DuplicateSelectedNodes();
			GetViewport().SetInputAsHandled();
		}
	}

	private void DisconnectUISignals()
	{
		if (_tabBar is not null)
		{
			_tabBar.TabChanged -= OnTabChanged;
			_tabBar.TabClosePressed -= OnTabClosePressed;
		}

		if (_graphEdit is not null)
		{
			_graphEdit.ConnectionRequest -= OnConnectionRequest;
			_graphEdit.DisconnectionRequest -= OnDisconnectionRequest;
			_graphEdit.DeleteNodesRequest -= OnDeleteNodesRequest;
			_graphEdit.BeginNodeMove -= OnBeginNodeMove;
			_graphEdit.EndNodeMove -= OnEndNodeMove;
			_graphEdit.PopupRequest -= OnGraphEditPopupRequest;
			_graphEdit.ConnectionToEmpty -= OnConnectionToEmpty;
			_graphEdit.ConnectionFromEmpty -= OnConnectionFromEmpty;
			_graphEdit.GuiInput -= OnGraphEditGuiInput;
		}

		if (_fileMenuPopup is not null)
		{
			_fileMenuPopup.IdPressed -= OnFileMenuIdPressed;
		}

		if (_addNodeButton is not null)
		{
			_addNodeButton.Pressed -= OnAddNodeButtonPressed;
		}

		if (_variablesToggleButton is not null)
		{
			_variablesToggleButton.Toggled -= OnVariablesToggled;
		}

		if (_onlineDocsButton is not null)
		{
			_onlineDocsButton.Pressed -= OnOnlineDocsPressed;
		}

		if (_addNodeDialog is not null)
		{
			_addNodeDialog.Canceled -= OnDialogCanceled;
			_addNodeDialog.NodeCreationRequested -= OnDialogNodeCreationRequested;
		}

		if (_variablePanel is not null)
		{
			_variablePanel.VariablesChanged -= OnGraphVariablesChanged;
			_variablePanel.VariableUndoRedoPerformed -= OnVariableUndoRedoPerformed;
			_variablePanel.VariableHighlightChanged -= OnVariableHighlightChanged;
		}
	}

	private void ConnectUISignals()
	{
		if (_tabBar is not null)
		{
			_tabBar.TabChanged += OnTabChanged;
			_tabBar.TabClosePressed += OnTabClosePressed;
		}

		if (_graphEdit is not null)
		{
			_graphEdit.ConnectionRequest += OnConnectionRequest;
			_graphEdit.DisconnectionRequest += OnDisconnectionRequest;
			_graphEdit.DeleteNodesRequest += OnDeleteNodesRequest;
			_graphEdit.BeginNodeMove += OnBeginNodeMove;
			_graphEdit.EndNodeMove += OnEndNodeMove;
			_graphEdit.PopupRequest += OnGraphEditPopupRequest;
			_graphEdit.ConnectionToEmpty += OnConnectionToEmpty;
			_graphEdit.ConnectionFromEmpty += OnConnectionFromEmpty;
			_graphEdit.GuiInput += OnGraphEditGuiInput;
		}

		if (_fileMenuPopup is not null)
		{
			_fileMenuPopup.IdPressed += OnFileMenuIdPressed;
		}

		if (_addNodeButton is not null)
		{
			_addNodeButton.Pressed += OnAddNodeButtonPressed;
		}

		if (_variablesToggleButton is not null)
		{
			_variablesToggleButton.Toggled += OnVariablesToggled;
		}

		if (_onlineDocsButton is not null)
		{
			_onlineDocsButton.Pressed += OnOnlineDocsPressed;
		}

		if (_addNodeDialog is not null)
		{
			_addNodeDialog.Canceled += OnDialogCanceled;
			_addNodeDialog.NodeCreationRequested += OnDialogNodeCreationRequested;
		}

		if (_variablePanel is not null)
		{
			_variablePanel.VariablesChanged += OnGraphVariablesChanged;
			_variablePanel.VariableUndoRedoPerformed += OnVariableUndoRedoPerformed;
			_variablePanel.VariableHighlightChanged += OnVariableHighlightChanged;
		}
	}

	private int FindFirstEnabledInputPort(string nodeId)
	{
		if (_graphEdit is null)
		{
			return -1;
		}

		Node? child = _graphEdit.GetNodeOrNull(nodeId);
		if (child is not GraphNode graphNode)
		{
			return -1;
		}

		for (int i = 0; i < graphNode.GetChildCount(); i++)
		{
			if (graphNode.IsSlotEnabledLeft(i))
			{
				return i;
			}
		}

		return -1;
	}

	private int FindFirstEnabledOutputPort(string nodeId)
	{
		if (_graphEdit is null)
		{
			return -1;
		}

		Node? child = _graphEdit.GetNodeOrNull(nodeId);
		if (child is not GraphNode graphNode)
		{
			return -1;
		}

		for (int i = 0; i < graphNode.GetChildCount(); i++)
		{
			if (graphNode.IsSlotEnabledRight(i))
			{
				return i;
			}
		}

		return -1;
	}

	private StatescriptGraphNode? FindGraphNodeVisual(string nodeId)
	{
		if (_graphEdit is null)
		{
			return null;
		}

		return _graphEdit.GetNodeOrNull(nodeId) as StatescriptGraphNode;
	}

	private int ToRuntimeOutputPort(string nodeId, int visualOutputPort)
	{
		return FindGraphNodeVisual(nodeId)?.VisualToRuntimeOutputPort(visualOutputPort) ?? visualOutputPort;
	}

	private int ToVisualOutputPort(string nodeId, int runtimeOutputPort)
	{
		return FindGraphNodeVisual(nodeId)?.RuntimeToVisualOutputPort(runtimeOutputPort) ?? runtimeOutputPort;
	}

	private sealed class GraphTab
	{
		private string _cachedPath;

		public StatescriptGraph GraphResource { get; }

		public List<StatescriptGraphNode> CachedGraphNodes { get; } = [];

		public string ResourcePath => !string.IsNullOrEmpty(GraphResource?.ResourcePath)
			? GraphResource.ResourcePath
			: _cachedPath;

		public bool VariablesPanelOpen { get; set; }

		public string? SelectedVariableName { get; set; }

		public GraphTab(StatescriptGraph graphResource)
		{
			GraphResource = graphResource;
			_cachedPath = graphResource?.ResourcePath ?? string.Empty;
		}

		public void UpdateCachedPathIfMissing()
		{
			if (GraphResource is null)
			{
				return;
			}

			if (!string.IsNullOrEmpty(GraphResource.ResourcePath))
			{
				_cachedPath = GraphResource.ResourcePath;
			}
		}
	}
}
#endif
