// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FluentAssertions;
using Gamesmiths.Forge.Godot.Core.Statescript.Nodes.Action;
using Gamesmiths.Forge.Godot.Editor.Statescript;
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Godot;
using GodotCollections = Godot.Collections;

namespace Gamesmiths.Forge.Godot.Tests.Editor;

/// <summary>
/// Undo/redo regressions in the Statescript graph editor, driven through the real
/// <see cref="EditorUndoRedoManager"/> in a live headless editor.
/// </summary>
internal static class UndoRedoTests
{
	private static readonly string _debugNodeType = typeof(DebugNode).FullName!;

	/// <summary>
	/// The regression that started this work: per-node edits were registered on the node's visual, which undo frees
	/// when it removes the node. Godot skips operations whose target is gone, so redo replayed the graph-level actions
	/// and silently dropped every per-node one. Undoing past the node's creation is what makes this bite.
	/// </summary>
	/// <param name="context">The test context.</param>
	[EditorTest]
	public static void Redo_restores_every_edit_across_node_recreation(EditorTestContext context)
	{
		StatescriptGraph graph = context.OpenNewGraph();
		string initial = Snapshot(graph);

		string nodeId = context.Dock.TestOnlyAddNode("Debug", _debugNodeType);
		context.Dock.TestOnlySetNodeConfig(graph, nodeId, "valueType", "Float");
		context.Dock.TestOnlySetNodeWidth(graph, nodeId, 420f);

		Snapshot(graph).Should().NotBe(initial, "the three edits have to change the graph");

		UndoRedo history = context.HistoryFor(graph);

		for (int i = 0; i < 3; i++)
		{
			history.Undo();
		}

		Snapshot(graph).Should().Be(initial, "undoing all three edits has to restore the original graph");

		for (int i = 0; i < 3; i++)
		{
			history.Redo();
		}

		// Asserted property by property rather than against a whole-graph snapshot: this is the assertion that fails
		// when the bug regresses, and "these two long strings differ" would not say which edit went missing.
		StatescriptNode? node = FindNode(graph, nodeId);

		node.Should().NotBeNull("redo has to re-create the node");
		node!.CustomData["valueType"].AsString().Should().Be("Float", "redo has to restore the node's config");
		node.CustomData[StatescriptGraphNode.CustomWidthKey].AsSingle().Should().Be(
			420f, "redo has to restore the node's width");
	}

	/// <summary>
	/// Deleting a multi-node selection used to record one action per node, so it took as many undos to get back.
	/// </summary>
	/// <param name="context">The test context.</param>
	[EditorTest]
	public static void Deleting_a_selection_is_a_single_undo_step(EditorTestContext context)
	{
		StatescriptGraph graph = context.OpenNewGraph();
		var ids = new GodotCollections.Array<StringName>();

		for (int i = 0; i < 3; i++)
		{
			ids.Add(context.Dock.TestOnlyAddNode($"Debug{i}", _debugNodeType));
		}

		string beforeDelete = Snapshot(graph);
		int nodesBefore = graph.Nodes.Count;

		context.Dock.TestOnlyDeleteNodes(ids);

		graph.Nodes.Should().HaveCount(nodesBefore - 3, "deleting the selection has to remove all three nodes");

		context.HistoryFor(graph).Undo();

		Snapshot(graph).Should().Be(beforeDelete, "one undo has to restore the whole deleted selection");
	}

	/// <summary>
	/// Recording during a replay is destructive: Godot's <c>commit_action</c> clears the redo stack, so one stray
	/// record while undoing strands everything above it. Undo has to leave redo available.
	/// </summary>
	/// <param name="context">The test context.</param>
	[EditorTest]
	public static void Replaying_an_undo_does_not_clear_the_redo_stack(EditorTestContext context)
	{
		StatescriptGraph graph = context.OpenNewGraph();

		string nodeId = context.Dock.TestOnlyAddNode("Debug", _debugNodeType);
		context.Dock.TestOnlySetNodeConfig(graph, nodeId, "valueType", "Float");

		UndoRedo history = context.HistoryFor(graph);

		history.Undo();

		history.HasRedo().Should().BeTrue("redo has to still be available after one undo");

		history.Undo();

		history.HasRedo().Should().BeTrue("redo has to survive undoing past the node's creation");
	}

	private static StatescriptNode? FindNode(StatescriptGraph graph, string nodeId)
	{
		return graph.Nodes.FirstOrDefault(node => node.NodeId == nodeId);
	}

	/// <summary>
	/// Produces a stable structural dump of everything the editor is meant to persist, so two whole states can be
	/// compared exactly. Deliberately covers CustomData and bindings, which is where the silent-drop bugs lived.
	/// </summary>
	/// <param name="graph">The graph to snapshot.</param>
	/// <returns>A deterministic string describing the graph's persisted state.</returns>
	private static string Snapshot(StatescriptGraph graph)
	{
		var builder = new StringBuilder();

		foreach (StatescriptNode node in graph.Nodes)
		{
			builder.Append("node(").Append(node.NodeId).Append(',').Append(node.Title)
				.Append(',').Append(node.RuntimeTypeName)
				.Append(",pos=").Append(node.PositionOffset);

			List<string> keys = [.. node.CustomData.Keys];
			keys.Sort(StringComparer.Ordinal);

			foreach (string key in keys)
			{
				builder.Append(",cd[").Append(key).Append(']').Append('=')
					.Append(node.CustomData[key].ToString());
			}

			foreach (StatescriptNodeProperty binding in node.PropertyBindings)
			{
				builder.Append(",bind(").Append(binding.Direction).Append(',').Append(binding.PropertyIndex)
					.Append(',').Append(binding.Resolver?.ResolverTypeId ?? "none").Append(')');
			}

			builder.AppendLine(")");
		}

		List<string> connections = [.. graph.Connections.Select(connection =>
			$"conn({connection.FromNode}:{connection.OutputPort}->{connection.ToNode}:{connection.InputPort})")];

		connections.Sort(StringComparer.Ordinal);

		foreach (string connection in connections)
		{
			builder.AppendLine(connection);
		}

		return builder.ToString();
	}
}
#endif
