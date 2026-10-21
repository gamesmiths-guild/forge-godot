// Copyright © Gamesmiths Guild.

#if TOOLS
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Gamesmiths.Forge.Godot.Core.Statescript.Nodes.State;
using Gamesmiths.Forge.Godot.Editor.Statescript;
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Gamesmiths.Forge.Statescript;
using Godot;

namespace Gamesmiths.Forge.Godot.Tests.Editor;

/// <summary>
/// The badge in a node's title bar that shows what its work means to a networked game. Simulation is the default and
/// goes unmarked.
/// </summary>
internal static class NetworkKindBadgeTests
{
	/// <summary>
	/// Adds every node type to one graph and checks each badge against the kind the node itself reports.
	/// </summary>
	/// <param name="context">The test context.</param>
	[EditorTest]
	public static void Every_node_shows_its_network_kind_and_simulation_nodes_show_none(EditorTestContext context)
	{
		StatescriptGraph graph = context.OpenNewGraph();
		var mismatched = new List<string>();

		foreach (StatescriptNodeDiscovery.NodeTypeInfo info in StatescriptNodeDiscovery.GetDiscoveredNodeTypes())
		{
			string nodeId = context.Dock.TestOnlyAddNode(info.DisplayName, info.RuntimeTypeName);

			string? expected = info.NetworkKind switch
			{
				NodeNetworkKind.Presentation => "Presentation",
				NodeNetworkKind.Authority => "Authority",
				_ => null,
			};

			if (Badge(context, graph, nodeId) != expected)
			{
				mismatched.Add(info.RuntimeTypeName);
			}
		}

		mismatched.Should().BeEmpty("each node shows the badge for the kind it reports");
	}

	/// <summary>
	/// A node whose kind depends on how it is set up: the badge follows the switch a Node Enabled Override holds.
	/// </summary>
	/// <param name="context">The test context.</param>
	[EditorTest]
	public static void The_badge_follows_the_switch_a_node_enabled_override_holds(EditorTestContext context)
	{
		StatescriptGraph graph = context.OpenNewGraph();
		string nodeId = context.Dock.TestOnlyAddNode("Node Enabled Override", typeof(NodeEnabledOverrideNode).FullName!);
		Badge(context, graph, nodeId).Should().Be("Presentation", "visibility only changes what is seen");

		context.Dock.TestOnlyNodeVisual(graph, nodeId)!.TestOnlySetNodeConfig("aspect", "Monitoring");

		// Replaying the change rebuilds the node the way a user's own edit does.
		UndoRedo history = context.HistoryFor(graph);
		history.Undo();
		history.Redo();

		Badge(context, graph, nodeId).Should().BeNull("monitoring changes gameplay");
	}

	private static string? Badge(EditorTestContext context, StatescriptGraph graph, string nodeId)
	{
		StatescriptGraphNode visual = context.Dock.TestOnlyNodeVisual(graph, nodeId)!;

		return visual.GetTitlebarHBox().GetChildren().OfType<Label>()
			.Select(x => x.Text)
			.SingleOrDefault(x => x is "Presentation" or "Authority");
	}
}
#endif
