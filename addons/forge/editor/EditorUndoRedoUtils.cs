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
	/// Clears any scope left open, so a fresh plugin session never starts with recording disabled.
	/// </summary>
	/// <remarks>
	/// Depth is balanced by <c>using</c>, but an assembly reload can tear down a callback mid-flight and one leaked
	/// scope would kill undo for the rest of the session, with no symptom beyond "Ctrl+Z does nothing".
	/// </remarks>
	public static void ResetScopes()
	{
		ReplayScope.Reset();
		ViewStateScope.Reset();
	}

	/// <summary>
	/// Suppresses undo/redo recording for as long as the returned value is alive, while still letting the change reach
	/// the resource.
	/// </summary>
	/// <remarks>
	/// For view state that happens to be serialized, such as which foldable is collapsed: it belongs on disk so a
	/// reopened graph looks the way it was left, but not on the undo stack. Neither Blender nor the Godot editor
	/// undoes panel folding.
	/// </remarks>
	/// <returns>A scope that resumes recording when disposed.</returns>
	public static ViewStateScope EnterViewStateChange()
	{
		return ViewStateScope.Enter();
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
		if (IsReplaying || ViewStateScope.Depth > 0)
		{
			return;
		}

		if (undoRedo is null)
		{
			// The change still lands, so warn rather than let it look like an undo bug later.
			GD.PushWarning(
				$"Forge: '{actionName}' was applied without undo support because no undo manager is available. " +
				"This usually means the editor plugin was reloaded without being re-initialized; reopen the project.");
			fallback?.Invoke();
			return;
		}

		undoRedo.CreateAction(actionName, customContext: context);
		configure(undoRedo);
		undoRedo.CommitAction(execute);
	}

	/// <summary>
	/// Scope returned by <see cref="EnterReplay"/>.
	/// </summary>
	internal readonly struct ReplayScope : IDisposable
	{
		/// <summary>
		/// Gets the number of replay scopes currently open. Nesting is expected: a replay callback may invoke another.
		/// </summary>
		internal static int Depth { get; private set; }

		/// <inheritdoc/>
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

		/// <summary>
		/// Drops any scope left open by a callback that did not unwind normally.
		/// </summary>
		internal static void Reset()
		{
			Depth = 0;
		}

		private static void Exit()
		{
			Depth--;
		}
	}

	/// <summary>
	/// Scope returned by <see cref="EnterViewStateChange"/>.
	/// </summary>
	internal readonly struct ViewStateScope : IDisposable
	{
		/// <summary>
		/// Gets the number of view-state scopes currently open.
		/// </summary>
		internal static int Depth { get; private set; }

		/// <inheritdoc/>
		public void Dispose()
		{
			Exit();
		}

		/// <summary>
		/// Opens a view-state scope.
		/// </summary>
		/// <returns>The scope that resumes recording when disposed.</returns>
		internal static ViewStateScope Enter()
		{
			Depth++;
			return default;
		}

		/// <summary>
		/// Drops any scope left open by a callback that did not unwind normally.
		/// </summary>
		internal static void Reset()
		{
			Depth = 0;
		}

		private static void Exit()
		{
			Depth--;
		}
	}
}
#endif
