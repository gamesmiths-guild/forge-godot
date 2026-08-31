// Copyright © Gamesmiths Guild.

#if TOOLS
using System.Collections.Generic;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Godot.Core;
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Godot;
using GodotCollections = Godot.Collections;

namespace Gamesmiths.Forge.Godot.Editor.Statescript;

public partial class StatescriptGraphEditorDock
{
	// The defaults a node type wants a fresh instance to start with are written at creation, not while its rows are
	// built: adding a node runs inside a replay scope, where row-time seeding is deliberately suppressed so that an
	// undo clearing a binding is not immediately undone by a rebuild. The node's own editor gets first say, then every
	// required input still unbound is given the resolver its row is about to show.
	private static void SeedDefaultBindings(StatescriptGraph graph, StatescriptNode nodeResource)
	{
		if (StatescriptNodeDiscovery.FindForNode(nodeResource) is not { } typeInfo)
		{
			return;
		}

		if (!CustomNodeEditorRegistry.TryCreate(nodeResource.RuntimeTypeName, out CustomNodeEditor? editor))
		{
			DefaultInputBindings.SeedMissing(graph, nodeResource, typeInfo, editor: null);
			return;
		}

		using (editor)
		{
			editor.SeedDefaultBindings(nodeResource, typeInfo);
			DefaultInputBindings.SeedMissing(graph, nodeResource, typeInfo, editor);
		}
	}

	private static bool WouldCreateLoop(
		StatescriptGraph graphResource,
		string fromNodeId,
		int fromPort,
		string toNodeId,
		int toPort)
	{
		var tempConnection = new StatescriptConnection
		{
			FromNode = fromNodeId,
			OutputPort = fromPort,
			ToNode = toNodeId,
			InputPort = toPort,
		};

		graphResource.Connections.Add(tempConnection);

		try
		{
			StatescriptGraphBuilder.Build(graphResource);
		}
		catch (ValidationException)
		{
			return true;
		}
		finally
		{
			graphResource.Connections.Remove(tempConnection);
		}

		return false;
	}

	private void OnConnectionRequest(StringName fromNode, long fromPort, StringName toNode, long toPort)
	{
		StatescriptGraph? graph = CurrentGraph;
		if (graph is null || _graphEdit is null)
		{
			return;
		}

		string fromNodeId = fromNode.ToString();
		string toNodeId = toNode.ToString();
		int runtimeFromPort = ToRuntimeOutputPort(fromNodeId, (int)fromPort);
		int runtimeToPort = (int)toPort;

		if (WouldCreateLoop(graph, fromNodeId, runtimeFromPort, toNodeId, runtimeToPort))
		{
			ShowLoopWarningDialog();
			return;
		}

		EditorUndoRedoUtils.Record(
			_undoRedo,
			"Connect Statescript Nodes",
			graph,
			undo =>
			{
				undo.AddDoMethod(this, MethodName.DoConnect, fromNodeId, runtimeFromPort, toNodeId, runtimeToPort);
				undo.AddUndoMethod(this, MethodName.UndoConnect, fromNodeId, runtimeFromPort, toNodeId, runtimeToPort);
			},
			execute: true,
			fallback: () => DoConnect(fromNodeId, runtimeFromPort, toNodeId, runtimeToPort));
	}

	private void DoConnect(string fromNode, int fromPort, string toNode, int toPort)
	{
		using EditorUndoRedoUtils.ReplayScope replay = EditorUndoRedoUtils.EnterReplay();

		_graphEdit?.ConnectNode(fromNode, ToVisualOutputPort(fromNode, fromPort), toNode, toPort);
		SyncConnectionsToCurrentGraph();
	}

	private void UndoConnect(string fromNode, int fromPort, string toNode, int toPort)
	{
		using EditorUndoRedoUtils.ReplayScope replay = EditorUndoRedoUtils.EnterReplay();

		_graphEdit?.DisconnectNode(fromNode, ToVisualOutputPort(fromNode, fromPort), toNode, toPort);
		SyncConnectionsToCurrentGraph();
	}

