// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using System.Collections.Generic;
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Statescript;

/// <summary>
/// Right-side panel for editing graph variables. Variables are created with a name and type via a creation dialog.
/// Once created, only the initial value can be edited. To change name or type, delete and recreate the variable.
/// </summary>
[Tool]
internal sealed partial class StatescriptVariablePanel : VBoxContainer, ISerializationListener
{
	private const string ExpandedArraysMetaKey = "_expanded_arrays";

	private readonly HashSet<string> _expandedArrays = [];

	private StatescriptGraph? _graph;
	private VBoxContainer? _variableList;
	private Button? _addButton;

	private Window? _creationDialog;
	private LineEdit? _newNameEdit;
	private OptionButton? _newTypeDropdown;
	private CheckBox? _newArrayToggle;

	private Texture2D? _addIcon;
	private Texture2D? _removeIcon;

	private EditorUndoRedoManager? _undoRedo;

	/// <summary>
	/// Raised when any variable is added, removed, or its value changes.
	/// </summary>
	public event Action? VariablesChanged;

	public override void _Ready()
	{
		base._Ready();

		_addIcon = EditorInterface.Singleton.GetEditorTheme().GetIcon("Add", "EditorIcons");
		_removeIcon = EditorInterface.Singleton.GetEditorTheme().GetIcon("Remove", "EditorIcons");

		SizeFlagsVertical = SizeFlags.ExpandFill;
		CustomMinimumSize = new Vector2(360, 0);

		var headerHBox = new HBoxContainer();
		AddChild(headerHBox);

		var titleLabel = new Label
		{
			Text = "Graph Variables",
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
		};

		headerHBox.AddChild(titleLabel);

		_addButton = new Button
		{
			Icon = _addIcon,
			Flat = true,
			TooltipText = "Add Variable",
			CustomMinimumSize = new Vector2(28, 28),
		};

		_addButton.Pressed += OnAddPressed;
		headerHBox.AddChild(_addButton);

		var separator = new HSeparator();
		AddChild(separator);

		var scrollContainer = new ScrollContainer
		{
			SizeFlagsVertical = SizeFlags.ExpandFill,
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
		};

		AddChild(scrollContainer);

		_variableList = new VBoxContainer
		{
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
		};

		scrollContainer.AddChild(_variableList);
	}

	public override void _ExitTree()
	{
		base._ExitTree();

		_creationDialog?.QueueFree();
		_creationDialog = null;
		_newNameEdit = null;
		_newTypeDropdown = null;
		_newArrayToggle = null;
	}

	public void OnBeforeSerialize()
	{
		_creationDialog = null;
		_newNameEdit = null;
		_newTypeDropdown = null;
		_newArrayToggle = null;
	}

	public void OnAfterDeserialize()
	{
		// Nothing to restore, dialog fields are transient.
	}

	/// <summary>
	/// Sets the graph to display variables for.
	/// </summary>
	/// <param name="graph">The graph resource, or null to clear.</param>
	public void SetGraph(StatescriptGraph? graph)
	{
		_graph = graph;
		LoadExpandedArrayState();
		RebuildList();
	}

	/// <summary>
	/// Sets the <see cref="EditorUndoRedoManager"/> used for undo/redo support.
	/// </summary>
	/// <param name="undoRedo">The undo/redo manager from the editor plugin.</param>
	public void SetUndoRedo(EditorUndoRedoManager undoRedo)
	{
		_undoRedo = undoRedo;
	}

	/// <summary>
	/// Rebuilds the variable list UI from the current graph.
	/// </summary>
	public void RebuildList()
	{
		if (_variableList is null)
		{
			return;
		}

		foreach (Node child in _variableList.GetChildren())
		{
			child.QueueFree();
		}

		if (_graph is null)
		{
			return;
		}

		for (var i = 0; i < _graph.Variables.Count; i++)
		{
			AddVariableRow(_graph.Variables[i], i);
		}
	}

	private static void SetArrayElementValue(StatescriptGraphVariable variable, int index, Variant newValue)
	{
		variable.InitialArrayValues[index] = newValue;
		variable.EmitChanged();
	}

	private void SaveExpandedArrayState()
	{
		if (_graph is null)
		{
			return;
		}

		var packed = new string[_expandedArrays.Count];
		_expandedArrays.CopyTo(packed);
		_graph.SetMeta(ExpandedArraysMetaKey, Variant.From(packed));
	}

	private void LoadExpandedArrayState()
	{
		_expandedArrays.Clear();

		if (_graph?.HasMeta(ExpandedArraysMetaKey) != true)
		{
			return;
		}

		Variant meta = _graph.GetMeta(ExpandedArraysMetaKey);

		if (meta.VariantType == Variant.Type.PackedStringArray)
		{
			foreach (var name in meta.AsStringArray())
			{
				_expandedArrays.Add(name);
			}
		}
	}

