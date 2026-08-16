// Copyright © Gamesmiths Guild.

#if TOOLS
using System.Collections.Generic;
using Gamesmiths.Forge.Godot.Resources;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Statescript;

/// <summary>
/// Hosts the undo/redo replay callbacks for shared variable set edits.
/// </summary>
/// <remarks>
/// <para>
/// Undo targets this controller rather than the <see cref="SharedVariableSetEditorProperty"/> that made the change,
/// because the inspector frees and rebuilds property editors constantly while the resource outlives them, and Godot's
/// <c>UndoRedo</c> silently skips operations whose target has been freed. Actions registered on the editor stopped
/// replaying as soon as the inspector refreshed. Mirrors <see cref="Tags.TagSourceEditingController"/>.
/// </para>
/// <para>
/// Every callback mutates the resource, then refreshes whichever editors happen to be showing that set.
/// </para>
/// <para>
/// A <see cref="Node"/> parented to the plugin rather than a bare <see cref="GodotObject"/>, so Godot owns its
/// lifetime.
/// </para>
/// </remarks>
[Tool]
public sealed partial class SharedVariableSetEditingController : Node
{
	private readonly List<SharedVariableSetEditorProperty> _editors = [];

	private EditorUndoRedoManager? _undoRedo;

	/// <summary>
	/// Sets the <see cref="EditorUndoRedoManager"/> used to record edits.
	/// </summary>
	/// <param name="undoRedo">The undo/redo manager from the editor plugin.</param>
	public void SetUndoRedo(EditorUndoRedoManager? undoRedo)
	{
		_undoRedo = undoRedo;
	}

	/// <summary>
	/// Applies a variable's initial value.
	/// </summary>
	/// <param name="set">The set owning the variable.</param>
	/// <param name="def">The variable definition to write to.</param>
	/// <param name="value">The value to apply.</param>
	public void ReplayVariableValue(
		ForgeSharedVariableSet set,
		ForgeSharedVariableDefinition def,
		Variant value)
	{
		using EditorUndoRedoUtils.ReplayScope replay = EditorUndoRedoUtils.EnterReplay();

		def.InitialValue = value;
		NotifyChanged(set);
	}

	/// <summary>
	/// Applies a single array element's value.
	/// </summary>
	/// <param name="set">The set owning the variable.</param>
	/// <param name="def">The variable definition to write to.</param>
	/// <param name="index">The element index.</param>
	/// <param name="value">The value to apply.</param>
	public void ReplayArrayElementValue(
		ForgeSharedVariableSet set,
		ForgeSharedVariableDefinition def,
		int index,
		Variant value)
	{
		using EditorUndoRedoUtils.ReplayScope replay = EditorUndoRedoUtils.EnterReplay();

		if (index >= 0 && index < def.InitialArrayValues.Count)
		{
			def.InitialArrayValues[index] = value;
		}

		NotifyChanged(set);
	}

	/// <summary>
	/// Adds a variable definition to the set.
	/// </summary>
	/// <param name="set">The set to add to.</param>
	/// <param name="def">The definition to add.</param>
	public void ReplayAddVariable(ForgeSharedVariableSet set, ForgeSharedVariableDefinition def)
	{
		using EditorUndoRedoUtils.ReplayScope replay = EditorUndoRedoUtils.EnterReplay();

		set.Variables.Add(def);
		NotifyChanged(set);
	}

	/// <summary>
	/// Removes a variable definition from the set.
	/// </summary>
	/// <param name="set">The set to remove from.</param>
	/// <param name="def">The definition to remove.</param>
	public void ReplayRemoveVariable(ForgeSharedVariableSet set, ForgeSharedVariableDefinition def)
	{
		using EditorUndoRedoUtils.ReplayScope replay = EditorUndoRedoUtils.EnterReplay();

		set.Variables.Remove(def);
		NotifyChanged(set);
	}

	/// <summary>
	/// Re-inserts a variable definition at the position it was removed from.
	/// </summary>
	/// <param name="set">The set to insert into.</param>
	/// <param name="def">The definition to insert.</param>
	/// <param name="index">The index it previously occupied.</param>
	public void ReplayInsertVariable(ForgeSharedVariableSet set, ForgeSharedVariableDefinition def, int index)
	{
		using EditorUndoRedoUtils.ReplayScope replay = EditorUndoRedoUtils.EnterReplay();

		if (index >= 0 && index < set.Variables.Count)
		{
			set.Variables.Insert(index, def);
		}
		else
		{
			set.Variables.Add(def);
		}

		NotifyChanged(set);
	}

