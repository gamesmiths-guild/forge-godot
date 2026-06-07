// Copyright © Gamesmiths Guild.

#if TOOLS
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Statescript;

internal sealed partial class StatescriptVariablePanel
{
	private Control CreateScalarValueEditor(StatescriptGraphVariable variable)
	{
		if (variable.VariableType == StatescriptVariableType.Entity)
		{
			var info = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
			info.AddChild(new Label { Text = "Runtime-assigned entity reference." });
			return info;
		}

		if (variable.VariableType == StatescriptVariableType.Bool)
		{
			var hBox = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };

			hBox.AddChild(StatescriptEditorControls.CreateBoolEditor(
				variable.InitialValue.AsBool(),
				x => SetVariableValue(variable, Variant.From(x))));

			return hBox;
		}

		if (StatescriptEditorControls.IsIntegerType(variable.VariableType)
			|| StatescriptEditorControls.IsFloatType(variable.VariableType))
		{
			var hBox = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };

			EditorSpinSlider spin = StatescriptEditorControls.CreateNumericSpinSlider(
				variable.VariableType,
				variable.InitialValue.AsDouble(),
				onChanged: x =>
				{
					Variant newValue = StatescriptEditorControls.IsIntegerType(variable.VariableType)
						? Variant.From((long)x)
						: Variant.From(x);
					SetVariableValue(variable, newValue);
				});

