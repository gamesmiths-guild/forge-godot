// Copyright © Gamesmiths Guild.

#if TOOLS
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Gamesmiths.Forge.Godot.Core.Statescript.Nodes.Action;
using Gamesmiths.Forge.Godot.Core.Statescript.Nodes.State;
using Gamesmiths.Forge.Godot.Editor.Statescript;
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Gamesmiths.Forge.Statescript;
using Godot;

namespace Gamesmiths.Forge.Godot.Tests.Editor;

/// <summary>
/// Client-only and server-only regions, kept in the graph resource and shown as frames around their nodes: made from a
/// selection, joined by dropping a node on a frame, left and deleted, all through the undo history. And the badge of a
/// node that is presentation only in its graph.
/// </summary>
internal static class RegionTests
{
	private const long ClientOnly = 0;

	private const long ServerOnly = 1;

	private const long Leave = 2;

	/// <summary>
	/// Selected nodes become a client-only region, framed, and the history takes it back and gives it again.
	/// </summary>
	/// <param name="context">The test context.</param>
	[EditorTest]
	public static void Selected_nodes_become_a_framed_region_the_history_can_take_back(EditorTestContext context)
	{
		StatescriptGraph graph = context.OpenNewGraph();
		string first = AddDebug(context);
		string second = AddDebug(context);
		Select(context, graph, first, second);

		context.Dock.TestOnlyPressRegionMenu(ClientOnly);

		graph.Regions.Should().ContainSingle().Which.Kind.Should().Be(NodeRegion.ClientOnly);
		graph.Regions[0].NodeIds.Should().BeEquivalentTo(first, second);
		FramedNodes(context).Should().BeEquivalentTo(first, second);

		UndoRedo history = context.HistoryFor(graph);
		history.Undo();

		graph.Regions.Should().BeEmpty();
		context.Dock.TestOnlyRegionFrames().Should().BeEmpty();

		history.Redo();

		graph.Regions.Should().ContainSingle();
		FramedNodes(context).Should().BeEquivalentTo(first, second);
	}

	/// <summary>
	/// A node joining another region leaves the one it was in, and a region left empty goes.
	/// </summary>
	/// <param name="context">The test context.</param>
	[EditorTest]
	public static void A_node_is_in_one_region_and_a_region_left_empty_goes(EditorTestContext context)
	{
		StatescriptGraph graph = context.OpenNewGraph();
		string first = AddDebug(context);
		string second = AddDebug(context);
		Select(context, graph, first, second);
		context.Dock.TestOnlyPressRegionMenu(ClientOnly);

		Select(context, graph, second);
		context.Dock.TestOnlyPressRegionMenu(ServerOnly);

		Describe(graph).Should().Equal($"ClientOnly {first}", $"ServerOnly {second}");

		Select(context, graph, second);
		context.Dock.TestOnlyPressRegionMenu(Leave);

		Describe(graph).Should().Equal($"ClientOnly {first}");
	}

	/// <summary>
	/// A node dropped onto a frame joins that frame's region.
	/// </summary>
	/// <param name="context">The test context.</param>
	[EditorTest]
	public static void A_node_dropped_on_a_frame_joins_its_region(EditorTestContext context)
	{
		StatescriptGraph graph = context.OpenNewGraph();
		string first = AddDebug(context);
		string second = AddDebug(context);
		Select(context, graph, first);
		context.Dock.TestOnlyPressRegionMenu(ServerOnly);
		StringName frame = context.Dock.TestOnlyRegionFrames().Keys.Single();

		context.Dock.TestOnlyDropOnFrame([second], frame);

		Describe(graph).Should().Equal($"ServerOnly {first},{second}");
		FramedNodes(context).Should().BeEquivalentTo(first, second);
	}

	/// <summary>
	/// Deleting a region's frame deletes the region and keeps its nodes.
	/// </summary>
	/// <param name="context">The test context.</param>
	[EditorTest]
	public static void Deleting_a_frame_deletes_its_region_and_keeps_its_nodes(EditorTestContext context)
	{
		StatescriptGraph graph = context.OpenNewGraph();
		string node = AddDebug(context);
		Select(context, graph, node);
		context.Dock.TestOnlyPressRegionMenu(ClientOnly);
		StringName frame = context.Dock.TestOnlyRegionFrames().Keys.Single();

		context.Dock.TestOnlyDeleteNodes([frame]);

		graph.Regions.Should().BeEmpty();
		graph.Nodes.Should().Contain(x => x.NodeId == node);
		context.Dock.TestOnlyRegionFrames().Should().BeEmpty();
	}

	/// <summary>
	/// Deleting a region's nodes takes them out of it, so a later node given a deleted id doesn't join it, and a region
	/// left empty goes; one undo brings the nodes back in it.
	/// </summary>
	/// <param name="context">The test context.</param>
	[EditorTest]
	public static void Deleting_a_region_s_nodes_takes_them_out_of_it_and_one_undo_brings_them_back(
		EditorTestContext context)
	{
		StatescriptGraph graph = context.OpenNewGraph();
		string first = AddDebug(context);
		string second = AddDebug(context);
		Select(context, graph, first, second);
		context.Dock.TestOnlyPressRegionMenu(ClientOnly);

		context.Dock.TestOnlyDeleteNodes([first]);

		Describe(graph).Should().Equal($"ClientOnly {second}");

		context.Dock.TestOnlyDeleteNodes([second]);

		graph.Regions.Should().BeEmpty();

		UndoRedo history = context.HistoryFor(graph);
		history.Undo();
		context.Dock.TestOnlyRefreshNetworkView();

		Describe(graph).Should().Equal($"ClientOnly {second}");
		FramedNodes(context).Should().Equal(second);
	}

