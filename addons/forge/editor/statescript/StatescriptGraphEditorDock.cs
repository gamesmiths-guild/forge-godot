// Copyright © Gamesmiths Guild.

#if TOOLS
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
	private Button? _saveButton;
	private Button? _variablesToggleButton;
	private StatescriptAddNodeDialog? _addNodeDialog;
	private StatescriptVariablePanel? _variablePanel;
	private HSplitContainer? _splitContainer;

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

		RestoreOpenTabs();
	}

	public override void _ExitTree()
	{
		base._ExitTree();

		SaveOpenTabs();
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
		SaveOpenTabs();
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
		}
		else
		{
			ClearGraphEditor();
		}

		UpdateVisibility();
		SaveOpenTabs();
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

		_saveButton = new Button
		{
			Text = "Save",
			Flat = true,
		};

		_saveButton.Pressed += OnSavePressed;
		tabBarHBox.AddChild(_saveButton);

		_variablesToggleButton = new Button
		{
			Text = "Variables",
			ToggleMode = true,
			Flat = true,
		};

		_variablesToggleButton.Toggled += OnVariablesToggled;
		tabBarHBox.AddChild(_variablesToggleButton);

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
			Visible = false,
		};

		_variablePanel.VariablesChanged += OnGraphVariablesChanged;
		_splitContainer.AddChild(_variablePanel);

		HBoxContainer menuHBox = _graphEdit.GetMenuHBox();

		var separator = new VSeparator();
		menuHBox.AddChild(separator);
		menuHBox.MoveChild(separator, 0);

		_addNodeButton = new Button
		{
			Text = "Add Node...",
			Flat = true,
		};

		_addNodeButton.Pressed += OnAddNodeButtonPressed;

		menuHBox.AddChild(_addNodeButton);
		menuHBox.MoveChild(_addNodeButton, 0);

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
			if (nodeId.StartsWith("node_", System.StringComparison.InvariantCultureIgnoreCase)
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
			if (child is StatescriptGraphNode sgn)
			{
				firstNode = sgn;
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
					return;
				}
			}
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
		if (_variablePanel is null)
		{
			return;
		}

		_variablePanel.Visible = pressed;

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

			if (_variablePanel?.Visible == true)
			{
				_variablePanel.SetGraph(_openTabs[(int)tab].GraphResource);
			}
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
			if (child is StatescriptGraphNode { Selected: true } sgn
				&& sgn.NodeResource is not null
				&& sgn.NodeResource.NodeType != StatescriptNodeType.Entry)
			{
				selectedNodes.Add(sgn);
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
				GD.PushWarning("Cannot delete the Entry node.");
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

	private void AddNodeAtCenter(StatescriptNodeType nodeType, string title, string runtimeTypeName)
	{
		if (_graphEdit is null)
		{
			return;
		}

		Vector2 spawnPosition = (_graphEdit.ScrollOffset + (_graphEdit.Size / 2)) / _graphEdit.Zoom;
		AddNodeAtPosition(nodeType, title, runtimeTypeName, spawnPosition);
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
			GD.PushWarning("Statescript resource has no path. Save it as a resource file first.");
			return;
		}

		ResourceSaver.Save(graph);
		GD.Print($"Statescript graph saved: {graph.ResourcePath}");
	}

	private void SaveOpenTabs()
	{
		if (_isLoadingGraph)
		{
			return;
		}

		EditorSettings settings = EditorInterface.Singleton.GetEditorSettings();

		var paths = new string[_openTabs.Count];
		for (var i = 0; i < _openTabs.Count; i++)
		{
			paths[i] = _openTabs[i].ResourcePath;
		}

		settings.SetProjectMetadata("statescript", "open_tabs", string.Join(";", paths));
		settings.SetProjectMetadata(
			"statescript",
			"active_tab",
			_tabBar is not null && _openTabs.Count > 0 ? _tabBar.CurrentTab : 0);
	}

	private void RestoreOpenTabs()
	{
		EditorSettings settings = EditorInterface.Singleton.GetEditorSettings();

		var tabsValue = settings.GetProjectMetadata("statescript", "open_tabs", string.Empty).AsString();
		if (string.IsNullOrEmpty(tabsValue))
		{
			return;
		}

		_isLoadingGraph = true;

		foreach (var path in tabsValue.Split(';', System.StringSplitOptions.RemoveEmptyEntries))
		{
			if (!ResourceLoader.Exists(path))
			{
				continue;
			}

			StatescriptGraph? graph = ResourceLoader.Load<StatescriptGraph>(path);
			if (graph is not null)
			{
				OpenGraph(graph);
			}
		}

		_isLoadingGraph = false;

		var activeTab = settings.GetProjectMetadata("statescript", "active_tab", 0).AsInt32();
		if (_tabBar is not null && activeTab >= 0 && activeTab < _openTabs.Count)
		{
			_tabBar.CurrentTab = activeTab;
			LoadGraphIntoEditor(_openTabs[activeTab].GraphResource);

			if (_variablePanel?.Visible == true)
			{
				_variablePanel.SetGraph(_openTabs[activeTab].GraphResource);
			}
		}
	}

	/// <summary>
	/// Internal record to track open tab state.
	/// </summary>
	private sealed class GraphTab
	{
		public StatescriptGraph GraphResource { get; }

		public string ResourcePath { get; }

		public GraphTab(StatescriptGraph graphResource)
		{
			GraphResource = graphResource;
			ResourcePath = graphResource.ResourcePath;
		}
	}
}
#endif
