// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using System.Collections.Generic;
using System.Linq;
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Godot;
using GodotCollections = Godot.Collections;

namespace Gamesmiths.Forge.Godot.Editor.Statescript;

/// <summary>
/// Main editor panel for Statescript graphs. Supports editing multiple graphs via tabs.
/// Designed to be shown in the bottom panel area of the Godot editor.
/// </summary>
[Tool]
public partial class StatescriptGraphEditorDock : EditorDock
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
	private Button? _variablesToggleButton;
	private Button? _onlineDocsButton;

	private AcceptDialog? _newStatescriptDialog;
	private LineEdit? _newStatescriptNameEdit;
	private LineEdit? _newStatescriptPathEdit;

	private EditorUndoRedoManager? _undoRedo;

	private int _nextNodeId;
	private bool _isLoadingGraph;

	private string? _pendingConnectionNode;
	private int _pendingConnectionPort;
	private bool _pendingConnectionIsOutput;

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

		EditorInterface.Singleton.GetResourceFilesystem().FilesystemChanged += OnFilesystemChanged;
	}

	public override void _ExitTree()
	{
		base._ExitTree();

		EditorInterface.Singleton.GetResourceFilesystem().FilesystemChanged -= OnFilesystemChanged;
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

		for (var i = 0; i < _openTabs.Count; i++)
		{
			if (_openTabs[i].GraphResource == graph || (!string.IsNullOrEmpty(graph.ResourcePath)
				&& _openTabs[i].ResourcePath == graph.ResourcePath))
			{
				_tabBar.CurrentTab = i;
				return;
			}
		}

		graph.EnsureEntryNode();

		var tab = new GraphTab(graph);
		_openTabs.Add(tab);

		_tabBar.AddTab(graph.StatescriptName);
		_tabBar.CurrentTab = _openTabs.Count - 1;

		LoadGraphIntoEditor(graph);
		UpdateVisibility();
		// EditorPlugin.QueueSaveLayout();
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

		var currentTab = _tabBar.CurrentTab;
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
		return [.. _openTabs.Select(x => x.ResourcePath)];
	}

	/// <summary>
	/// Returns the currently active tab index.
	/// </summary>
	/// <returns>>The active tab index, or 1 if no tabs are open.</returns>
	public int GetActiveTabIndex()
	{
		return _tabBar?.CurrentTab ?? -1;
	}

	/// <summary>
	/// Returns per-tab variables panel visibility states.
	/// </summary>
	/// <returns><see langword="true"/> for tabs with the variables panel open, <see langword="false"/> otherwise.
	/// </returns>
	public bool[] GetVariablesPanelStates()
	{
		return [.. _openTabs.Select(x => x.VariablesPanelOpen)];
	}

	/// <summary>
	/// Restores tabs from paths and active index, used by EditorPlugin _SetState.
	/// </summary>
	/// <param name="paths">The resource paths of the tabs to restore.</param>
	/// <param name="activeIndex">The index of the tab to make active.</param>
	/// <param name="variablesStates">The visibility states of the variables panel for each tab.</param>
	public void RestoreFromPaths(string[] paths, int activeIndex, bool[]? variablesStates = null)
	{
		GD.Print("Restaurando abas...");

		_isLoadingGraph = true;

		_openTabs.Clear();
		if (_tabBar is not null)
		{
			while (_tabBar.GetTabCount() > 0)
			{
				_tabBar.RemoveTab(0);
			}
		}

		for (var i = 0; i < paths.Length; i++)
		{
			var path = paths[i];
			if (!ResourceLoader.Exists(path))
			{
				continue;
			}

			StatescriptGraph? graph = ResourceLoader.Load<StatescriptGraph>(path);
			if (graph is null)
			{
				continue;
			}

			graph.EnsureEntryNode();
			var tab = new GraphTab(graph);

			if (variablesStates is not null && i < variablesStates.Length)
			{
				tab.VariablesPanelOpen = variablesStates[i];
			}

			_openTabs.Add(tab);
			_tabBar?.AddTab(graph.StatescriptName);
		}

		_isLoadingGraph = false;

		if (_tabBar is not null && activeIndex >= 0 && activeIndex < _openTabs.Count)
		{
			_tabBar.CurrentTab = activeIndex;
			LoadGraphIntoEditor(_openTabs[activeIndex].GraphResource);
			ApplyVariablesPanelState(activeIndex);
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

	private void CloseTabByIndex(int tabIndex)
	{
		if (_tabBar is null || tabIndex < 0 || tabIndex >= _openTabs.Count)
		{
			return;
		}

		_openTabs.RemoveAt(tabIndex);
		_tabBar.RemoveTab(tabIndex);

		if (_openTabs.Count > 0)
		{
			var newTab = Mathf.Min(tabIndex, _openTabs.Count - 1);
			_tabBar.CurrentTab = newTab;
			LoadGraphIntoEditor(_openTabs[newTab].GraphResource);
			ApplyVariablesPanelState(newTab);
		}
		else
		{
			ClearGraphEditor();
		}

		UpdateVisibility();
		// EditorPlugin.QueueSaveLayout();
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
		_splitContainer.AddChild(_variablePanel);

		HBoxContainer menuHBox = _graphEdit.GetMenuHBox();

		menuHBox.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		//menuHBox.CustomMinimumSize = new Vector2(10000, 0);

		_fileMenuButton = new MenuButton
		{
			Text = "File",
			Flat = true,
			SwitchOnHover = true,
		};

		PopupMenu filePopup = _fileMenuButton.GetPopup();
#pragma warning disable RCS1130, S3265 // Bitwise operation on enum without Flags attribute
		filePopup.AddItem("New Statescript...", 0, Key.N | (Key)KeyModifierMask.MaskCtrl);
		filePopup.AddItem("Load Statescript File...", 1, Key.O | (Key)KeyModifierMask.MaskCtrl);
		filePopup.AddSeparator();
		filePopup.AddItem("Save", 2, Key.S | (Key)KeyModifierMask.MaskCtrl | (Key)KeyModifierMask.MaskAlt);
		filePopup.AddItem("Save As...", 3);
		filePopup.AddSeparator();
		filePopup.AddItem("Close", 4, Key.W | (Key)KeyModifierMask.MaskCtrl);
#pragma warning restore RCS1130, S3265 // Bitwise operation on enum without Flags attribute
		filePopup.IdPressed += OnFileMenuIdPressed;

		menuHBox.AddChild(_fileMenuButton);
		menuHBox.MoveChild(_fileMenuButton, 0);

		var separator1 = new VSeparator();
		menuHBox.AddChild(separator1);
		menuHBox.MoveChild(separator1, 1);

		_addNodeButton = new Button
		{
			Text = "Add Node...",
			Flat = true,
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
			Flat = true,
		};

		_variablesToggleButton.Toggled += OnVariablesToggled;
		menuHBox.AddChild(_variablesToggleButton);

		var separator3 = new VSeparator();
		menuHBox.AddChild(separator3);

		_onlineDocsButton = new Button
		{
			Text = "Online Docs",
			Flat = true,
			//Alignment = HorizontalAlignment.Right,
		};

		_onlineDocsButton.Pressed += () =>
		{
			OS.ShellOpen("https://github.com/gamesmiths-guild/forge-godot/tree/main/docs");
		};

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

	private void OnFileMenuIdPressed(long id)
	{
		switch ((int)id)
		{
			case 0:
				ShowNewStatescriptDialog();
				break;

			case 1:
				ShowLoadStatescriptDialog();
				break;

			case 2:
				OnSavePressed();
				break;

			case 3:
				ShowSaveAsDialog();
				break;

			case 4:
				CloseCurrentTab();
				break;
		}
	}

	private void ShowNewStatescriptDialog()
	{
		_newStatescriptDialog?.QueueFree();

		_newStatescriptDialog = new AcceptDialog
		{
			Title = "Create Statescript",
			Size = new Vector2I(400, 140),
			Exclusive = true,
		};

		var vBox = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };

		var pathRow = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		vBox.AddChild(pathRow);

		pathRow.AddChild(new Label { Text = "Path:", CustomMinimumSize = new Vector2(50, 0) });

		_newStatescriptPathEdit = new LineEdit
		{
			Text = "res://new_statescript.tres",
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
		};

		pathRow.AddChild(_newStatescriptPathEdit);

		_newStatescriptDialog.AddChild(vBox);
		_newStatescriptDialog.Confirmed += OnNewStatescriptConfirmed;

		AddChild(_newStatescriptDialog);
		_newStatescriptDialog.PopupCentered();
	}

	private void OnNewStatescriptConfirmed()
	{
		if (_newStatescriptPathEdit is null)
		{
			return;
		}

		var path = _newStatescriptPathEdit.Text.Trim();
		if (string.IsNullOrEmpty(path))
		{
			return;
		}

		if (!path.EndsWith(".tres", StringComparison.OrdinalIgnoreCase))
		{
			path += ".tres";
		}

		var graph = new StatescriptGraph();
		graph.EnsureEntryNode();

		graph.StatescriptName = path.GetFile().GetBaseName();

		ResourceSaver.Save(graph, path);
		EditorInterface.Singleton.GetResourceFilesystem().Scan();

		graph = ResourceLoader.Load<StatescriptGraph>(path);
		if (graph is not null)
		{
			OpenGraph(graph);
		}

		_newStatescriptDialog?.QueueFree();
		_newStatescriptDialog = null;
	}

	private void ShowLoadStatescriptDialog()
	{
		var dialog = new EditorFileDialog
		{
			FileMode = FileDialog.FileModeEnum.OpenFile,
			Title = "Load Statescript File",
			Access = FileDialog.AccessEnum.Resources,
		};

		dialog.AddFilter("*.tres", "Godot Resource");
		dialog.FileSelected += path =>
		{
			StatescriptGraph? graph = ResourceLoader.Load<StatescriptGraph>(path);
			if (graph is not null)
			{
				OpenGraph(graph);
			}
			else
			{
				GD.PushWarning($"Failed to load StatescriptGraph from: {path}");
			}

			dialog.QueueFree();
		};

		dialog.Canceled += dialog.QueueFree;

		AddChild(dialog);
		dialog.PopupCentered(new Vector2I(700, 500));
	}

	private void ShowSaveAsDialog()
	{
		StatescriptGraph? graph = CurrentGraph;
		if (graph is null)
		{
			return;
		}

		var dialog = new EditorFileDialog
		{
			FileMode = FileDialog.FileModeEnum.SaveFile,
			Title = "Save Statescript As",
			Access = FileDialog.AccessEnum.Resources,
		};

		dialog.AddFilter("*.tres", "Godot Resource");
		dialog.FileSelected += path =>
		{
			if (_graphEdit is not null)
			{
				graph.ScrollOffset = _graphEdit.ScrollOffset;
				graph.Zoom = _graphEdit.Zoom;
				SyncVisualNodePositionsToGraph();
				SyncConnectionsToCurrentGraph();
			}

			ResourceSaver.Save(graph, path);
			EditorInterface.Singleton.GetResourceFilesystem().Scan();
			GD.Print($"Statescript graph saved as: {path}");

			StatescriptGraph? savedGraph = ResourceLoader.Load<StatescriptGraph>(path);
			if (savedGraph is not null)
			{
				OpenGraph(savedGraph);
			}

			dialog.QueueFree();
		};

		dialog.Canceled += dialog.QueueFree;

		AddChild(dialog);
		dialog.PopupCentered(new Vector2I(700, 500));
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
		var hasOpenGraph = _openTabs.Count > 0;

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

		var wasLoading = _isLoadingGraph;
		_isLoadingGraph = true;

		ClearGraphEditor();

		_graphEdit.ScrollOffset = graph.ScrollOffset;
		_graphEdit.Zoom = graph.Zoom;

		UpdateNextNodeId(graph);

		foreach (StatescriptNode nodeResource in graph.Nodes)
		{
			var graphNode = new StatescriptGraphNode();
			_graphEdit.AddChild(graphNode);
			graphNode.Initialize(nodeResource, graph);
		}

		foreach (StatescriptConnection connection in graph.Connections)
		{
			_graphEdit.ConnectNode(
				connection.FromNode,
				connection.OutputPort,
				connection.ToNode,
				connection.InputPort);
		}

		_isLoadingGraph = wasLoading;
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
			_graphEdit.RemoveChild(node);
			node.Free();
		}
	}

	private void UpdateNextNodeId(StatescriptGraph graph)
	{
		var maxId = 0;
		foreach (var nodeId in graph.Nodes.Select(x => x.NodeId))
		{
			if (nodeId.StartsWith("node_", StringComparison.InvariantCultureIgnoreCase)
				&& int.TryParse(nodeId["node_".Length..], out var id)
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

		for (var i = 0; i < _openTabs.Count; i++)
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

		for (var i = 0; i < _openTabs.Count; i++)
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
			var connectionResource = new StatescriptConnection
			{
				FromNode = connection["from_node"].AsString(),
				OutputPort = connection["from_port"].AsInt32(),
				ToNode = connection["to_node"].AsString(),
				InputPort = connection["to_port"].AsInt32(),
			};

			graph.Connections.Add(connectionResource);
		}
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

		foreach (Node child in _graphEdit.GetChildren())
		{
			if (child is not StatescriptGraphNode sgn || sgn.NodeResource is null)
			{
				continue;
			}

			sgn.NodeResource.PositionOffset = sgn.PositionOffset;
		}
	}

	private void OnVariablesToggled(bool pressed)
	{
		if (_variablePanel is null || _tabBar is null || _openTabs.Count == 0)
		{
			return;
		}

		_variablePanel.Visible = pressed;

		var current = _tabBar.CurrentTab;
		if (current >= 0 && current < _openTabs.Count)
		{
			_openTabs[current].VariablesPanelOpen = pressed;
		}

		if (pressed)
		{
			StatescriptGraph? graph = CurrentGraph;
			if (graph is not null)
			{
				_variablePanel.SetGraph(graph);
			}
		}
	}

	private void OnGraphVariablesChanged()
	{
		StatescriptGraph? graph = CurrentGraph;
		if (graph is null)
		{
			return;
		}

		LoadGraphIntoEditor(graph);
	}

	private void OnFilesystemChanged()
	{
		EditorFileSystem filesystem = EditorInterface.Singleton.GetResourceFilesystem();

		for (var i = _openTabs.Count - 1; i >= 0; i--)
		{
			var path = _openTabs[i].ResourcePath;

			if (string.IsNullOrEmpty(path))
			{
				continue;
			}

			if (string.IsNullOrEmpty(filesystem.GetFileType(path)))
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

		var shouldShow = _openTabs[tabIndex].VariablesPanelOpen;
		_variablePanel.Visible = shouldShow;
		_variablesToggleButton.SetPressedNoSignal(shouldShow);

		if (shouldShow)
		{
			_variablePanel.SetGraph(_openTabs[tabIndex].GraphResource);
		}
	}

	private void OnBeginNodeMove()
	{
		if (_graphEdit is null)
		{
			return;
		}

		_preMovePositions.Clear();
		foreach (Node child in _graphEdit.GetChildren())
		{
			if (child is StatescriptGraphNode { Selected: true } sgn)
			{
				_preMovePositions[sgn.Name] = sgn.PositionOffset;
			}
		}
	}

	private void OnEndNodeMove()
	{
		StatescriptGraph? graph = CurrentGraph;
		if (graph is null || _graphEdit is null || _preMovePositions.Count == 0)
		{
			return;
		}

		var movedNodes = new GodotCollections.Dictionary<StringName, Vector2>();
		var oldPositions = new GodotCollections.Dictionary<StringName, Vector2>();

		foreach (Node child in _graphEdit.GetChildren())
		{
			if (child is not StatescriptGraphNode sgn || !_preMovePositions.TryGetValue(sgn.Name, out Vector2 oldPos))
			{
				continue;
			}

			Vector2 newPos = sgn.PositionOffset;
			if (oldPos != newPos)
			{
				movedNodes[sgn.Name] = newPos;
				oldPositions[sgn.Name] = oldPos;
			}
		}

		_preMovePositions.Clear();

		if (movedNodes.Count == 0)
		{
			return;
		}

		if (_undoRedo is not null)
		{
			_undoRedo.CreateAction("Move Statescript Node(s)", customContext: graph);
			_undoRedo.AddDoMethod(this, MethodName.DoMoveNodes, graph, movedNodes);
			_undoRedo.AddUndoMethod(this, MethodName.DoMoveNodes, graph, oldPositions);
			_undoRedo.CommitAction(false);
		}

		SyncNodePositionsToResource(graph, movedNodes);
	}

	private void DoMoveNodes(
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

		if (CurrentGraph == graph && _graphEdit is not null)
		{
			foreach (Node child in _graphEdit.GetChildren())
			{
				if (child is StatescriptGraphNode sgn && positions.TryGetValue(sgn.Name, out Vector2 pos))
				{
					sgn.PositionOffset = pos;
				}
			}
		}
	}

	private void OnGraphEditPopupRequest(Vector2 atPosition)
	{
		if (CurrentGraph is null || _graphEdit is null || _addNodeDialog is null)
		{
			return;
		}

		ClearPendingConnection();

		Vector2 graphPosition = (_graphEdit.ScrollOffset + atPosition) / _graphEdit.Zoom;
		var screenPosition = (Vector2I)(_graphEdit.GetScreenPosition() + atPosition);

		_addNodeDialog.ShowAtPosition(graphPosition, screenPosition);
	}

	private void OnConnectionToEmpty(StringName fromNode, long fromPort, Vector2 releasePosition)
	{
		if (CurrentGraph is null || _graphEdit is null || _addNodeDialog is null)
		{
			return;
		}

		_pendingConnectionNode = fromNode;
		_pendingConnectionPort = (int)fromPort;
		_pendingConnectionIsOutput = true;

		Vector2 graphPosition = (_graphEdit.ScrollOffset + releasePosition) / _graphEdit.Zoom;
		var screenPosition = (Vector2I)(_graphEdit.GetScreenPosition() + releasePosition);

		_addNodeDialog.ShowAtPosition(graphPosition, screenPosition);
	}

	private void OnConnectionFromEmpty(StringName toNode, long toPort, Vector2 releasePosition)
	{
		if (CurrentGraph is null || _graphEdit is null || _addNodeDialog is null)
		{
			return;
		}

		_pendingConnectionNode = toNode;
		_pendingConnectionPort = (int)toPort;
		_pendingConnectionIsOutput = false;

		Vector2 graphPosition = (_graphEdit.ScrollOffset + releasePosition) / _graphEdit.Zoom;
		var screenPosition = (Vector2I)(_graphEdit.GetScreenPosition() + releasePosition);

		_addNodeDialog.ShowAtPosition(graphPosition, screenPosition);
	}

	private void OnDialogNodeCreationRequested(
		StatescriptNodeDiscovery.NodeTypeInfo? typeInfo,
		StatescriptNodeType nodeType,
		Vector2 position)
	{
		string newNodeId;

		if (typeInfo is not null)
		{
			newNodeId = AddNodeAtPosition(nodeType, typeInfo.DisplayName, typeInfo.RuntimeTypeName, position);
		}
		else
		{
			newNodeId = AddNodeAtPosition(StatescriptNodeType.Exit, "Exit", string.Empty, position);
		}

		if (_pendingConnectionNode is not null && _graphEdit is not null)
		{
			if (_pendingConnectionIsOutput)
			{
				var inputPort = FindFirstEnabledInputPort(newNodeId);
				if (inputPort >= 0)
				{
					OnConnectionRequest(
						_pendingConnectionNode,
						_pendingConnectionPort,
						newNodeId,
						inputPort);
				}
			}
			else
			{
				var outputPort = FindFirstEnabledOutputPort(newNodeId);
				if (outputPort >= 0)
				{
					OnConnectionRequest(
						newNodeId,
						outputPort,
						_pendingConnectionNode,
						_pendingConnectionPort);
				}
			}

			ClearPendingConnection();
		}
	}

	private void UndoAddNode(StatescriptGraph graph, StatescriptNode nodeResource)
	{
		graph.Nodes.Remove(nodeResource);

		if (CurrentGraph == graph)
		{
			LoadGraphIntoEditor(graph);
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

		for (var i = 0; i < graphNode.GetChildCount(); i++)
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

		for (var i = 0; i < graphNode.GetChildCount(); i++)
		{
			if (graphNode.IsSlotEnabledRight(i))
			{
				return i;
			}
		}

		return -1;
	}

	private void OnGraphEditGuiInput(InputEvent @event)
	{
		if (@event is InputEventKey { Pressed: true, Keycode: Key.D, CtrlPressed: true })
		{
			DuplicateSelectedNodes();
			GetViewport().SetInputAsHandled();
		}
	}

	private void DuplicateSelectedNodes()
	{
		StatescriptGraph? graph = CurrentGraph;
		if (graph is null || _graphEdit is null)
		{
			return;
		}

		var selectedNodes = new List<StatescriptGraphNode>();

		foreach (Node child in _graphEdit.GetChildren())
		{
			if (child is StatescriptGraphNode { Selected: true } statescriptNode
				&& statescriptNode.NodeResource is not null
				&& statescriptNode.NodeResource.NodeType != StatescriptNodeType.Entry)
			{
				selectedNodes.Add(statescriptNode);
			}
		}

		if (selectedNodes.Count == 0)
		{
			return;
		}

		foreach (StatescriptGraphNode sgn in selectedNodes)
		{
			sgn.Selected = false;
		}

		var duplicatedIds = new Dictionary<string, string>();
		const float offset = 40f;

		foreach (StatescriptGraphNode sgn in selectedNodes)
		{
			StatescriptNode original = sgn.NodeResource!;
			var newNodeId = $"node_{_nextNodeId++}";
			duplicatedIds[original.NodeId] = newNodeId;

			var duplicated = new StatescriptNode
			{
				NodeId = newNodeId,
				Title = original.Title,
				NodeType = original.NodeType,
				RuntimeTypeName = original.RuntimeTypeName,
				PositionOffset = original.PositionOffset + new Vector2(offset, offset),
			};

			foreach (KeyValuePair<string, Variant> kvp in original.CustomData)
			{
				duplicated.CustomData[kvp.Key] = kvp.Value;
			}

			foreach (StatescriptNodeProperty binding in original.PropertyBindings)
			{
				var newBinding = new StatescriptNodeProperty
				{
					Direction = binding.Direction,
					PropertyIndex = binding.PropertyIndex,
					Resolver = binding.Resolver is not null
						? (StatescriptResolverResource)binding.Resolver.Duplicate(true)
						: null,
				};

				duplicated.PropertyBindings.Add(newBinding);
			}

			graph.Nodes.Add(duplicated);

			var graphNode = new StatescriptGraphNode();
			_graphEdit.AddChild(graphNode);
			graphNode.Initialize(duplicated, graph);
			graphNode.Selected = true;
		}

		foreach (StatescriptConnection connection in graph.Connections)
		{
			if (duplicatedIds.TryGetValue(connection.FromNode, out var newFrom)
				&& duplicatedIds.TryGetValue(connection.ToNode, out var newTo))
			{
				_graphEdit.ConnectNode(newFrom, connection.OutputPort, newTo, connection.InputPort);
			}
		}

		SyncConnectionsToCurrentGraph();
	}

	private void OnConnectionRequest(StringName fromNode, long fromPort, StringName toNode, long toPort)
	{
		StatescriptGraph? graph = CurrentGraph;
		if (graph is null || _graphEdit is null)
		{
			return;
		}

		if (_undoRedo is not null)
		{
			_undoRedo.CreateAction("Connect Statescript Nodes", customContext: graph);
			_undoRedo.AddDoMethod(
				this,
				MethodName.DoConnect,
				fromNode.ToString(),
				(int)fromPort,
				toNode.ToString(),
				(int)toPort);
			_undoRedo.AddUndoMethod(
				this,
				MethodName.UndoConnect,
				fromNode.ToString(),
				(int)fromPort,
				toNode.ToString(),
				(int)toPort);
			_undoRedo.CommitAction();
		}
		else
		{
			DoConnect(fromNode.ToString(), (int)fromPort, toNode.ToString(), (int)toPort);
		}
	}

	private void DoConnect(string fromNode, int fromPort, string toNode, int toPort)
	{
		_graphEdit?.ConnectNode(fromNode, fromPort, toNode, toPort);
		SyncConnectionsToCurrentGraph();
	}

	private void UndoConnect(string fromNode, int fromPort, string toNode, int toPort)
	{
		_graphEdit?.DisconnectNode(fromNode, fromPort, toNode, toPort);
		SyncConnectionsToCurrentGraph();
	}

	private void OnDisconnectionRequest(StringName fromNode, long fromPort, StringName toNode, long toPort)
	{
		StatescriptGraph? graph = CurrentGraph;
		if (graph is null || _graphEdit is null)
		{
			return;
		}

		if (_undoRedo is not null)
		{
			_undoRedo.CreateAction("Disconnect Statescript Nodes", customContext: graph);
			_undoRedo.AddDoMethod(
				this,
				MethodName.UndoConnect,
				fromNode.ToString(),
				(int)fromPort,
				toNode.ToString(),
				(int)toPort);
			_undoRedo.AddUndoMethod(
				this,
				MethodName.DoConnect,
				fromNode.ToString(),
				(int)fromPort,
				toNode.ToString(),
				(int)toPort);
			_undoRedo.CommitAction();
		}
		else
		{
			_graphEdit.DisconnectNode(fromNode, (int)fromPort, toNode, (int)toPort);
			SyncConnectionsToCurrentGraph();
		}
	}

	private void OnDeleteNodesRequest(GodotCollections.Array<StringName> deletedNodes)
	{
		StatescriptGraph? graph = CurrentGraph;
		if (graph is null || _graphEdit is null)
		{
			return;
		}

		foreach (StringName nodeName in deletedNodes)
		{
			Node? child = _graphEdit.GetNodeOrNull(nodeName.ToString());

			if (child is not StatescriptGraphNode graphNode)
			{
				continue;
			}

			if (graphNode.NodeResource?.NodeType == StatescriptNodeType.Entry)
			{
				GD.PushWarning("Cannot delete the Entry statescriptNode.");
				continue;
			}

			if (graphNode.NodeResource is null)
			{
				continue;
			}

			var affectedConnections = new List<StatescriptConnection>();
			foreach (GodotCollections.Dictionary connection in _graphEdit.GetConnectionList())
			{
				StringName from = connection["from_node"].AsStringName();
				StringName to = connection["to_node"].AsStringName();

				if (from == nodeName || to == nodeName)
				{
					affectedConnections.Add(new StatescriptConnection
					{
						FromNode = connection["from_node"].AsString(),
						OutputPort = connection["from_port"].AsInt32(),
						ToNode = connection["to_node"].AsString(),
						InputPort = connection["to_port"].AsInt32(),
					});
				}
			}

			if (_undoRedo is not null)
			{
				_undoRedo.CreateAction("Delete Statescript Node", customContext: graph);
				_undoRedo.AddDoMethod(
					this,
					MethodName.DoDeleteNode,
					graph,
					graphNode.NodeResource,
					new GodotCollections.Array<StatescriptConnection>(affectedConnections));
				_undoRedo.AddUndoMethod(
					this,
					MethodName.UndoDeleteNode,
					graph,
					graphNode.NodeResource,
					new GodotCollections.Array<StatescriptConnection>(affectedConnections));
				_undoRedo.CommitAction();
			}
			else
			{
				DoDeleteNode(
					graph,
					graphNode.NodeResource,
					[.. affectedConnections]);
			}
		}
	}

	private void DoDeleteNode(
		StatescriptGraph graph,
		StatescriptNode nodeResource,
		GodotCollections.Array<StatescriptConnection> affectedConnections)
	{
		if (_graphEdit is not null && CurrentGraph == graph)
		{
			foreach (StatescriptConnection connection in affectedConnections)
			{
				_graphEdit.DisconnectNode(
					connection.FromNode,
					connection.OutputPort,
					connection.ToNode,
					connection.InputPort);
			}

			Node? child = _graphEdit.GetNodeOrNull(nodeResource.NodeId);
			child?.QueueFree();
		}

		graph.Nodes.Remove(nodeResource);
		SyncConnectionsToCurrentGraph();
	}

	private void UndoDeleteNode(
		StatescriptGraph graph,
		StatescriptNode nodeResource,
		GodotCollections.Array<StatescriptConnection> affectedConnections)
	{
		graph.Nodes.Add(nodeResource);

		graph.Connections.AddRange(affectedConnections);

		if (CurrentGraph == graph)
		{
			LoadGraphIntoEditor(graph);
		}
	}

	private void OnDialogCanceled()
	{
		ClearPendingConnection();
	}

	private void ClearPendingConnection()
	{
		_pendingConnectionNode = null;
		_pendingConnectionPort = 0;
		_pendingConnectionIsOutput = false;
	}

	private void OnAddNodeButtonPressed()
	{
		if (CurrentGraph is null || _graphEdit is null || _addNodeDialog is null || _addNodeButton is null)
		{
			return;
		}

		ClearPendingConnection();

		var screenPosition = (Vector2I)(_addNodeButton.GetScreenPosition() + new Vector2(0, _addNodeButton.Size.Y));

		Vector2 centerPosition = (_graphEdit.ScrollOffset + (_graphEdit.Size / 2)) / _graphEdit.Zoom;

		_addNodeDialog.ShowAtPosition(centerPosition, screenPosition);
	}

	private string AddNodeAtPosition(
		StatescriptNodeType nodeType,
		string title,
		string runtimeTypeName,
		Vector2 position)
	{
		StatescriptGraph? graph = CurrentGraph;
		if (graph is null || _graphEdit is null)
		{
			return string.Empty;
		}

		var nodeId = $"node_{_nextNodeId++}";

		var nodeResource = new StatescriptNode
		{
			NodeId = nodeId,
			Title = title,
			NodeType = nodeType,
			RuntimeTypeName = runtimeTypeName,
			PositionOffset = position,
		};

		if (_undoRedo is not null)
		{
			_undoRedo.CreateAction("Add Statescript Node", customContext: graph);
			_undoRedo.AddDoMethod(this, MethodName.DoAddNode, graph, nodeResource);
			_undoRedo.AddUndoMethod(this, MethodName.UndoAddNode, graph, nodeResource);
			_undoRedo.CommitAction();
		}
		else
		{
			DoAddNode(graph, nodeResource);
		}

		return nodeId;
	}

	private void DoAddNode(StatescriptGraph graph, StatescriptNode nodeResource)
	{
		graph.Nodes.Add(nodeResource);

		if (CurrentGraph == graph && _graphEdit is not null)
		{
			var graphNode = new StatescriptGraphNode();
			_graphEdit.AddChild(graphNode);
			graphNode.Initialize(nodeResource, graph);
		}
	}

	private void OnSavePressed()
	{
		StatescriptGraph? graph = CurrentGraph;
		if (graph is null || _graphEdit is null)
		{
			return;
		}

		graph.ScrollOffset = _graphEdit.ScrollOffset;
		graph.Zoom = _graphEdit.Zoom;
		SyncVisualNodePositionsToGraph();
		SyncConnectionsToCurrentGraph();

		if (string.IsNullOrEmpty(graph.ResourcePath))
		{
			ShowSaveAsDialog();
			return;
		}

		ResourceSaver.Save(graph);
		GD.Print($"Statescript graph saved: {graph.ResourcePath}");
	}

	/// <summary>
	/// Internal record to track open tab state.
	/// </summary>
	private sealed class GraphTab
	{
		public StatescriptGraph GraphResource { get; }

		public string ResourcePath { get; }

		public bool VariablesPanelOpen { get; set; }

		public GraphTab(StatescriptGraph graphResource)
		{
			GraphResource = graphResource;
			ResourcePath = graphResource.ResourcePath;
		}
	}
}
#endif