	private void OnDisconnectionRequest(StringName fromNode, long fromPort, StringName toNode, long toPort)
	{
		StatescriptGraph? graph = CurrentGraph;
		if (graph is null || _graphEdit is null)
		{
			return;
		}

		string fromNodeId = fromNode.ToString();
		string toNodeId = toNode.ToString();
		int runtimeFromPort = ToRuntimeOutputPort(fromNodeId, (int)fromPort);
		int runtimeToPort = (int)toPort;

		EditorUndoRedoUtils.Record(
			_undoRedo,
			"Disconnect Statescript Nodes",
			graph,
			undo =>
			{
				undo.AddDoMethod(this, MethodName.UndoConnect, fromNodeId, runtimeFromPort, toNodeId, runtimeToPort);
				undo.AddUndoMethod(this, MethodName.DoConnect, fromNodeId, runtimeFromPort, toNodeId, runtimeToPort);
			},
			execute: true,
			fallback: () =>
			{
				_graphEdit.DisconnectNode(fromNode, (int)fromPort, toNode, (int)toPort);
				SyncConnectionsToCurrentGraph();
			});
	}

	private void OnDeleteNodesRequest(GodotCollections.Array<StringName> deletedNodes)
	{
		StatescriptGraph? graph = CurrentGraph;
		if (graph is null || _graphEdit is null)
		{
			return;
		}

		var nodesToDelete = new GodotCollections.Array<StatescriptNode>();
		var deletedIds = new HashSet<string>();

		foreach (StringName nodeName in deletedNodes)
		{
			if (_graphEdit.GetNodeOrNull(nodeName.ToString()) is not StatescriptGraphNode graphNode
				|| graphNode.NodeResource is null)
			{
				continue;
			}

			if (graphNode.NodeResource.NodeType == StatescriptNodeType.Entry)
			{
				GD.PushWarning("Cannot delete the Entry statescriptNode.");
				continue;
			}

			nodesToDelete.Add(graphNode.NodeResource);
			deletedIds.Add(graphNode.NodeResource.NodeId);
		}

		if (nodesToDelete.Count == 0)
		{
			return;
		}

		// Off the resource rather than the visual connection list: keeps the instances that have to be removed and
		// restored, and captures a connection between two deleted nodes once instead of once per endpoint.
		var affectedConnections = new GodotCollections.Array<StatescriptConnection>();
		foreach (StatescriptConnection connection in graph.Connections)
		{
			if (deletedIds.Contains(connection.FromNode) || deletedIds.Contains(connection.ToNode))
			{
				affectedConnections.Add(connection);
			}
		}

		EditorUndoRedoUtils.Record(
			_undoRedo,
			nodesToDelete.Count > 1 ? "Delete Statescript Nodes" : "Delete Statescript Node",
			graph,
			undo =>
			{
				undo.AddDoMethod(this, MethodName.DoDeleteNodes, graph, nodesToDelete, affectedConnections);
				undo.AddUndoMethod(this, MethodName.UndoDeleteNodes, graph, nodesToDelete, affectedConnections);
			},
			execute: true,
			fallback: () => DoDeleteNodes(graph, nodesToDelete, affectedConnections));
	}

	private void DoDeleteNodes(
		StatescriptGraph graph,
		GodotCollections.Array<StatescriptNode> nodes,
		GodotCollections.Array<StatescriptConnection> affectedConnections)
	{
		using EditorUndoRedoUtils.ReplayScope replay = EditorUndoRedoUtils.EnterReplay();

		bool isCurrent = _graphEdit is not null && CurrentGraph == graph;

		if (isCurrent)
		{
			// Must precede node removal: the runtime-to-visual port mapping is read off the live node.
			foreach (StatescriptConnection connection in affectedConnections)
			{
				_graphEdit!.DisconnectNode(
					connection.FromNode,
					ToVisualOutputPort(connection.FromNode, connection.OutputPort),
					connection.ToNode,
					connection.InputPort);
			}
		}

		// Explicit, because SyncConnectionsToCurrentGraph only syncs the visible graph: deleting in a background tab
		// used to leave these behind for good.
		foreach (StatescriptConnection connection in affectedConnections)
		{
			graph.Connections.Remove(connection);
		}

		foreach (StatescriptNode nodeResource in nodes)
		{
			if (isCurrent)
			{
				Node? child = _graphEdit!.GetNodeOrNull(nodeResource.NodeId);
				if (child is StatescriptGraphNode graphNode)
				{
					ReleaseGraphNodeVisualDeferred(graphNode);
				}
				else
				{
					child?.QueueFree();
				}
			}

			graph.Nodes.Remove(nodeResource);
		}

		if (!isCurrent)
		{
			InvalidateCachedGraphVisuals(graph);
		}

		graph.EmitChanged();
	}

