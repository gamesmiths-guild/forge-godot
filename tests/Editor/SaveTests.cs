// Copyright © Gamesmiths Guild.

#if TOOLS
using System.Collections.Generic;
using System.Text.RegularExpressions;
using FluentAssertions;
using Gamesmiths.Forge.Godot.Core.Statescript.Nodes.Action;
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Godot;

namespace Gamesmiths.Forge.Godot.Tests.Editor;

/// <summary>
/// What the editor's save-all does to graphs it has open: nothing to one that did not change, and nothing to the
/// identity of what did not change in one that did. Both used to rewrite every connection in every open graph.
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

	// Two nodes hung off the entry, saved, then reopened from disk the way the editor does it.
	private static StatescriptGraph OpenSavedGraph(EditorTestContext context, string path)
	{
		StatescriptGraph authored = context.OpenNewGraph();
		string first = context.Dock.TestOnlyAddNode("First", _debugNodeType);
		string second = context.Dock.TestOnlyAddNode("Second", _debugNodeType);
		context.Dock.TestOnlyConnect("entry", 0, first, 0);
		context.Dock.TestOnlyConnect("entry", 0, second, 0);

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
