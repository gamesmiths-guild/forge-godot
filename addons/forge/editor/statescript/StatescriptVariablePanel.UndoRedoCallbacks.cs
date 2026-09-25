// Copyright © Gamesmiths Guild.

#if TOOLS
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Statescript;

internal sealed partial class StatescriptVariablePanel
{
	private static void SetArrayElementValue(
		StatescriptGraph graph,
		StatescriptGraphVariable variable,
		int index,
		Variant newValue)
	{
		variable.InitialArrayValues[index] = newValue;
		variable.EmitChanged();
		graph.EmitChanged();
	}

	private void SetArrayElementValueWithUndo(StatescriptGraphVariable variable, int index, Variant newValue)
	{
		if (_graph is null || index < 0 || index >= variable.InitialArrayValues.Count)
		{
			return;
		}

		Variant oldValue = variable.InitialArrayValues[index];

		SetArrayElementValue(_graph, variable, index, newValue);

		EditorUndoRedoUtils.Record(
			_undoRedo,
			$"Change Variable '{variable.VariableName}' [{index}]",
			_graph,
			undo =>
			{
				undo.AddDoMethod(this, MethodName.ApplyArrayElementValue, _graph, variable, index, newValue);
				undo.AddUndoMethod(this, MethodName.ApplyArrayElementValue, _graph, variable, index, oldValue);
			});
	}

	// A replay is handed the variable's graph rather than using the one on show: an undo can run after the panel has
	// moved to another tab, and it is the variable's graph that has to be marked changed for the save to write it.
	private void ApplyArrayElementValue(
		StatescriptGraph graph,
		StatescriptGraphVariable variable,
		int index,
		Variant value)
	{
		using EditorUndoRedoUtils.ReplayScope replay = EditorUndoRedoUtils.EnterReplay();

		if (index >= 0 && index < variable.InitialArrayValues.Count)
		{
			variable.InitialArrayValues[index] = value;
			variable.EmitChanged();
			graph.EmitChanged();
		}

		// Reveal the changed element so an undo/redo isn't hidden inside a collapsed array.
		EnsureArrayExpanded(variable.VariableName);

		RebuildList();
		VariableUndoRedoPerformed?.Invoke();
	}

	private void SetVariableValue(StatescriptGraphVariable variable, Variant newValue)
	{
		if (_graph is null)
		{
			return;
		}

		Variant oldValue = variable.InitialValue;

		variable.InitialValue = newValue;
		variable.EmitChanged();
		_graph.EmitChanged();

		EditorUndoRedoUtils.Record(
			_undoRedo,
			$"Change Variable '{variable.VariableName}'",
			_graph,
			undo =>
			{
				undo.AddDoMethod(this, MethodName.ApplyVariableValue, _graph, variable, newValue);
				undo.AddUndoMethod(this, MethodName.ApplyVariableValue, _graph, variable, oldValue);
			});
	}

	private void ApplyVariableValue(StatescriptGraph graph, StatescriptGraphVariable variable, Variant value)
	{
		using EditorUndoRedoUtils.ReplayScope replay = EditorUndoRedoUtils.EnterReplay();

		variable.InitialValue = value;
		variable.EmitChanged();
		graph.EmitChanged();
		RebuildList();
		VariableUndoRedoPerformed?.Invoke();
	}

	private void DoAddVariable(StatescriptGraph graph, StatescriptGraphVariable variable)
	{
		using EditorUndoRedoUtils.ReplayScope replay = EditorUndoRedoUtils.EnterReplay();

		graph.Variables.Add(variable);
		RebuildList();
		VariablesChanged?.Invoke();
	}

	private void UndoAddVariable(StatescriptGraph graph, StatescriptGraphVariable variable)
	{
		using EditorUndoRedoUtils.ReplayScope replay = EditorUndoRedoUtils.EnterReplay();

		graph.Variables.Remove(variable);
		RebuildList();
		VariablesChanged?.Invoke();
		VariableUndoRedoPerformed?.Invoke();
	}

	private void DoRemoveVariable(StatescriptGraph graph, StatescriptGraphVariable variable, int index)
	{
		using EditorUndoRedoUtils.ReplayScope replay = EditorUndoRedoUtils.EnterReplay();

		if (_selectedVariableName == variable.VariableName)
		{
			_selectedVariableName = null;
			VariableHighlightChanged?.Invoke(null);
		}

		graph.Variables.RemoveAt(index);
		ClearReferencesToVariable(variable.VariableName);
		RebuildList();
		VariablesChanged?.Invoke();
	}

	private void UndoRemoveVariable(StatescriptGraph graph, StatescriptGraphVariable variable, int index)
	{
		using EditorUndoRedoUtils.ReplayScope replay = EditorUndoRedoUtils.EnterReplay();

		if (index >= graph.Variables.Count)
		{
			graph.Variables.Add(variable);
		}
		else
		{
			graph.Variables.Insert(index, variable);
		}

		RebuildList();
		VariablesChanged?.Invoke();
		VariableUndoRedoPerformed?.Invoke();
	}

	private void DoAddArrayElement(StatescriptGraph graph, StatescriptGraphVariable variable, Variant value)
	{
		using EditorUndoRedoUtils.ReplayScope replay = EditorUndoRedoUtils.EnterReplay();

		variable.InitialArrayValues.Add(value);
		variable.EmitChanged();
		graph.EmitChanged();
		EnsureArrayExpanded(variable.VariableName);
		RebuildList();
	}

	private void UndoAddArrayElement(StatescriptGraph graph, StatescriptGraphVariable variable)
	{
		using EditorUndoRedoUtils.ReplayScope replay = EditorUndoRedoUtils.EnterReplay();

		if (variable.InitialArrayValues.Count > 0)
		{
			variable.InitialArrayValues.RemoveAt(variable.InitialArrayValues.Count - 1);
			variable.EmitChanged();
			graph.EmitChanged();
		}

		EnsureArrayExpanded(variable.VariableName);
		RebuildList();
		VariableUndoRedoPerformed?.Invoke();
	}

	private void DoRemoveArrayElement(StatescriptGraph graph, StatescriptGraphVariable variable, int index)
	{
		using EditorUndoRedoUtils.ReplayScope replay = EditorUndoRedoUtils.EnterReplay();

		variable.InitialArrayValues.RemoveAt(index);
		variable.EmitChanged();
		graph.EmitChanged();
		RebuildList();
	}

	private void UndoRemoveArrayElement(
		StatescriptGraph graph,
		StatescriptGraphVariable variable,
		int index,
		Variant value)
	{
		using EditorUndoRedoUtils.ReplayScope replay = EditorUndoRedoUtils.EnterReplay();

		if (index >= variable.InitialArrayValues.Count)
		{
			variable.InitialArrayValues.Add(value);
		}
		else
		{
			variable.InitialArrayValues.Insert(index, value);
		}

		variable.EmitChanged();
		graph.EmitChanged();
		EnsureArrayExpanded(variable.VariableName);
		RebuildList();
		VariableUndoRedoPerformed?.Invoke();
	}

	private void EnsureArrayExpanded(string variableName)
	{
		// Reveal the array so an undo/redo or programmatic change isn't hidden inside a collapsed entry. This adjusts
		// only the persisted expand state and does not record its own undo step.
		if (_expandedArrays.Add(variableName))
		{
			SaveExpandedArrayState();
		}
	}
}
#endif
