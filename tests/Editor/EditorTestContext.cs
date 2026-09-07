// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using Gamesmiths.Forge.Godot.Editor.Statescript;
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Godot;

namespace Gamesmiths.Forge.Godot.Tests.Editor;

/// <summary>
/// What an editor test is handed: the live dock, the editor's undo manager, and a fresh graph per test.
/// </summary>
/// <param name="dock">The live Statescript dock.</param>
/// <param name="undoRedo">The editor's undo/redo manager.</param>
internal sealed class EditorTestContext(StatescriptGraphEditorDock dock, EditorUndoRedoManager undoRedo)
{
	/// <summary>
	/// Gets the live Statescript dock under test.
	/// </summary>
	public StatescriptGraphEditorDock Dock { get; } = dock;

	/// <summary>
	/// Opens a new, empty graph in the dock and returns it.
	/// </summary>
	/// <remarks>
	/// Each test gets its own graph. The dock is shared - there is only one in an editor session - but a fresh graph
	/// means a fresh undo history, so no test can be affected by what ran before it.
	/// </remarks>
	/// <returns>The newly opened graph.</returns>
	public StatescriptGraph OpenNewGraph()
	{
		var graph = new StatescriptGraph { StatescriptName = "EditorTests" };
		graph.EnsureEntryNode();
		Dock.OpenGraph(graph);
		return graph;
	}

	/// <summary>
	/// Resolves the undo history a graph's recorded actions land in.
	/// </summary>
	/// <remarks>
	/// <see cref="EditorUndoRedoManager"/> does not expose undo/redo to scripting - only the editor's own shortcut
	/// handling calls them - so tests drive the underlying history object it hands out. That still replays the
	/// recorded operations, which is what these tests are about, but it bypasses the manager's own cross-history
	/// bookkeeping, so that part stays uncovered.
	/// </remarks>
	/// <param name="graph">The graph whose history is wanted.</param>
	/// <returns>The history object backing that graph's actions.</returns>
	/// <exception cref="InvalidOperationException">Exception thrown when the graph has no undo history, which should
	/// never happen for a graph opened through <see cref="OpenNewGraph"/>.</exception>
	public UndoRedo HistoryFor(StatescriptGraph graph)
	{
		return undoRedo.GetHistoryUndoRedo(undoRedo.GetObjectHistoryId(graph))
			?? throw new InvalidOperationException($"No undo history for graph '{graph.StatescriptName}'.");
	}
}

/// <summary>
/// Marks a static method as an editor test. The runner discovers these by reflection, so a new test needs no
/// registration anywhere.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
internal sealed class EditorTestAttribute : Attribute;
#endif
