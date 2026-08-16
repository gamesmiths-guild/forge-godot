// Copyright © Gamesmiths Guild.

#if TOOLS
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Godot;
using GodotCollections = Godot.Collections;

namespace Gamesmiths.Forge.Godot.Editor.Statescript;

/// <summary>
/// Undo/redo replay callbacks for per-node edits (config, width, resolver bindings, layout).
/// </summary>
/// <remarks>
/// <para>
/// These live on the dock rather than on the <see cref="StatescriptGraphNode"/> visual, which is freed when the node
/// is deleted, its tab closes, or cached visuals are invalidated. Godot's <c>UndoRedo</c> silently skips operations
/// whose target object is gone, so per-node actions registered on the visual stopped replaying once undo removed the
/// node: redo appeared to die past that point.
/// </para>
/// <para>
/// Each callback resolves the node by <c>(graph, nodeId)</c> at replay time, going through the live visual when the
/// graph is on screen so the UI rebuilds in the same step, and through the resource otherwise.
/// </para>
/// </remarks>
public partial class StatescriptGraphEditorDock
{
	private static StatescriptNode? FindNodeResource(StatescriptGraph graph, string nodeId)
	{
		foreach (StatescriptNode node in graph.Nodes)
		{
			if (node.NodeId == nodeId)
			{
				return node;
			}
		}

		return null;
	}

	private static void WriteCustomDataToResource(StatescriptNode node, GodotCollections.Dictionary customData)
	{
		foreach (KeyValuePair<Variant, Variant> entry in customData)
		{
			string key = entry.Key.AsString();

			if (entry.Value.VariantType == Variant.Type.Nil)
			{
				node.CustomData.Remove(key);
				continue;
			}

			node.CustomData[key] = entry.Value;
		}
	}

	private static void SetBindingOnResource(
		StatescriptNode node,
		StatescriptPropertyDirection direction,
		int propertyIndex,
		Variant resolverVariant)
	{
		if (resolverVariant.AsGodotObject() is not StatescriptResolverResource resolver)
		{
			for (int i = node.PropertyBindings.Count - 1; i >= 0; i--)
			{
				StatescriptNodeProperty binding = node.PropertyBindings[i];

				if (binding.Direction == direction && binding.PropertyIndex == propertyIndex)
				{
					node.PropertyBindings.RemoveAt(i);
				}
			}

			return;
		}

		foreach (StatescriptNodeProperty binding in node.PropertyBindings)
		{
			if (binding.Direction == direction && binding.PropertyIndex == propertyIndex)
			{
				binding.Resolver = resolver;
				return;
			}
		}

		node.PropertyBindings.Add(new StatescriptNodeProperty
		{
			Direction = direction,
			PropertyIndex = propertyIndex,
			Resolver = resolver,
		});
	}

	/// <summary>
	/// Mirrors the live visual's <c>EnsurePropertyVisible</c> on the resource, so a restored value is not hidden in a
	/// collapsed section when the tab is next shown.
	/// </summary>
	/// <param name="node">The node resource to write fold state to.</param>
	/// <param name="direction">The direction of the property (input or output).</param>
	/// <param name="propertyIndex">The index of the property.</param>
	private static void EnsurePropertyVisibleOnResource(
		StatescriptNode node,
		StatescriptPropertyDirection direction,
		int propertyIndex)
	{
		if (direction == StatescriptPropertyDirection.Input)
		{
			node.CustomData[StatescriptGraphNode.FoldInputKey] = false;
			node.CustomData[$"{StatescriptGraphNode.FoldInputPropertyKeyPrefix}{propertyIndex}"] = false;
		}
		else
		{
			node.CustomData[StatescriptGraphNode.FoldOutputKey] = false;
		}
	}

	private void ReplayNodeConfig(StatescriptGraph graph, string nodeId, string key, Variant value)
	{
		using EditorUndoRedoUtils.ReplayScope replay = EditorUndoRedoUtils.EnterReplay();

		if (TryGetLiveNodeVisual(graph, nodeId, out StatescriptGraphNode? visual))
		{
			visual.ApplyNodeConfigInternal(key, value);
			return;
		}

		StatescriptNode? node = FindNodeResource(graph, nodeId);
		if (node is null)
		{
			return;
		}

		if (value.VariantType == Variant.Type.Nil)
		{
			node.CustomData.Remove(key);
		}
		else
		{
			node.CustomData[key] = value;
		}

		MarkReplayedNodeChanged(graph, node);
	}