	private void AddVariableRow(StatescriptGraphVariable variable, int index)
	{
		if (_variableList is null)
		{
			return;
		}

		var rowContainer = new VBoxContainer
		{
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
		};

		_variableList.AddChild(rowContainer);

		var headerRow = new HBoxContainer
		{
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
		};

		rowContainer.AddChild(headerRow);

		var nameLabel = new Label
		{
			Text = variable.VariableName,
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
		};

		nameLabel.AddThemeColorOverride("font_color", new(0xe5c07bff));
		nameLabel.AddThemeFontOverride(
			"font",
			EditorInterface.Singleton.GetEditorTheme().GetFont("bold", "EditorFonts"));

		headerRow.AddChild(nameLabel);

		var typeLabel = new Label
		{
			Text = $"({StatescriptVariableTypeConverter.GetDisplayName(variable.VariableType)}"
				+ (variable.IsArray ? "[])" : ")"),
		};

		typeLabel.AddThemeColorOverride("font_color", new Color(0.6f, 0.6f, 0.6f));
		headerRow.AddChild(typeLabel);

		var capturedIndex = index;

		var deleteButton = new Button
		{
			Icon = _removeIcon,
			Flat = true,
			TooltipText = "Remove Variable",
			CustomMinimumSize = new Vector2(28, 28),
		};

		deleteButton.Pressed += () => OnDeletePressed(capturedIndex);
		headerRow.AddChild(deleteButton);

		if (!variable.IsArray)
		{
			Control valueEditor = CreateScalarValueEditor(variable);
			rowContainer.AddChild(valueEditor);
		}
		else
		{
			VBoxContainer arrayEditor = CreateArrayValueEditor(variable);
			rowContainer.AddChild(arrayEditor);
		}

		rowContainer.AddChild(new HSeparator());
	}

	private Control CreateScalarValueEditor(StatescriptGraphVariable variable)
	{
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

		var headerRow = new HBoxContainer();
		vBox.AddChild(headerRow);

		var isExpanded = _expandedArrays.Contains(variable.VariableName);

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

			if (x)
			{
				_expandedArrays.Add(variable.VariableName);
			}
			else
			{
				_expandedArrays.Remove(variable.VariableName);
			}

			SaveExpandedArrayState();
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

			if (_undoRedo is not null)
			{
				_undoRedo.CreateAction("Add Array Element", customContext: _graph);
				_undoRedo.AddDoMethod(
					this,
					MethodName.DoAddArrayElement,
					variable,
					defaultValue);
				_undoRedo.AddUndoMethod(
					this,
					MethodName.UndoAddArrayElement,
					variable);
				_undoRedo.CommitAction();
			}
			else
			{
				DoAddArrayElement(variable, defaultValue);
			}
		};

		headerRow.AddChild(addElementButton);

		vBox.AddChild(elementsContainer);

