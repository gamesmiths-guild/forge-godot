// Copyright © Gamesmiths Guild.

#if TOOLS
using FluentAssertions;
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Godot;

namespace Gamesmiths.Forge.Godot.Tests.Editor;

/// <summary>
/// Restoring the tabs of an earlier session in the Statescript graph editor, driven through the dock in a live headless
/// editor.
/// </summary>
internal static class TabRestoringTests
{
	/// <summary>
	/// A saved tab whose file is gone is skipped without disturbing the others. The saved states line up with the saved
	/// paths, but were read by restored tab, so a missing file shifted every later tab onto its predecessor's state.
	/// </summary>
	/// <param name="context">The test context.</param>
	[EditorTest]
	public static void Restoring_past_a_missing_file_keeps_each_tab_state(EditorTestContext context)
	{
		const string firstPath = "user://forge_editor_tests_restore_first.tres";
		const string secondPath = "user://forge_editor_tests_restore_second.tres";
		ResourceSaver.Save(new StatescriptGraph { StatescriptName = "First" }, firstPath);
		ResourceSaver.Save(new StatescriptGraph { StatescriptName = "Second" }, secondPath);

		// As at startup, the dock restores into no tabs at all.
		while (context.Dock.CurrentGraph is not null)
		{
			context.Dock.CloseCurrentTab();
		}

		context.Dock.RestoreFromPaths(
			["user://forge_editor_tests_restore_missing.tres", firstPath, secondPath],
			activeIndex: 2,
			variablesStates: [true, false, true]);

		context.Dock.CurrentGraph!.ResourcePath.Should().Be(secondPath, "the second graph was the active tab");
		context.Dock.GetVariablesPanelStates()[0].Should().BeFalse("the first graph had its variables panel closed");

		context.Dock.CloseCurrentTab();
		context.Dock.CloseCurrentTab();
	}
}
#endif