	private void ReplayNodeWidth(StatescriptGraph graph, string nodeId, float width)
	{
		using EditorUndoRedoUtils.ReplayScope replay = EditorUndoRedoUtils.EnterReplay();

		if (TryGetLiveNodeVisual(graph, nodeId, out StatescriptGraphNode? visual))
		{
			visual.ApplyCustomWidthInternal(width);
			return;
		}

		StatescriptNode? node = FindNodeResource(graph, nodeId);
		if (node is null)
		{
			return;
		}

		node.CustomData[StatescriptGraphNode.CustomWidthKey] = Variant.From(width);
		MarkReplayedNodeChanged(graph, node);
	}

	private void ReplayResolverBinding(
		StatescriptGraph graph,
		string nodeId,
		int directionInt,
		int propertyIndex,
		Variant resolverVariant)
	{
		using EditorUndoRedoUtils.ReplayScope replay = EditorUndoRedoUtils.EnterReplay();

		if (TryGetLiveNodeVisual(graph, nodeId, out StatescriptGraphNode? visual))
		{
			visual.ApplyResolverBindingInternal(directionInt, propertyIndex, resolverVariant);
			return;
		}

		StatescriptNode? node = FindNodeResource(graph, nodeId);
		if (node is null)
		{
			return;
		}

		var direction = (StatescriptPropertyDirection)directionInt;
		SetBindingOnResource(node, direction, propertyIndex, resolverVariant);
		EnsurePropertyVisibleOnResource(node, direction, propertyIndex);
		MarkReplayedNodeChanged(graph, node);
	}

	private void ReplayInputPropertyConfig(
		StatescriptGraph graph,
		string nodeId,
		GodotCollections.Dictionary customData,
		int propertyIndex,
		Variant resolverVariant)
	{
		using EditorUndoRedoUtils.ReplayScope replay = EditorUndoRedoUtils.EnterReplay();

		if (TryGetLiveNodeVisual(graph, nodeId, out StatescriptGraphNode? visual))
		{
			visual.ApplyInputPropertyConfigInternal(customData, propertyIndex, resolverVariant);
			return;
		}

		StatescriptNode? node = FindNodeResource(graph, nodeId);
		if (node is null)
		{
			return;
		}

		WriteCustomDataToResource(node, customData);
		SetBindingOnResource(node, StatescriptPropertyDirection.Input, propertyIndex, resolverVariant);
		EnsurePropertyVisibleOnResource(node, StatescriptPropertyDirection.Input, propertyIndex);
		MarkReplayedNodeChanged(graph, node);
	}

	private void ReplayLayoutConfig(
		StatescriptGraph graph,
		string nodeId,
		GodotCollections.Dictionary customData,
		GodotCollections.Array<StatescriptConnection> connectionsToRemove,
		GodotCollections.Array<StatescriptConnection> connectionsToAdd)
	{
		using EditorUndoRedoUtils.ReplayScope replay = EditorUndoRedoUtils.EnterReplay();

		if (TryGetLiveNodeVisual(graph, nodeId, out StatescriptGraphNode? visual))
		{
			visual.ApplyLayoutConfigInternal(customData, connectionsToRemove, connectionsToAdd);
			return;
		}

		StatescriptNode? node = FindNodeResource(graph, nodeId);
		if (node is null)
		{
			return;
		}

		foreach (StatescriptConnection connection in connectionsToRemove)
		{
			graph.Connections.Remove(connection);
		}

		WriteCustomDataToResource(node, customData);

		foreach (StatescriptConnection connection in connectionsToAdd)
		{
			if (!graph.Connections.Contains(connection))
			{
				graph.Connections.Add(connection);
			}
		}

		MarkReplayedNodeChanged(graph, node);
	}

	/// <summary>
	/// Resolves the live visual for a node when its graph is the one on screen.
	/// </summary>
	/// <param name="graph">The graph the recorded action belongs to.</param>
	/// <param name="nodeId">The node's id, which is also the visual's name in the graph edit.</param>
	/// <param name="visual">The live visual, when there is one.</param>
	/// <returns><see langword="true"/> when the node's visual is currently alive and shown.</returns>
	private bool TryGetLiveNodeVisual(
		StatescriptGraph graph,
		string nodeId,
		[NotNullWhen(true)] out StatescriptGraphNode? visual)
	{
		visual = CurrentGraph == graph
			&& _graphEdit?.GetNodeOrNull(nodeId) is StatescriptGraphNode candidate
			&& candidate.NodeResource is not null
			? candidate
			: null;

		return visual is not null;
	}

	private void MarkReplayedNodeChanged(StatescriptGraph graph, StatescriptNode node)
	{
		node.EmitChanged();
		graph.EmitChanged();
		InvalidateCachedGraphVisuals(graph);
	}
}
#endif
