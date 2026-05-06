// Copyright © Gamesmiths Guild.

#if TOOLS
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Statescript;

internal sealed partial class StatescriptVariablePanel
{
	private static void SetArrayElementValue(StatescriptGraphVariable variable, int index, Variant newValue)
	{
		variable.InitialArrayValues[index] = newValue;
		variable.EmitChanged();
	}

	private void SetVariableValue(StatescriptGraphVariable variable, Variant newValue)
	{
		Variant oldValue = variable.InitialValue;

		variable.InitialValue = newValue;
		variable.EmitChanged();

		if (_undoRedo is not null)
		{
			_undoRedo.CreateAction(
				$"Change Variable '{variable.VariableName}'",
				customContext: _graph);

			_undoRedo.AddDoMethod(
				this,
				MethodName.ApplyVariableValue,
				variable,
				newValue);

			_undoRedo.AddUndoMethod(
				this,
				MethodName.ApplyVariableValue,
				variable,
				oldValue);

			_undoRedo.CommitAction(false);
		}
	}

	private void ApplyVariableValue(StatescriptGraphVariable variable, Variant value)
	{
		variable.InitialValue = value;
		variable.EmitChanged();
		RebuildList();
		VariableUndoRedoPerformed?.Invoke();
	}

	private void DoAddVariable(StatescriptGraph graph, StatescriptGraphVariable variable)
	{
		graph.Variables.Add(variable);
		RebuildList();
		VariablesChanged?.Invoke();
	}

	private void UndoAddVariable(StatescriptGraph graph, StatescriptGraphVariable variable)
	{
		graph.Variables.Remove(variable);
		RebuildList();
		VariablesChanged?.Invoke();
		VariableUndoRedoPerformed?.Invoke();
	}

	private void DoRemoveVariable(StatescriptGraph graph, StatescriptGraphVariable variable, int index)
	{
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

	private void DoAddArrayElement(StatescriptGraphVariable variable, Variant value)
	{
		variable.InitialArrayValues.Add(value);
		variable.EmitChanged();
		_expandedArrays.Add(variable.VariableName);
		SaveExpandedArrayState();
		RebuildList();
	}

	private void UndoAddArrayElement(StatescriptGraphVariable variable)
	{
		if (variable.InitialArrayValues.Count > 0)
		{
			variable.InitialArrayValues.RemoveAt(variable.InitialArrayValues.Count - 1);
			variable.EmitChanged();
		}

		RebuildList();
		VariableUndoRedoPerformed?.Invoke();
	}

	private void DoRemoveArrayElement(StatescriptGraphVariable variable, int index)
	{
		variable.InitialArrayValues.RemoveAt(index);
		variable.EmitChanged();
		RebuildList();
	}

	private void UndoRemoveArrayElement(StatescriptGraphVariable variable, int index, Variant value)
	{
		if (index >= variable.InitialArrayValues.Count)
		{
			variable.InitialArrayValues.Add(value);
		}
		else
		{
			variable.InitialArrayValues.Insert(index, value);
		}

		variable.EmitChanged();
		RebuildList();
		VariableUndoRedoPerformed?.Invoke();
	}

	private void DoSetArrayExpanded(string variableName, bool expanded)
	{
		if (expanded)
		{
			_expandedArrays.Add(variableName);
		}
		else
		{
			_expandedArrays.Remove(variableName);
		}

		SaveExpandedArrayState();
		RebuildList();
		VariableUndoRedoPerformed?.Invoke();
	}
}
#endif
