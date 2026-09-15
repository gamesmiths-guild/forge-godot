// Copyright © Gamesmiths Guild.

#if TOOLS
using FluentAssertions;
using Gamesmiths.Forge.Godot.Resources.Statescript;

namespace Gamesmiths.Forge.Godot.Tests.Editor;

/// <summary>
/// Closing tabs in the Statescript graph editor, driven through the dock in a live headless editor.
/// </summary>
internal static class TabClosingTests
{
	/// <summary>
	/// Closing the current tab selects the tab that slides into its place and shows that graph. The bar picks a
	/// neighbour of its own when a tab is removed; the dock's choice has to be the one that lands, in both the tab
	/// and the editor.
	/// </summary>
	/// <param name="context">The test context.</param>
	[EditorTest]
	public static void Closing_the_current_tab_shows_the_tab_that_takes_its_place(EditorTestContext context)
	{
		StatescriptGraph first = context.OpenNewGraph();
		StatescriptGraph second = context.OpenNewGraph();
		StatescriptGraph third = context.OpenNewGraph();

		context.Dock.OpenGraph(second);
		context.Dock.CloseCurrentTab();

		context.Dock.CurrentGraph.Should().BeSameAs(third, "the tab to the right slides into the closed one's place");
		context.Dock.TestOnlyDisplayedGraph().Should().BeSameAs(third, "the editor has to show the selected tab");

		context.Dock.OpenGraph(first);
		context.Dock.CurrentGraph.Should().BeSameAs(first, "the tabs to the left are untouched");
	}
}
#endif
