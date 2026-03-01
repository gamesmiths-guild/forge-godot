// Copyright © Gamesmiths Guild.

#if TOOLS
using System.Collections.Generic;
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Godot;
using GodotCollections = Godot.Collections;

namespace Gamesmiths.Forge.Godot.Editor.Statescript;

public partial class StatescriptGraphEditorDock
{
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
			graphNode.SetUndoRedo(_undoRedo);
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
}
#endif
