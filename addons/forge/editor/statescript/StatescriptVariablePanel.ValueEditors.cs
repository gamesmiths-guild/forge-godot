// Copyright © Gamesmiths Guild.

#if TOOLS
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Statescript;

internal sealed partial class StatescriptVariablePanel
{
	private Control CreateScalarValueEditor(StatescriptGraphVariable variable)
	{
		if (!string.IsNullOrEmpty(variable.ObjectTypeId))
		{
			var info = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
			info.AddChild(new Label { Text = "Runtime-assigned reference." });
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

		if (!string.IsNullOrEmpty(variable.ObjectTypeId))
		{
			vBox.AddChild(new Label { Text = "Runtime-assigned references." });
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

		// Method callables reading the row's data off the emitting button, not lambdas: an assembly reload restores the
		// objects a lambda captured as copies.
		toggleButton.SetMeta(ArrayVariableMetaKey, variable);
		toggleButton.SetMeta(ArrayElementsMetaKey, elementsContainer);
		toggleButton.Connect(
			BaseButton.SignalName.Toggled,
			new Callable(this, MethodName.OnArrayToggled),
			(uint)ConnectFlags.AppendSourceObject);

		headerRow.AddChild(toggleButton);

		var addElementButton = new Button
		{
			Icon = _addIcon,
			Flat = true,
			TooltipText = "Add Element",
			CustomMinimumSize = new Vector2(24, 24),
		};

		addElementButton.SetMeta(ArrayVariableMetaKey, variable);
		addElementButton.Connect(
			BaseButton.SignalName.Pressed,
			new Callable(this, MethodName.OnAddArrayElementPressed),
			(uint)ConnectFlags.AppendSourceObject);

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

		removeElementButton.SetMeta(ArrayVariableMetaKey, variable);
		removeElementButton.SetMeta(ArrayElementIndexMetaKey, elementIndex);
		removeElementButton.Connect(
			BaseButton.SignalName.Pressed,
			new Callable(this, MethodName.OnRemoveArrayElementPressed),
			(uint)ConnectFlags.AppendSourceObject);

		row.AddChild(removeElementButton);
	}

	private void OnArrayToggled(bool expanded, Button toggleButton)
	{
		toggleButton.GetMeta(ArrayElementsMetaKey).As<VBoxContainer>().Visible = expanded;
		string variableName = toggleButton.GetMeta(ArrayVariableMetaKey).As<StatescriptGraphVariable>().VariableName;

		if (expanded)
		{
			_expandedArrays.Add(variableName);
		}
		else
		{
			_expandedArrays.Remove(variableName);
		}

		// Persisted but not recorded: expanding a row is view state, same as a collapsed foldable.
		SaveExpandedArrayState();
	}

	private void OnAddArrayElementPressed(Button addElementButton)
	{
		StatescriptGraphVariable variable =
			addElementButton.GetMeta(ArrayVariableMetaKey).As<StatescriptGraphVariable>();
		Variant defaultValue = StatescriptVariableTypeConverter.CreateDefaultGodotVariant(variable.VariableType);

		EditorUndoRedoUtils.Record(
			_undoRedo,
			"Add Array Element",
			_graph,
			undo =>
			{
				undo.AddDoMethod(this, MethodName.DoAddArrayElement, _graph!, variable, defaultValue);
				undo.AddUndoMethod(this, MethodName.UndoAddArrayElement, _graph!, variable);
			},
			execute: true,
			fallback: () => DoAddArrayElement(_graph!, variable, defaultValue));
	}

	private void OnRemoveArrayElementPressed(Button removeElementButton)
	{
		StatescriptGraphVariable variable =
			removeElementButton.GetMeta(ArrayVariableMetaKey).As<StatescriptGraphVariable>();
		int elementIndex = removeElementButton.GetMeta(ArrayElementIndexMetaKey).AsInt32();
		Variant removedValue = variable.InitialArrayValues[elementIndex];

		EditorUndoRedoUtils.Record(
			_undoRedo,
			"Remove Array Element",
			_graph,
			undo =>
			{
				undo.AddDoMethod(this, MethodName.DoRemoveArrayElement, _graph!, variable, elementIndex);
				undo.AddUndoMethod(
					this,
					MethodName.UndoRemoveArrayElement,
					_graph!,
					variable,
					elementIndex,
					removedValue);
			},
			execute: true,
			fallback: () => DoRemoveArrayElement(_graph!, variable, elementIndex));
	}
}
#endif
