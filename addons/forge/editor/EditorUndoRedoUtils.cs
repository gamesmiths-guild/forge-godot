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
	/// Gets a value indicating whether an undo or redo action is currently being replayed.
	/// </summary>
	/// <remarks>
	/// Godot's <c>commit_action</c> clears the redo stack, so recording from inside a replay strands every action
	/// after the one being replayed. Replay callbacks rebuild editor UI, and rebuilding re-enters code that would
	/// otherwise record, so any such code has to check this and skip.
	/// </remarks>
	public static bool IsReplaying => ReplayScope.Depth > 0;

	/// <summary>
	/// Marks the calling scope as an undo/redo replay for as long as the returned value is alive. Every method
	/// registered through <c>AddDoMethod</c> / <c>AddUndoMethod</c> should open one.
	/// </summary>
	/// <returns>A scope that ends the replay when disposed.</returns>
	public static ReplayScope EnterReplay()
	{
		return ReplayScope.Enter();
	}

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
		if (IsReplaying)
		{
			// Recording here would clear the redo stack and strand every action after the one being replayed. The
			// replay itself already puts the data in the right state, so there is nothing to record.
			return;
		}

		if (undoRedo is null)
		{
			fallback?.Invoke();
			return;
		}

		undoRedo.CreateAction(actionName, customContext: context);
		configure(undoRedo);
		undoRedo.CommitAction(execute);
	}

	/// <summary>
	/// Scope returned by <see cref="EnterReplay"/>. Ends the replay when disposed.
	/// </summary>
	internal readonly struct ReplayScope : IDisposable
	{
		/// <summary>
		/// Gets the number of replay scopes currently open. Nested scopes are expected: a replay callback may call
		/// another one directly.
		/// </summary>
		internal static int Depth { get; private set; }

		/// <summary>
		/// Ends the replay scope.
		/// </summary>
		public void Dispose()
		{
			Exit();
		}

		/// <summary>
		/// Opens a replay scope.
		/// </summary>
		/// <returns>The scope that ends the replay when disposed.</returns>
		internal static ReplayScope Enter()
		{
			Depth++;
			return default;
		}

		private static void Exit()
		{
			_depth--;
		}
	}
}
#endif