	private void UndoDeleteNodes(
		StatescriptGraph graph,
		GodotCollections.Array<StatescriptNode> nodes,
		GodotCollections.Array<StatescriptConnection> affectedConnections)
	{
		using EditorUndoRedoUtils.ReplayScope replay = EditorUndoRedoUtils.EnterReplay();

		// Every node goes back before any connection does, since a restored connection may span two of them.
		graph.Nodes.AddRange(nodes);
		graph.Connections.AddRange(affectedConnections);
		graph.EmitChanged();

		if (CurrentGraph == graph && _graphEdit is not null)
		{
			GraphTab? tab = FindTab(graph);

			foreach (StatescriptNode nodeResource in nodes)
			{
				StatescriptGraphNode graphNode = AddGraphNodeVisual(nodeResource, graph);
				tab?.CachedGraphNodes.Add(graphNode);
			}

			foreach (StatescriptConnection connection in affectedConnections)
			{
				_graphEdit.ConnectNode(
					connection.FromNode,
					ToVisualOutputPort(connection.FromNode, connection.OutputPort),
					connection.ToNode,
					connection.InputPort);
			}

			ReapplyCurrentNodeHighlights();
		}
		else
		{
			InvalidateCachedGraphVisuals(graph);
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

		EditorUndoRedoUtils.Record(
			_undoRedo,
			"Move Statescript Node(s)",
			graph,
			undo =>
			{
				undo.AddDoMethod(this, MethodName.DoMoveNodes, graph, movedNodes);
				undo.AddUndoMethod(this, MethodName.DoMoveNodes, graph, oldPositions);
			});

		SyncNodePositionsToResource(graph, movedNodes);
	}

	private void DoMoveNodes(
		StatescriptGraph graph,
		GodotCollections.Dictionary<StringName, Vector2> positions)
	{
		using EditorUndoRedoUtils.ReplayScope replay = EditorUndoRedoUtils.EnterReplay();

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

		string nodeId = $"node_{_nextNodeId++}";

		var nodeResource = new StatescriptNode
		{
			NodeId = nodeId,
			Title = title,
			NodeType = nodeType,
			RuntimeTypeName = runtimeTypeName,
			PositionOffset = position,
		};

		SeedDefaultBindings(graph, nodeResource);

		EditorUndoRedoUtils.Record(
			_undoRedo,
			"Add Statescript Node",
			graph,
			undo =>
			{
				undo.AddDoMethod(this, MethodName.DoAddNode, graph, nodeResource);
				undo.AddUndoMethod(this, MethodName.UndoAddNode, graph, nodeResource);
			},
			execute: true,
			fallback: () => DoAddNode(graph, nodeResource));

		return nodeId;
	}

	private void DoAddNode(StatescriptGraph graph, StatescriptNode nodeResource)
	{
		using EditorUndoRedoUtils.ReplayScope replay = EditorUndoRedoUtils.EnterReplay();

		graph.Nodes.Add(nodeResource);
		graph.EmitChanged();

		if (CurrentGraph == graph && _graphEdit is not null)
		{
			GraphTab? tab = FindTab(graph);
			StatescriptGraphNode graphNode = AddGraphNodeVisual(nodeResource, graph);
			tab?.CachedGraphNodes.Add(graphNode);
		}
	}

	private void UndoAddNode(StatescriptGraph graph, StatescriptNode nodeResource)
	{
		using EditorUndoRedoUtils.ReplayScope replay = EditorUndoRedoUtils.EnterReplay();

		graph.Nodes.Remove(nodeResource);
		graph.EmitChanged();

		if (CurrentGraph == graph)
		{
			RemoveNodeVisualWithConnections(nodeResource.NodeId);
		}
		else
		{
			InvalidateCachedGraphVisuals(graph);
		}
	}

	/// <summary>
	/// Removes a single node's visual from the current graph view, disconnecting any visual connections that still
	/// touch it, without rebuilding the rest of the graph.
	/// </summary>
	/// <param name="nodeId">The id of the node whose visual should be removed.</param>
	private void RemoveNodeVisualWithConnections(string nodeId)
	{
		if (_graphEdit is null)
		{
			return;
		}

		foreach (GodotCollections.Dictionary connection in _graphEdit.GetConnectionList())
		{
			string fromNode = connection["from_node"].AsString();
			string toNode = connection["to_node"].AsString();

			if (fromNode == nodeId || toNode == nodeId)
			{
				_graphEdit.DisconnectNode(
					fromNode,
					connection["from_port"].AsInt32(),
					toNode,
					connection["to_port"].AsInt32());
			}
		}

		if (_graphEdit.GetNodeOrNull(nodeId) is StatescriptGraphNode graphNode)
		{
			RemoveGraphNodeVisual(graphNode);
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
		var duplicatedNodes = new GodotCollections.Array<StatescriptNode>();
		const float offset = 40f;

		foreach (StatescriptGraphNode sgn in selectedNodes)
		{
			StatescriptNode original = sgn.NodeResource!;
			string newNodeId = $"node_{_nextNodeId++}";
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

			duplicatedNodes.Add(duplicated);
		}

		var duplicatedConnections = new GodotCollections.Array<StatescriptConnection>();
		foreach (StatescriptConnection connection in graph.Connections)
		{
			if (duplicatedIds.TryGetValue(connection.FromNode, out string? newFrom)
				&& duplicatedIds.TryGetValue(connection.ToNode, out string? newTo))
			{
				duplicatedConnections.Add(new StatescriptConnection
				{
					FromNode = newFrom,
					OutputPort = connection.OutputPort,
					ToNode = newTo,
					InputPort = connection.InputPort,
				});
			}
		}

		EditorUndoRedoUtils.Record(
			_undoRedo,
			"Duplicate Statescript Node(s)",
			graph,
			undo =>
			{
				undo.AddDoMethod(this, MethodName.DoDuplicateNodes, graph, duplicatedNodes, duplicatedConnections);
				undo.AddUndoMethod(this, MethodName.UndoDuplicateNodes, graph, duplicatedNodes, duplicatedConnections);
			},
			execute: true,
			fallback: () => DoDuplicateNodes(graph, duplicatedNodes, duplicatedConnections));
	}

	private void DoDuplicateNodes(
		StatescriptGraph graph,
		GodotCollections.Array<StatescriptNode> nodes,
		GodotCollections.Array<StatescriptConnection> connections)
	{
		using EditorUndoRedoUtils.ReplayScope replay = EditorUndoRedoUtils.EnterReplay();

		graph.Nodes.AddRange(nodes);

		graph.Connections.AddRange(connections);
		graph.EmitChanged();

		if (CurrentGraph == graph && _graphEdit is not null)
		{
			GraphTab? tab = FindTab(graph);

			foreach (StatescriptNode nodeResource in nodes)
			{
				StatescriptGraphNode graphNode = AddGraphNodeVisual(nodeResource, graph);
				tab?.CachedGraphNodes.Add(graphNode);
			}

			foreach (StatescriptConnection connection in connections)
			{
				_graphEdit.ConnectNode(
					connection.FromNode,
					ToVisualOutputPort(connection.FromNode, connection.OutputPort),
					connection.ToNode,
					connection.InputPort);
			}

			SelectGraphNodes(graph, nodes);
			ReapplyCurrentNodeHighlights();
		}
		else
		{
			InvalidateCachedGraphVisuals(graph);
		}
	}

	private void UndoDuplicateNodes(
		StatescriptGraph graph,
		GodotCollections.Array<StatescriptNode> nodes,
		GodotCollections.Array<StatescriptConnection> connections)
	{
		using EditorUndoRedoUtils.ReplayScope replay = EditorUndoRedoUtils.EnterReplay();

		foreach (StatescriptConnection connection in connections)
		{
			graph.Connections.Remove(connection);
		}

		foreach (StatescriptNode node in nodes)
		{
			graph.Nodes.Remove(node);
		}

		graph.EmitChanged();

		if (CurrentGraph == graph)
		{
			foreach (StatescriptNode node in nodes)
			{
				RemoveNodeVisualWithConnections(node.NodeId);
			}
		}
		else
		{
			InvalidateCachedGraphVisuals(graph);
		}
	}

	private void SelectGraphNodes(StatescriptGraph graph, GodotCollections.Array<StatescriptNode> nodes)
	{
		if (_graphEdit is null || CurrentGraph != graph)
		{
			return;
		}

		var ids = new HashSet<string>();
		foreach (StatescriptNode node in nodes)
		{
			ids.Add(node.NodeId);
		}

		foreach (Node child in _graphEdit.GetChildren())
		{
			if (child is StatescriptGraphNode sgn && sgn.NodeResource is not null)
			{
				sgn.Selected = ids.Contains(sgn.NodeResource.NodeId);
			}
		}
	}

	private void ShowLoopWarningDialog()
	{
		var dialog = new AcceptDialog
		{
			Title = "Connection Rejected",
			DialogText = "This connection would create a loop in the graph, which is not allowed.",
			Exclusive = true,
		};

		dialog.Confirmed += dialog.QueueFree;
		dialog.Canceled += dialog.QueueFree;
		AddChild(dialog);
		dialog.PopupCentered();
	}
}
#endif
