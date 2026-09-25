// Copyright © Gamesmiths Guild.

#if TOOLS
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using FluentAssertions;
using Gamesmiths.Forge.Godot.Core.Statescript.Nodes.Action;
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Godot;

namespace Gamesmiths.Forge.Godot.Tests.Editor;

/// <summary>
/// What the editor's save-all does to graphs it has open: it writes every edit, retrying one whose write failed, and
/// nothing else - not a graph that did not change, and not the identity of what did not change in one that did. Both of
/// those used to rewrite every connection in every open graph.
/// </summary>
internal static partial class SaveTests
{
	private static readonly string _debugNodeType = typeof(DebugNode).FullName!;

	[EditorTest]
	public static void Save_all_skips_a_graph_that_did_not_change(EditorTestContext context)
	{
		const string path = "user://forge_editor_tests_untouched.tres";
		StatescriptGraph graph = OpenSavedGraph(context, path);

		DirAccess.RemoveAbsolute(path);
		context.Dock.SaveAllOpenGraphs();

		FileAccess.FileExists(path).Should().BeFalse("an untouched graph is not written on save-all");

		context.Dock.TestOnlyAddNode("Later", _debugNodeType);
		context.Dock.SaveAllOpenGraphs();

		FileAccess.FileExists(path).Should().BeTrue("a graph that changed is written on save-all");
		FileAccess.GetFileAsString(path).Should().Contain("Later", "the change is what gets written");
		graph.ResourcePath.Should().Be(path);

		context.Dock.CloseCurrentTab();
	}

	[EditorTest]
	public static void Saving_keeps_the_connections_it_loaded(EditorTestContext context)
	{
		const string path = "user://forge_editor_tests_connections.tres";
		OpenSavedGraph(context, path);
		HashSet<string> ids = ConnectionIds(FileAccess.GetFileAsString(path));

		ids.Should().HaveCount(2, "the graph was saved with two connections");

		context.Dock.TestOnlyAddNode("Later", _debugNodeType);
		context.Dock.SaveAllOpenGraphs();
		string after = FileAccess.GetFileAsString(path);

		ConnectionIds(after).Should().BeEquivalentTo(ids, "a save keeps the connection resources it loaded");

		context.Dock.SaveAllOpenGraphs();

		FileAccess.GetFileAsString(path).Should().Be(after, "a save with nothing changed writes the same file");

		context.Dock.CloseCurrentTab();
	}

	[EditorTest]
	public static void Save_all_retries_a_graph_that_failed_to_save(EditorTestContext context)
	{
		const string directory = "user://forge_editor_tests_missing";
		const string path = directory + "/graph.tres";
		DirAccess.RemoveAbsolute(path);
		DirAccess.RemoveAbsolute(directory);

		StatescriptGraph graph = context.OpenNewGraph();
		graph.TakeOverPath(path);
		context.Dock.TestOnlyAddNode("Later", _debugNodeType);

		// Expected to log a save error: the directory does not exist yet.
		context.Dock.SaveAllOpenGraphs();

		FileAccess.FileExists(path).Should().BeFalse("the save failed");

		DirAccess.MakeDirAbsolute(directory);
		context.Dock.SaveAllOpenGraphs();

		FileAccess.FileExists(path).Should().BeTrue("a graph whose save failed is still unsaved");

		context.Dock.CloseCurrentTab();
		DirAccess.RemoveAbsolute(path);
		DirAccess.RemoveAbsolute(directory);
	}

	[EditorTest]
	public static void Save_all_writes_a_moved_node(EditorTestContext context)
	{
		const string path = "user://forge_editor_tests_moved.tres";
		StatescriptGraph graph = OpenSavedGraph(context, path);
		string first = graph.Nodes.First(x => x.Title == "First").NodeId;

		context.Dock.TestOnlyMoveNode(first, new Vector2(320, 160));
		context.Dock.SaveAllOpenGraphs();

		FileAccess.GetFileAsString(path).Should().Contain("Vector2(320, 160)", "a moved node is saved");

		context.HistoryFor(graph).Undo();
		context.Dock.SaveAllOpenGraphs();

		FileAccess.GetFileAsString(path).Should().NotContain("Vector2(320, 160)", "undoing the move is saved too");

		context.Dock.CloseCurrentTab();
	}

