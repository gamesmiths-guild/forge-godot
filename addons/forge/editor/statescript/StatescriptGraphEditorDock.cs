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
	private StatescriptAddNodeDialog? _addNodeDialog;

	private EditorUndoRedoManager? _undoRedo;

	private int _nextNodeId;
	private bool _isLoadingGraph;

	// Pending connection state for drag-to-empty behavior.
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

		// Apply negative margins to fill the bottom panel edge-to-edge, matching the MSBuild panel approach.
		StyleBox bottomPanelStylebox = EditorInterface.Singleton.GetBaseControl()
			.GetThemeStylebox("BottomPanel", "EditorStyles");

		AddThemeConstantOverride("margin_top", -(int)bottomPanelStylebox.ContentMarginTop);
		AddThemeConstantOverride("margin_left", -(int)bottomPanelStylebox.ContentMarginLeft);
		AddThemeConstantOverride("margin_right", -(int)bottomPanelStylebox.ContentMarginRight);

		BuildUI();
		UpdateVisibility();

		EditorInterface.Singleton.GetResourceFilesystem().FilesystemChanged += OnFilesystemChanged;

		// Defer restoration so the filesystem is fully scanned first.
		GetTree().CreateTimer(0).Timeout += RestoreOpenTabs;
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

		// Check if this graph is already open in a tab.
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

		// Tab bar background panel styled like a TabContainer header.
		_tabBarBackground = new PanelContainer();
		vBox.AddChild(_tabBarBackground);

		var tabBarHBox = new HBoxContainer
		{
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
		};

		_tabBarBackground.AddChild(tabBarHBox);

		// Tab bar for multiple open graphs.
		_tabBar = new TabBar
		{
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			TabCloseDisplayPolicy = TabBar.CloseButtonDisplayPolicy.ShowActiveOnly,
			DragToRearrangeEnabled = true,
		};

		_tabBar.TabChanged += OnTabChanged;
		_tabBar.TabClosePressed += OnTabClosePressed;
		tabBarHBox.AddChild(_tabBar);

		// Save button in the tab bar area.
		_saveButton = new Button
		{
			Text = "Save",
			Flat = true,
		};

		_saveButton.Pressed += OnSavePressed;
		tabBarHBox.AddChild(_saveButton);

		// Content panel styled like a TabContainer content area.
		_contentPanel = new PanelContainer
		{
			SizeFlagsVertical = SizeFlags.ExpandFill,
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
		};

		vBox.AddChild(_contentPanel);

		var contentVBox = new VBoxContainer
		{
			SizeFlagsVertical = SizeFlags.ExpandFill,
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
		};

		_contentPanel.AddChild(contentVBox);

		// GraphEdit.
		_graphEdit = new GraphEdit
		{
			SizeFlagsVertical = SizeFlags.ExpandFill,
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			RightDisconnects = true,
			Visible = false,
		};

		_graphEdit.ConnectionRequest += OnConnectionRequest;
		_graphEdit.DisconnectionRequest += OnDisconnectionRequest;
		_graphEdit.DeleteNodesRequest += OnDeleteNodesRequest;
		_graphEdit.BeginNodeMove += OnBeginNodeMove;
		_graphEdit.EndNodeMove += OnEndNodeMove;
		_graphEdit.PopupRequest += OnGraphEditPopupRequest;
		_graphEdit.ConnectionToEmpty += OnConnectionToEmpty;
		_graphEdit.ConnectionFromEmpty += OnConnectionFromEmpty;
		contentVBox.AddChild(_graphEdit);

		// Add custom buttons to GraphEdit's built-in toolbar.
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

		// Empty state label.
		_emptyLabel = new Label
		{
			Text = "Select a Statescript resource to begin editing.",
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center,
			SizeFlagsVertical = SizeFlags.ExpandFill,
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
		};

		_emptyLabel.AddThemeColorOverride("font_color", new Color(0.6f, 0.6f, 0.6f));
		contentVBox.AddChild(_emptyLabel);

		// Add Node dialog (created once, reused).
		_addNodeDialog = new StatescriptAddNodeDialog();
		_addNodeDialog.NodeCreationRequested += OnDialogNodeCreationRequested;
		_addNodeDialog.Canceled += OnDialogCanceled;
		AddChild(_addNodeDialog);

		// Apply initial theme.
		UpdateTheme();
	}

	private void UpdateTheme()
	{
		if (_tabBarBackground is null || _contentPanel is null)
		{
			return;
		}

		// Pull the TabContainer style boxes from the editor theme for consistent styling.
		Control baseControl = EditorInterface.Singleton.GetBaseControl();

		StyleBox tabBarStyle = baseControl.GetThemeStylebox("tabbar_background", "TabContainer");
		_tabBarBackground.AddThemeStyleboxOverride("panel", tabBarStyle);

		StyleBox panelStyle = baseControl.GetThemeStylebox("panel", "TabContainer");
		_contentPanel.AddThemeStyleboxOverride("panel", panelStyle);
	}

	private void UpdateVisibility()
	{
		var hasOpenGraph = _openTabs.Count > 0;

		if (_graphEdit is not null)
		{
			_graphEdit.Visible = hasOpenGraph;
		}

		if (_tabBarBackground is not null)
		{
			_tabBarBackground.Visible = hasOpenGraph;
		}

		if (_emptyLabel is not null)
		{
			_emptyLabel.Visible = !hasOpenGraph;
		}
	}

	private void LoadGraphIntoEditor(StatescriptGraph graph)
	{
		if (_graphEdit is null)
		{
			return;
		}

		_isLoadingGraph = true;

		ClearGraphEditor();

		_graphEdit.ScrollOffset = graph.ScrollOffset;
		_graphEdit.Zoom = graph.Zoom;

		// Update the next node ID counter based on existing nodes to avoid ID collisions.
		UpdateNextNodeId(graph);

		// Create visual nodes.
		foreach (StatescriptNode nodeResource in graph.Nodes)
		{
			var graphNode = new StatescriptGraphNode();
			_graphEdit.AddChild(graphNode);
			graphNode.Initialize(nodeResource);
		}

		// Restore connections.
		foreach (StatescriptConnection connection in graph.Connections)
		{
			_graphEdit.ConnectNode(
				connection.FromNode,
				connection.OutputPort,
				connection.ToNode,
				connection.InputPort);
		}

		_isLoadingGraph = false;
	}

	private void ClearGraphEditor()
	{
		if (_graphEdit is null)
		{
			return;
		}

		_graphEdit.ClearConnections();

		// Remove all GraphNode children immediately so they don't interfere with new nodes.
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

		SyncConnectionsToGraph(graph);
	}

	private void SaveOutgoingTabState(int newTabIndex)
	{
		if (_graphEdit is null || _openTabs.Count <= 1)
		{
			return;
		}

		// Identify the outgoing tab by matching node resources in GraphEdit to graph nodes.
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

		// Find which tab owns this node.
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

	private void OnFilesystemChanged()
	{
		EditorFileSystem filesystem = EditorInterface.Singleton.GetResourceFilesystem();

		// Close tabs whose resource files have been deleted.
		for (var i = _openTabs.Count - 1; i >= 0; i--)
		{
			var path = _openTabs[i].ResourcePath;

			if (string.IsNullOrEmpty(path))
			{
				continue;
			}

			// GetFileType returns an empty string if the file is not in the scanned filesystem.
			if (string.IsNullOrEmpty(filesystem.GetFileType(path)))
			{
				CloseTabByIndex(i);
			}
		}

		// Refresh tab titles in case a graph was renamed externally.
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

		// Collect the before/after positions for all moved nodes.
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
		else
		{
			// Without undo/redo, just sync positions directly.
			DoMoveNodes(graph, movedNodes);
		}
	}

	private void DoMoveNodes(
		StatescriptGraph graph,
		GodotCollections.Dictionary<StringName, Vector2> positions)
	{
		// Update the resource positions.
		foreach (StatescriptNode node in graph.Nodes)
		{
			if (positions.TryGetValue(node.NodeId, out Vector2 pos))
			{
				node.PositionOffset = pos;
			}
		}

		// Update the visual nodes if this graph is currently displayed.
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

		// Clear any existing pending connection state.
		ClearPendingConnection();

		// Convert the click position from GraphEdit local coordinates to graph-local coordinates.
		Vector2 graphPosition = (_graphEdit.ScrollOffset + atPosition) / _graphEdit.Zoom;

		// Convert to screen position for dialog placement.
		var screenPosition = (Vector2I)(_graphEdit.GetScreenPosition() + atPosition);

		_addNodeDialog.ShowAtPosition(graphPosition, screenPosition);
	}

	private void OnConnectionToEmpty(StringName fromNode, long fromPort, Vector2 releasePosition)
	{
		if (CurrentGraph is null || _graphEdit is null || _addNodeDialog is null)
		{
			return;
		}

		// Dragging from an output port to empty space.
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

		// Dragging from an input port to empty space.
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

		// Auto-connect if there is a pending connection from a drag-to-empty action.
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

			// Prevent deleting the Entry node.
			if (graphNode.NodeResource?.NodeType == StatescriptNodeType.Entry)
			{
				GD.PushWarning("Cannot delete the Entry node.");
				continue;
			}

			if (graphNode.NodeResource is null)
			{
				continue;
			}

			// Collect connections involving this node.
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
			// Remove connections in the visual editor.
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

		// Re-add the connections to the resource.
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

		// Position the dialog near the Add Node button.
		var screenPosition = (Vector2I)(_addNodeButton.GetScreenPosition() + new Vector2(0, _addNodeButton.Size.Y));

		// Nodes created via the button are placed at the center of the current view.
		Vector2 centerPosition = (_graphEdit.ScrollOffset + (_graphEdit.Size / 2)) / _graphEdit.Zoom;

		_addNodeDialog.ShowAtPosition(centerPosition, screenPosition);
	}

	private void AddNodeAtCenter(StatescriptNodeType nodeType, string title, string runtimeTypeName)
	{
		if (_graphEdit is null)
		{
			return;
		}

		// Place the new node near the center of the current view.
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
			graphNode.Initialize(nodeResource);
		}
	}

	private void OnSavePressed()
	{
		StatescriptGraph? graph = CurrentGraph;
		if (graph is null || _graphEdit is null)
		{
			return;
		}

		// Save the current tab's state directly.
		graph.ScrollOffset = _graphEdit.ScrollOffset;
		graph.Zoom = _graphEdit.Zoom;
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

		var paths = tabsValue.Split(';', System.StringSplitOptions.RemoveEmptyEntries);
		foreach (var path in paths)
		{
			if (!ResourceLoader.Exists(path))
			{
				continue;
			}

			var graph = ResourceLoader.Load<StatescriptGraph>(path);
			if (graph is not null)
			{
				OpenGraph(graph);
			}
		}

		var activeTab = settings.GetProjectMetadata("statescript", "active_tab", 0).AsInt32();
		if (_tabBar is not null && activeTab >= 0 && activeTab < _openTabs.Count)
		{
			_tabBar.CurrentTab = activeTab;
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