		for (var i = 0; i < variable.InitialArrayValues.Count; i++)
		{
			var capturedIndex = i;

			if (variable.VariableType == StatescriptVariableType.Bool)
			{
				var elementRow = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
				elementsContainer.AddChild(elementRow);
				elementRow.AddChild(new Label { Text = $"[{i}]" });

				elementRow.AddChild(StatescriptEditorControls.CreateBoolEditor(
					variable.InitialArrayValues[i].AsBool(),
					x => SetArrayElementValue(
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
						SetArrayElementValue(variable, capturedIndex, newValue);
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
						SetArrayElementValue(variable, capturedIndex, newValue);
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
			if (_undoRedo is not null)
			{
				Variant removedValue = variable.InitialArrayValues[elementIndex];

				_undoRedo.CreateAction("Remove Array Element", customContext: _graph);
				_undoRedo.AddDoMethod(
					this,
					MethodName.DoRemoveArrayElement,
					variable,
					elementIndex);
				_undoRedo.AddUndoMethod(
					this,
					MethodName.UndoRemoveArrayElement,
					variable,
					elementIndex,
					removedValue);
				_undoRedo.CommitAction();
			}
			else
			{
				DoRemoveArrayElement(variable, elementIndex);
			}
		};

		row.AddChild(removeElementButton);
	}

	private void OnAddPressed()
	{
		if (_graph is null)
		{
			return;
		}

		ShowCreationDialog();
	}

	private void ShowCreationDialog()
	{
		_creationDialog?.QueueFree();

		_creationDialog = new AcceptDialog
		{
			Title = "Add Variable",
			Size = new Vector2I(300, 160),
			Exclusive = true,
		};

		var vBox = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };

		var nameRow = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		vBox.AddChild(nameRow);

		nameRow.AddChild(new Label { Text = "Name:", CustomMinimumSize = new Vector2(60, 0) });

		_newNameEdit = new LineEdit
		{
			Text = GenerateUniqueName(),
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
		};

		nameRow.AddChild(_newNameEdit);

		var typeRow = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		vBox.AddChild(typeRow);

		typeRow.AddChild(new Label { Text = "Type:", CustomMinimumSize = new Vector2(60, 0) });

		_newTypeDropdown = new OptionButton { SizeFlagsHorizontal = SizeFlags.ExpandFill };

		StatescriptVariableType[] allTypes = StatescriptVariableTypeConverter.GetAllTypes();

		for (var t = 0; t < allTypes.Length; t++)
		{
			_newTypeDropdown.AddItem(StatescriptVariableTypeConverter.GetDisplayName(allTypes[t]), t);
		}

		_newTypeDropdown.Selected = (int)StatescriptVariableType.Int;
		typeRow.AddChild(_newTypeDropdown);

		var arrayRow = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		vBox.AddChild(arrayRow);

		arrayRow.AddChild(new Label { Text = "Array:", CustomMinimumSize = new Vector2(60, 0) });

		_newArrayToggle = new CheckBox();
		arrayRow.AddChild(_newArrayToggle);

		_creationDialog.AddChild(vBox);

		((AcceptDialog)_creationDialog).Confirmed += OnCreationConfirmed;

		AddChild(_creationDialog);
		_creationDialog.PopupCentered();
	}

	private void OnCreationConfirmed()
	{
		if (_graph is null || _newNameEdit is null || _newTypeDropdown is null || _newArrayToggle is null)
		{
			return;
		}

		var name = _newNameEdit.Text.Trim();

		if (string.IsNullOrEmpty(name) || HasVariableNamed(name))
		{
			return;
		}

		var varType = (StatescriptVariableType)_newTypeDropdown.Selected;

		var newVariable = new StatescriptGraphVariable
		{
			VariableName = name,
			VariableType = varType,
			IsArray = _newArrayToggle.ButtonPressed,
			InitialValue = StatescriptVariableTypeConverter.CreateDefaultGodotVariant(varType),
		};

		if (_undoRedo is not null)
		{
			_undoRedo.CreateAction("Add Graph Variable", customContext: _graph);
			_undoRedo.AddDoMethod(this, MethodName.DoAddVariable, _graph!, newVariable);
			_undoRedo.AddUndoMethod(this, MethodName.UndoAddVariable, _graph!, newVariable);
			_undoRedo.CommitAction();
		}
		else
		{
			DoAddVariable(_graph, newVariable);
		}

		_creationDialog?.QueueFree();
		_creationDialog = null;
		_newNameEdit = null;
		_newTypeDropdown = null;
		_newArrayToggle = null;
	}

	private void OnDeletePressed(int index)
	{
		if (_graph is null || index < 0 || index >= _graph.Variables.Count)
		{
			return;
		}

		StatescriptGraphVariable variable = _graph.Variables[index];

		if (_undoRedo is not null)
		{
			_undoRedo.CreateAction("Remove Graph Variable", customContext: _graph);
			_undoRedo.AddDoMethod(this, MethodName.DoRemoveVariable, _graph!, variable, index);
			_undoRedo.AddUndoMethod(this, MethodName.UndoRemoveVariable, _graph!, variable, index);
			_undoRedo.CommitAction();
		}
		else
		{
			DoRemoveVariable(_graph, variable, index);
		}
	}

	private void ClearReferencesToVariable(string variableName)
	{
		if (_graph is null)
		{
			return;
		}

		foreach (StatescriptNode node in _graph.Nodes)
		{
			foreach (StatescriptNodeProperty binding in node.PropertyBindings)
			{
				if (binding.Resolver is VariableResolverResource varRes
					&& varRes.VariableName == variableName)
				{
					varRes.VariableName = string.Empty;
				}
			}
		}
	}

	private string GenerateUniqueName()
	{
		if (_graph is null)
		{
			return "variable";
		}

		const string baseName = "variable";
		var counter = 1;
		var name = baseName;

		while (HasVariableNamed(name))
		{
			name = $"{baseName}_{counter++}";
		}

		return name;
	}

	private bool HasVariableNamed(string name)
	{
		if (_graph is null)
		{
			return false;
		}

		foreach (StatescriptGraphVariable variable in _graph.Variables)
		{
			if (variable.VariableName == name)
			{
				return true;
			}
		}

		return false;
	}

	private void ApplyVariableValue(StatescriptGraphVariable variable, Variant value)
	{
		variable.InitialValue = value;
		variable.EmitChanged();
		RebuildList();
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
	}

	private void DoRemoveVariable(StatescriptGraph graph, StatescriptGraphVariable variable, int index)
	{
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
}
#endif