			hBox.AddChild(spin);
			return hBox;
		}

		if (StatescriptEditorControls.IsVectorType(variable.VariableType))
		{
			return StatescriptEditorControls.CreateVectorEditor(
				variable.VariableType,
				x => StatescriptEditorControls.GetVectorComponent(
					variable.InitialValue,
					variable.VariableType,
					x),
				onChanged: x =>
				{
					Variant newValue = StatescriptEditorControls.BuildVectorVariant(
						variable.VariableType,
						x);
					SetVariableValue(variable, newValue);
				});
		}

		var fallback = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		fallback.AddChild(new Label { Text = variable.VariableType.ToString() });
		return fallback;
	}

	private VBoxContainer CreateArrayValueEditor(StatescriptGraphVariable variable)
	{
		var vBox = new VBoxContainer
		{
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
		};

		if (variable.VariableType == StatescriptVariableType.Entity)
		{
			vBox.AddChild(new Label { Text = "Runtime-assigned entity references." });
			return vBox;
		}

		var headerRow = new HBoxContainer();
		vBox.AddChild(headerRow);

		bool isExpanded = _expandedArrays.Contains(variable.VariableName);

		var elementsContainer = new VBoxContainer
		{
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			Visible = isExpanded,
		};

		var toggleButton = new Button
		{
			Text = $"Array (size {variable.InitialArrayValues.Count})",
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			ToggleMode = true,
			ButtonPressed = isExpanded,
		};

		toggleButton.Toggled += x =>
		{
			elementsContainer.Visible = x;

			bool wasExpanded = !x;

			if (x)
			{
				_expandedArrays.Add(variable.VariableName);
			}
			else
			{
				_expandedArrays.Remove(variable.VariableName);
			}

			SaveExpandedArrayState();

			EditorUndoRedoUtils.Record(
				_undoRedo,
				"Toggle Array Expand",
				_graph,
				undo =>
				{
					undo.AddDoMethod(this, MethodName.DoSetArrayExpanded, variable.VariableName, x);
					undo.AddUndoMethod(this, MethodName.DoSetArrayExpanded, variable.VariableName, wasExpanded);
				});
		};

		headerRow.AddChild(toggleButton);

		var addElementButton = new Button
		{
			Icon = _addIcon,
			Flat = true,
			TooltipText = "Add Element",
			CustomMinimumSize = new Vector2(24, 24),
		};

		addElementButton.Pressed += () =>
		{
			Variant defaultValue =
				StatescriptVariableTypeConverter.CreateDefaultGodotVariant(variable.VariableType);

			EditorUndoRedoUtils.Record(
				_undoRedo,
				"Add Array Element",
				_graph,
				undo =>
				{
					undo.AddDoMethod(this, MethodName.DoAddArrayElement, variable, defaultValue);
					undo.AddUndoMethod(this, MethodName.UndoAddArrayElement, variable);
				},
				execute: true,
				fallback: () => DoAddArrayElement(variable, defaultValue));
		};

		headerRow.AddChild(addElementButton);

		vBox.AddChild(elementsContainer);

		for (int i = 0; i < variable.InitialArrayValues.Count; i++)
		{
			int capturedIndex = i;

			if (variable.VariableType == StatescriptVariableType.Bool)
			{
				var elementRow = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
				elementsContainer.AddChild(elementRow);
				elementRow.AddChild(new Label { Text = $"[{i}]" });

				elementRow.AddChild(StatescriptEditorControls.CreateBoolEditor(
					variable.InitialArrayValues[i].AsBool(),
					x => SetArrayElementValueWithUndo(
						variable,
						capturedIndex,
						Variant.From(x))));

				AddArrayElementRemoveButton(elementRow, variable, capturedIndex);
			}
			else if (StatescriptEditorControls.IsVectorType(variable.VariableType))
			{
				var elementVBox = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
				elementsContainer.AddChild(elementVBox);

				var labelRow = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
				elementVBox.AddChild(labelRow);
				labelRow.AddChild(new Label
				{
					Text = $"[{i}]",
					SizeFlagsHorizontal = SizeFlags.ExpandFill,
				});

				AddArrayElementRemoveButton(labelRow, variable, capturedIndex);

				VBoxContainer vectorEditor = StatescriptEditorControls.CreateVectorEditor(
					variable.VariableType,
					x => StatescriptEditorControls.GetVectorComponent(
						variable.InitialArrayValues[capturedIndex],
						variable.VariableType,
						x),
					x =>
					{
						Variant newValue = StatescriptEditorControls.BuildVectorVariant(
							variable.VariableType,
							x);
						SetArrayElementValueWithUndo(variable, capturedIndex, newValue);
					});

				elementVBox.AddChild(vectorEditor);
			}
			else
			{
				var elementRow = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
				elementsContainer.AddChild(elementRow);
				elementRow.AddChild(new Label { Text = $"[{i}]" });

				EditorSpinSlider elementSpin = StatescriptEditorControls.CreateNumericSpinSlider(
					variable.VariableType,
					variable.InitialArrayValues[i].AsDouble(),
					onChanged: x =>
					{
						Variant newValue = StatescriptEditorControls.IsIntegerType(variable.VariableType)
							? Variant.From((long)x)
							: Variant.From(x);
						SetArrayElementValueWithUndo(variable, capturedIndex, newValue);
					});

				elementRow.AddChild(elementSpin);
				AddArrayElementRemoveButton(elementRow, variable, capturedIndex);
			}
		}

		return vBox;
	}

	private void AddArrayElementRemoveButton(
		HBoxContainer row,
		StatescriptGraphVariable variable,
		int elementIndex)
	{
		var removeElementButton = new Button
		{
			Icon = _removeIcon,
			Flat = true,
			CustomMinimumSize = new Vector2(24, 24),
		};

		removeElementButton.Pressed += () =>
		{
			Variant removedValue = variable.InitialArrayValues[elementIndex];

			EditorUndoRedoUtils.Record(
				_undoRedo,
				"Remove Array Element",
				_graph,
				undo =>
				{
					undo.AddDoMethod(this, MethodName.DoRemoveArrayElement, variable, elementIndex);
					undo.AddUndoMethod(this, MethodName.UndoRemoveArrayElement, variable, elementIndex, removedValue);
				},
				execute: true,
				fallback: () => DoRemoveArrayElement(variable, elementIndex));
		};

		row.AddChild(removeElementButton);
	}
}
#endif
