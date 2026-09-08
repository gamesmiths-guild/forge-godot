// Copyright © Gamesmiths Guild.

using System;
using System.Linq;
using FluentAssertions;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Godot.Core;
using Gamesmiths.Forge.Godot.Core.Statescript.Nodes.Action;
using Gamesmiths.Forge.Godot.Resources.Statescript;
using GdUnit4;
using ForgeGraph = Gamesmiths.Forge.Statescript.Graph;

namespace Gamesmiths.Forge.Godot.Tests.Statescript;

/// <summary>
/// The conversion from an authored <see cref="StatescriptGraph"/> resource to a runtime <see cref="ForgeGraph"/>.
/// This is the seam between what the editor saves and what actually executes, so everything the editor writes has to
/// survive it.
/// </summary>
[TestSuite]
public class StatescriptGraphBuilderTests
{
	private static readonly string _debugNodeType = typeof(DebugNode).FullName!;

	[TestCase]
	[RequireGodotRuntime]
	public void An_empty_graph_builds_with_only_its_entry_node()
	{
		StatescriptGraph resource = NewGraph();

		ForgeGraph graph = StatescriptGraphBuilder.Build(resource);

		graph.Should().NotBeNull();
		graph.EntryNode.Should().NotBeNull("every graph starts from an entry node.");
	}

	[TestCase]
	[RequireGodotRuntime]
	public void An_action_node_becomes_a_runtime_node()
	{
		StatescriptGraph resource = NewGraph();
		resource.Nodes.Add(ActionNode("action-1"));

		ForgeGraph graph = StatescriptGraphBuilder.Build(resource);

		graph.Nodes.Should().ContainSingle(node => node is DebugNode, "the authored action node has to be built.");
	}

	[TestCase]
	[RequireGodotRuntime]
	public void A_connection_between_two_nodes_is_recreated()
	{
		StatescriptGraph resource = NewGraph();
		StatescriptNode entry = resource.Nodes.First(node => node.NodeType == StatescriptNodeType.Entry);
		StatescriptNode action = ActionNode("action-1");
		resource.Nodes.Add(action);

		resource.Connections.Add(new StatescriptConnection
		{
			FromNode = entry.NodeId,
			OutputPort = 0,
			ToNode = action.NodeId,
			InputPort = 0,
		});

		ForgeGraph graph = StatescriptGraphBuilder.Build(resource);

		graph.Connections.Should().ContainSingle("the authored connection has to be rebuilt.");
	}

	[TestCase]
	[RequireGodotRuntime]
	public void A_node_with_no_runtime_type_is_rejected()
	{
		StatescriptGraph resource = NewGraph();
		StatescriptNode action = ActionNode("action-1");
		action.RuntimeTypeName = string.Empty;
		resource.Nodes.Add(action);

		Action build = () => StatescriptGraphBuilder.Build(resource);

		build.Should().Throw<InvalidOperationException>()
			.WithMessage("*no RuntimeTypeName*", "a node with nothing to instantiate cannot be built silently.");
	}

	[TestCase]
	[RequireGodotRuntime]
	public void A_node_naming_a_type_that_no_longer_exists_is_rejected()
	{
		StatescriptGraph resource = NewGraph();
		StatescriptNode action = ActionNode("action-1");
		action.RuntimeTypeName = "Some.Namespace.ThatWasRenamedAway";
		resource.Nodes.Add(action);

		Action build = () => StatescriptGraphBuilder.Build(resource);

		// This is what a namespace rename does to an already-saved graph, so the failure has to name the type.
		build.Should().Throw<InvalidOperationException>()
			.WithMessage("*Some.Namespace.ThatWasRenamedAway*");
	}

	[TestCase]
	[RequireGodotRuntime]
	public void A_connection_referencing_a_missing_node_is_skipped_rather_than_fatal()
	{
		StatescriptGraph resource = NewGraph();
		StatescriptNode entry = resource.Nodes.First(node => node.NodeType == StatescriptNodeType.Entry);

		resource.Connections.Add(new StatescriptConnection
		{
			FromNode = entry.NodeId,
			OutputPort = 0,
			ToNode = "a-node-that-was-deleted",
			InputPort = 0,
		});

		ForgeGraph graph = StatescriptGraphBuilder.Build(resource);

		// A dangling connection warns and is dropped: one broken edge must not cost the player the whole ability.
		graph.Connections.Should().BeEmpty("the dangling connection has to be dropped, not rebuilt.");
	}

	[TestCase]
	[RequireGodotRuntime]
	public void A_graph_variable_is_registered_on_the_runtime_graph()
	{
		StatescriptGraph resource = NewGraph();
		resource.Variables.Add(new StatescriptGraphVariable
		{
			VariableName = "Ammo",
			VariableType = StatescriptVariableType.Int,
		});

		ForgeGraph graph = StatescriptGraphBuilder.Build(resource);

		// Asserting on the definition itself, not on VariableDefinitions being non-null: Graph creates that container
		// itself, so a null check passes even when RegisterGraphVariables never runs.
		graph.VariableDefinitions.VariableDefinitions.Should().ContainSingle(
			definition => definition.Name == new StringKey("Ammo") && definition.ValueType == typeof(int),
			"the authored variable has to reach the runtime graph with its declared type.");
	}

	private static StatescriptGraph NewGraph()
	{
		var graph = new StatescriptGraph { StatescriptName = "BuilderTests" };
		graph.EnsureEntryNode();
		return graph;
	}

	private static StatescriptNode ActionNode(string nodeId)
	{
		return new StatescriptNode
		{
			NodeId = nodeId,
			Title = "Debug",
			NodeType = StatescriptNodeType.Action,
			RuntimeTypeName = _debugNodeType,
		};
	}
}