	/// <summary>
	/// A frame deleted together with its nodes comes back with them in one undo.
	/// </summary>
	/// <param name="context">The test context.</param>
	[EditorTest]
	public static void A_frame_deleted_with_its_nodes_comes_back_with_them_in_one_undo(EditorTestContext context)
	{
		StatescriptGraph graph = context.OpenNewGraph();
		string node = AddDebug(context);
		Select(context, graph, node);
		context.Dock.TestOnlyPressRegionMenu(ClientOnly);
		StringName frame = context.Dock.TestOnlyRegionFrames().Keys.Single();

		context.Dock.TestOnlyDeleteNodes([frame, node]);

		graph.Regions.Should().BeEmpty();
		graph.Nodes.Should().NotContain(x => x.NodeId == node);

		context.HistoryFor(graph).Undo();
		context.Dock.TestOnlyRefreshNetworkView();

		graph.Nodes.Should().Contain(x => x.NodeId == node);
		Describe(graph).Should().Equal($"ClientOnly {node}");
		FramedNodes(context).Should().Equal(node);
	}

	/// <summary>
	/// A graph whose node has a setting its type can't take still opens, with the kinds its node types declare.
	/// </summary>
	/// <param name="context">The test context.</param>
	[EditorTest]
	public static void A_graph_with_a_setting_its_node_can_t_take_still_opens(EditorTestContext context)
	{
		var graph = new StatescriptGraph { StatescriptName = "EditorTests" };
		graph.EnsureEntryNode();
		graph.Nodes.Add(new StatescriptNode
		{
			NodeId = "enable",
			Title = nameof(SetNodeEnabledNode),
			NodeType = StatescriptNodeType.Action,
			RuntimeTypeName = typeof(SetNodeEnabledNode).FullName!,
			CustomData = { ["aspect"] = "NoSuchAspect" },
		});

		System.Action open = () => context.Dock.OpenGraph(graph);

		open.Should().NotThrow();
	}

	/// <summary>
	/// A simulation node acting on the instance a scene node made shows presentation, and says why.
	/// </summary>
	/// <param name="context">The test context.</param>
	[EditorTest]
	public static void A_node_acting_on_what_a_scene_node_made_shows_presentation(EditorTestContext context)
	{
		var graph = new StatescriptGraph { StatescriptName = "EditorTests" };
		graph.EnsureEntryNode();
		graph.Variables.Add(new StatescriptGraphVariable { VariableName = "made", ObjectTypeId = "Entity" });
		graph.Nodes.Add(Node("scene", typeof(Scene3DNode), StatescriptPropertyDirection.Output, 1));
		graph.Nodes.Add(Node("move", typeof(MoveTo3DNode), StatescriptPropertyDirection.Input, 0));

		context.Dock.OpenGraph(graph);
		context.Dock.TestOnlyRefreshNetworkView();

		Label badge = Badge(context, graph, "move");
		badge.Text.Should().Be("Presentation");
		badge.TooltipText.Should().Contain("in this graph");
	}

	private static string AddDebug(EditorTestContext context)
	{
		return context.Dock.TestOnlyAddNode("Debug", typeof(DebugNode).FullName!);
	}

	private static void Select(EditorTestContext context, StatescriptGraph graph, params string[] nodeIds)
	{
		foreach (StatescriptNode node in graph.Nodes)
		{
			StatescriptGraphNode? visual = context.Dock.TestOnlyNodeVisual(graph, node.NodeId);

			if (visual is not null)
			{
				visual.Selected = nodeIds.Contains(node.NodeId);
			}
		}
	}

	private static IEnumerable<string> FramedNodes(EditorTestContext context)
	{
		return context.Dock.TestOnlyRegionFrames().Values.SelectMany(x => x).Select(x => x.ToString());
	}

	private static IEnumerable<string> Describe(StatescriptGraph graph)
	{
		return graph.Regions.Select(x => $"{x.Kind} {string.Join(",", x.NodeIds)}");
	}

	private static StatescriptNode Node(
		string nodeId,
		System.Type type,
		StatescriptPropertyDirection direction,
		int propertyIndex)
	{
		return new StatescriptNode
		{
			NodeId = nodeId,
			Title = type.Name,
			NodeType = StatescriptNodeType.State,
			RuntimeTypeName = type.FullName!,
			PropertyBindings =
			[
				new StatescriptNodeProperty
				{
					Direction = direction,
					PropertyIndex = propertyIndex,
					Resolver = new VariableResolverResource { VariableName = "made", ObjectTypeId = "Entity" },
				},
			],
		};
	}

	private static Label Badge(EditorTestContext context, StatescriptGraph graph, string nodeId)
	{
		StatescriptGraphNode visual = context.Dock.TestOnlyNodeVisual(graph, nodeId)!;

		return visual.GetTitlebarHBox().GetChildren().OfType<Label>()
			.Single(x => x.Text is "Presentation" or "Authority");
	}
}
#endif
