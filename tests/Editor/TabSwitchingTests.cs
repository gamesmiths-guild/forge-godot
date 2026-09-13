// Copyright © Gamesmiths Guild.

#if TOOLS
using FluentAssertions;
using Gamesmiths.Forge.Godot.Resources.Statescript;

namespace Gamesmiths.Forge.Godot.Tests.Editor;

/// <summary>
/// Tab switching regressions in the Statescript graph editor, driven through the dock in a live headless editor.
/// </summary>
internal static class TabSwitchingTests
{
	/// <summary>
	/// Opening a graph that already had a tab - a double-click in the FileSystem dock - selected its tab but left the
	/// previously shown graph in the editor, because the switch bypassed the tab-changed handler that loads it.
	/// </summary>
	/// <param name="context">The test context.</param>
	[EditorTest]
	public static void Reopening_a_background_tab_shows_its_graph(EditorTestContext context)
	{
		StatescriptGraph first = context.OpenNewGraph();
		StatescriptGraph second = context.OpenNewGraph();

		context.Dock.TestOnlyDisplayedGraph().Should().BeSameAs(second, "opening a graph has to show it");

		context.Dock.OpenGraph(first);

		context.Dock.CurrentGraph.Should().BeSameAs(first, "re-opening a graph has to select its tab");
		context.Dock.TestOnlyDisplayedGraph().Should().BeSameAs(
			first, "re-opening a graph has to show it, not the graph from the tab that was selected before");
	}
}
#endif