	[EditorTest]
	public static void Save_all_writes_arranged_nodes(EditorTestContext context)
	{
		const string path = "user://forge_editor_tests_arranged.tres";
		StatescriptGraph graph = OpenSavedGraph(context, path);
		StatescriptGraph other = context.OpenNewGraph();
		context.Dock.OpenGraph(graph);
		string before = FileAccess.GetFileAsString(path);

		// Nothing is selected, so the whole graph is arranged; switching tabs then used to mark the other graph.
		// Expected to log port index errors: headless, the dock is never shown, so its nodes have no port positions.
		context.Dock.TestOnlyArrangeNodes();
		context.Dock.OpenGraph(other);
		context.Dock.SaveAllOpenGraphs();

		FileAccess.GetFileAsString(path).Should().NotBe(before, "the arranged positions are saved");

		context.Dock.CloseCurrentTab();
		context.Dock.CloseCurrentTab();
	}

	[EditorTest]
	public static void Save_all_writes_a_variable_change_to_its_graph(EditorTestContext context)
	{
		const string path = "user://forge_editor_tests_variable.tres";
		StatescriptGraph graph = OpenSavedGraph(context, path);

		context.Dock.TestOnlySetVariableValue("Speed", 4.25);
		context.Dock.SaveAllOpenGraphs();

		FileAccess.GetFileAsString(path).Should().Contain("InitialValue = 4.25", "a changed variable is saved");

		// Undone from another tab, the change still belongs to the graph that owns the variable.
		context.OpenNewGraph();
		context.HistoryFor(graph).Undo();
		context.Dock.SaveAllOpenGraphs();

		FileAccess.GetFileAsString(path).Should().Contain("InitialValue = 1.5", "undoing it is saved to that graph");

		context.Dock.CloseCurrentTab();
		context.Dock.CloseCurrentTab();
	}

	// Two nodes hung off the entry and a variable, saved, then reopened from disk the way the editor does it.
	private static StatescriptGraph OpenSavedGraph(EditorTestContext context, string path)
	{
		StatescriptGraph authored = context.OpenNewGraph();
		string first = context.Dock.TestOnlyAddNode("First", _debugNodeType);
		string second = context.Dock.TestOnlyAddNode("Second", _debugNodeType);
		context.Dock.TestOnlyConnect("entry", 0, first, 0);
		context.Dock.TestOnlyConnect("entry", 0, second, 0);
		authored.Variables.Add(new StatescriptGraphVariable
		{
			VariableName = "Speed",
			VariableType = StatescriptVariableType.Float,
			InitialValue = 1.5,
		});

		authored.TakeOverPath(path);
		ResourceSaver.Save(authored);
		context.Dock.CloseCurrentTab();

		StatescriptGraph graph =
			ResourceLoader.Load<StatescriptGraph>(path, cacheMode: ResourceLoader.CacheMode.Ignore);

		context.Dock.OpenGraph(graph);
		return graph;
	}

	private static HashSet<string> ConnectionIds(string text)
	{
		var ids = new HashSet<string>();

		foreach (Match match in ConnectionBlock().Matches(text))
		{
			ids.Add(match.Groups["id"].Value);
		}

		return ids;
	}

	[GeneratedRegex("\\[sub_resource type=\"Resource\" id=\"(?<id>[^\"]+)\"\\]\\nscript " +
		"= ExtResource\\(\"[^\"]+\"\\)\\nFromNode = ")]
	private static partial Regex ConnectionBlock();
}
#endif