	/// <summary>
	/// Appends an element to a variable's initial array values, revealing the row so the change is visible.
	/// </summary>
	/// <param name="set">The set owning the variable.</param>
	/// <param name="def">The variable definition to append to.</param>
	/// <param name="value">The element value.</param>
	public void ReplayAddArrayElement(
		ForgeSharedVariableSet set,
		ForgeSharedVariableDefinition def,
		Variant value)
	{
		using EditorUndoRedoUtils.ReplayScope replay = EditorUndoRedoUtils.EnterReplay();

		def.InitialArrayValues.Add(value);

		foreach (SharedVariableSetEditorProperty editor in LiveEditorsFor(set))
		{
			editor.SetArrayExpandedInternal(def.VariableName, expanded: true);
		}

		NotifyChanged(set);
	}

	/// <summary>
	/// Removes the last element of a variable's initial array values, restoring the row's previous expanded state.
	/// </summary>
	/// <param name="set">The set owning the variable.</param>
	/// <param name="def">The variable definition to trim.</param>
	/// <param name="wasExpanded">Whether the row was expanded before the element was added.</param>
	public void ReplayRemoveLastArrayElement(
		ForgeSharedVariableSet set,
		ForgeSharedVariableDefinition def,
		bool wasExpanded)
	{
		using EditorUndoRedoUtils.ReplayScope replay = EditorUndoRedoUtils.EnterReplay();

		if (def.InitialArrayValues.Count > 0)
		{
			def.InitialArrayValues.RemoveAt(def.InitialArrayValues.Count - 1);
		}

		if (!wasExpanded)
		{
			foreach (SharedVariableSetEditorProperty editor in LiveEditorsFor(set))
			{
				editor.SetArrayExpandedInternal(def.VariableName, expanded: false);
			}
		}

		NotifyChanged(set);
	}

	/// <summary>
	/// Removes an element at the given index from a variable's initial array values.
	/// </summary>
	/// <param name="set">The set owning the variable.</param>
	/// <param name="def">The variable definition to trim.</param>
	/// <param name="index">The element index to remove.</param>
	public void ReplayRemoveArrayElementAt(
		ForgeSharedVariableSet set,
		ForgeSharedVariableDefinition def,
		int index)
	{
		using EditorUndoRedoUtils.ReplayScope replay = EditorUndoRedoUtils.EnterReplay();

		if (index >= 0 && index < def.InitialArrayValues.Count)
		{
			def.InitialArrayValues.RemoveAt(index);
		}

		NotifyChanged(set);
	}

	/// <summary>
	/// Re-inserts an element at the position it was removed from.
	/// </summary>
	/// <param name="set">The set owning the variable.</param>
	/// <param name="def">The variable definition to insert into.</param>
	/// <param name="index">The index it previously occupied.</param>
	/// <param name="value">The element value.</param>
	public void ReplayInsertArrayElement(
		ForgeSharedVariableSet set,
		ForgeSharedVariableDefinition def,
		int index,
		Variant value)
	{
		using EditorUndoRedoUtils.ReplayScope replay = EditorUndoRedoUtils.EnterReplay();

		if (index >= 0 && index < def.InitialArrayValues.Count)
		{
			def.InitialArrayValues.Insert(index, value);
		}
		else
		{
			def.InitialArrayValues.Add(value);
		}

		NotifyChanged(set);
	}

	/// <summary>
	/// Gets the undo/redo manager used to record edits.
	/// </summary>
	/// <returns>The undo/redo manager, or <see langword="null"/> when unavailable.</returns>
	internal EditorUndoRedoManager? GetUndoRedo()
	{
		return _undoRedo;
	}

	/// <summary>
	/// Registers a live editor so replays can refresh it. Editors register on entering the tree and unregister on
	/// leaving it, so the controller never holds a freed one.
	/// </summary>
	/// <param name="editor">The editor to register.</param>
	internal void RegisterEditor(SharedVariableSetEditorProperty editor)
	{
		if (!_editors.Contains(editor))
		{
			_editors.Add(editor);
		}
	}

	/// <summary>
	/// Unregisters a live editor.
	/// </summary>
	/// <param name="editor">The editor to unregister.</param>
	internal void UnregisterEditor(SharedVariableSetEditorProperty editor)
	{
		_editors.Remove(editor);
	}

	/// <summary>
	/// Marks the set as changed and refreshes every live editor showing it.
	/// </summary>
	/// <param name="set">The set that changed.</param>
	private void NotifyChanged(ForgeSharedVariableSet set)
	{
		set.EmitChanged();

		foreach (SharedVariableSetEditorProperty editor in LiveEditorsFor(set))
		{
			editor.NotifyReplayedChangeInternal();
		}
	}

	private List<SharedVariableSetEditorProperty> LiveEditorsFor(ForgeSharedVariableSet set)
	{
		var matches = new List<SharedVariableSetEditorProperty>();

		for (int i = _editors.Count - 1; i >= 0; i--)
		{
			SharedVariableSetEditorProperty editor = _editors[i];

			if (!IsInstanceValid(editor))
			{
				_editors.RemoveAt(i);
				continue;
			}

			if (editor.EditsSetInternal(set))
			{
				matches.Add(editor);
			}
		}

		return matches;
	}
}
#endif
