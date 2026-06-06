// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor;

/// <summary>
/// Shared helpers for recording editor undo/redo actions, centralizing the repeated
/// <c>CreateAction</c> / <c>AddDoMethod</c> / <c>AddUndoMethod</c> / <c>CommitAction</c> boilerplate.
/// </summary>
internal static class EditorUndoRedoUtils
{
	/// <summary>
	/// Records an undo/redo action. When <paramref name="undoRedo"/> is available, opens an action, lets
	/// <paramref name="configure"/> register the do/undo methods, and commits it. When it is <see langword="null"/>,
	/// the optional <paramref name="fallback"/> is invoked so the change still happens without undo support.
	/// </summary>
	/// <param name="undoRedo">The undo/redo manager, or <see langword="null"/> when unavailable.</param>
	/// <param name="actionName">The undo/redo action label.</param>
	/// <param name="context">The custom context (usually the edited resource) used to scope the action.</param>
	/// <param name="configure">Callback that registers the do/undo methods on the action.</param>
	/// <param name="execute">
	/// Whether committing should immediately run the registered do-methods. Pass <see langword="false"/> when the
	/// change has already been applied before recording.
	/// </param>
	/// <param name="fallback">Invoked to apply the change when no undo manager is available.</param>
	public static void Record(
		EditorUndoRedoManager? undoRedo,
		string actionName,
		GodotObject? context,
		Action<EditorUndoRedoManager> configure,
		bool execute = false,
		Action? fallback = null)
	{
		if (undoRedo is null)
		{
			fallback?.Invoke();
			return;
		}

		undoRedo.CreateAction(actionName, customContext: context);
		configure(undoRedo);
		undoRedo.CommitAction(execute);
	}
}
#endif
