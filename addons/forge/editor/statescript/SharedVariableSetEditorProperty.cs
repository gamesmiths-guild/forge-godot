// Copyright © Gamesmiths Guild.

#if TOOLS
using System.Collections.Generic;
using Gamesmiths.Forge.Godot.Resources;
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Godot;
using Godot.Collections;

namespace Gamesmiths.Forge.Godot.Editor.Statescript;

/// <summary>
/// Custom <see cref="EditorProperty"/> that renders the <see cref="ForgeSharedVariableSet.Variables"/> array using the
/// same polished value-editor controls as the graph variable panel.
/// </summary>
[Tool]
internal sealed partial class SharedVariableSetEditorProperty : EditorProperty, ISerializationListener
{
	private const string BackgroundPanelNodeName = "BackgroundPanel";
	private const string RootNodeName = "Root";
	private const string HeaderRowNodeName = "HeaderRow";
	private const string AddButtonNodeName = "AddButton";
	private const string VariableListNodeName = "VariableList";
	private const string VariableNameButtonMetaKey = "_shared_variable_name_button";

	private static readonly Color _variableColor = new(0xe5c07bff);
	private static readonly Color _highlightColor = new(0x56b6c2ff);

	private readonly HashSet<string> _expandedArrays = [];

	private EditorUndoRedoManager? _undoRedo;

	private VBoxContainer? _root;
	private VBoxContainer? _variableList;
	private Button? _addButton;

	private AcceptDialog? _creationDialog;
	private LineEdit? _newNameEdit;
	private OptionButton? _newTypeDropdown;
	private OptionButton? _newValueShapeDropdown;

	private Texture2D? _addIcon;
	private Texture2D? _removeIcon;
	private string? _selectedVariableName;

	/// <summary>
	/// Sets the <see cref="EditorUndoRedoManager"/> used for undo/redo support.
	/// </summary>
	/// <param name="undoRedo">The undo/redo manager from the editor plugin.</param>
	public void SetUndoRedo(EditorUndoRedoManager? undoRedo)
	{
		_undoRedo = undoRedo;
	}

	public override void _Ready()
	{
		base._Ready();
		SharedVariableHighlightState.Changed += OnSharedVariableHighlightChanged;

		_addIcon = EditorInterface.Singleton.GetEditorTheme().GetIcon("Add", "EditorIcons");
		_removeIcon = EditorInterface.Singleton.GetEditorTheme().GetIcon("Remove", "EditorIcons");

		if (GetEditedObject() is ForgeSharedVariableSet sharedVariableSet)
		{
			SharedVariableHighlightState.SetInspectorContext(sharedVariableSet.ResourcePath);
			SharedVariableHighlightState.SetSelection(sharedVariableSet.ResourcePath, _selectedVariableName);
		}

		var backgroundPanel = new PanelContainer
		{
			Name = BackgroundPanelNodeName,
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
		};

		var panelStyle = new StyleBoxFlat
		{
			BgColor = EditorInterface.Singleton.GetEditorTheme().GetColor("base_color", "Editor"),
			ContentMarginLeft = 6,
			ContentMarginRight = 6,
			ContentMarginTop = 4,
			ContentMarginBottom = 4,
			CornerRadiusTopLeft = 3,
			CornerRadiusTopRight = 3,
			CornerRadiusBottomLeft = 3,
			CornerRadiusBottomRight = 3,
		};

		backgroundPanel.AddThemeStyleboxOverride("panel", panelStyle);
		AddChild(backgroundPanel);
		SetBottomEditor(backgroundPanel);

		_root = new VBoxContainer
		{
			Name = RootNodeName,
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
		};
		backgroundPanel.AddChild(_root);

		var headerHBox = new HBoxContainer { Name = HeaderRowNodeName };
		_root.AddChild(headerHBox);

		_addButton = new Button
		{
			Name = AddButtonNodeName,
			Text = "Add Variable",
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
		};

		_addButton.Pressed += OnAddPressed;
		headerHBox.AddChild(_addButton);

		_root.AddChild(new HSeparator());

		_variableList = new VBoxContainer
		{
			Name = VariableListNodeName,
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
		};
		_root.AddChild(_variableList);
	}

	public override void _UpdateProperty()
	{
		EnsureControlsCached();

		if (GetEditedObject() is ForgeSharedVariableSet sharedVariableSet)
		{
			SharedVariableHighlightState.SetInspectorContext(sharedVariableSet.ResourcePath);
		}

		SyncSelectedVariableFromHighlightState();
		RebuildList();
	}

	public override void _ExitTree()
	{
		SharedVariableHighlightState.Changed -= OnSharedVariableHighlightChanged;

		if (GetEditedObject() is ForgeSharedVariableSet sharedVariableSet)
		{
			SharedVariableHighlightState.ClearInspectorContext(sharedVariableSet.ResourcePath);
		}

		ReleaseUiState();
		FreeAllChildren();

		base._ExitTree();
	}

	public void OnBeforeSerialize()
	{
		SharedVariableHighlightState.Changed -= OnSharedVariableHighlightChanged;
		ReleaseUiState();
	}

	public void OnAfterDeserialize()
	{
		EnsureControlsCached();
		SharedVariableHighlightState.Changed += OnSharedVariableHighlightChanged;
		SyncSelectedVariableFromHighlightState();

		if (_addButton is not null && IsInstanceValid(_addButton))
		{
			_addButton.Pressed += OnAddPressed;
		}

		RebuildList();
	}

	private static void UpdateVariableNameButtonAppearance(Button button, bool isSelected)
	{
		Color buttonColor = isSelected ? _highlightColor : _variableColor;
		button.AddThemeColorOverride("font_color", buttonColor);
		button.AddThemeColorOverride("font_pressed_color", buttonColor);
		button.AddThemeColorOverride("font_hover_color", buttonColor.Lightened(0.2f));
		button.AddThemeColorOverride("font_hover_pressed_color", buttonColor.Lightened(0.2f));
	}

	private Array<ForgeSharedVariableDefinition> GetDefinitions()
	{
		GodotObject obj = GetEditedObject();
		string propertyName = GetEditedProperty();
		Variant value = obj.Get(propertyName);

		return value.AsGodotArray<ForgeSharedVariableDefinition>() ?? [];
	}

	private void NotifyChanged()
	{
		GodotObject obj = GetEditedObject();
		string propertyName = GetEditedProperty();

		if (obj is not ForgeSharedVariableSet sharedVariableSet)
		{
			return;
		}

		obj.Set(propertyName, sharedVariableSet.Variables);
		EmitChanged(propertyName, sharedVariableSet.Variables);

		if (obj is Resource resource)
		{
			resource.EmitChanged();
		}
	}

	private void RebuildList()
	{
		EnsureControlsCached();

		if (_variableList is null)
		{
			return;
		}

		SyncSelectedVariableFromHighlightState();

		ClearVariableList();

		Array<ForgeSharedVariableDefinition> definitions = GetDefinitions();

		for (int i = 0; i < definitions.Count; i++)
		{
			AddVariableRow(definitions, i);
		}

		RefreshVariableSelectionVisuals();
	}

	private void ClearVariableList()
	{
		EnsureControlsCached();

		if (_variableList is null)
		{
			return;
		}

		foreach (Node child in _variableList.GetChildren())
		{
			_variableList.RemoveChild(child);
			child.Free();
		}
	}

	private void EnsureControlsCached()
	{
		_root ??= GetNodeOrNull<VBoxContainer>(
			$"{BackgroundPanelNodeName}/{RootNodeName}");
		_addButton ??= GetNodeOrNull<Button>(
			$"{BackgroundPanelNodeName}/{RootNodeName}/{HeaderRowNodeName}/{AddButtonNodeName}");
		_variableList ??= GetNodeOrNull<VBoxContainer>(
			$"{BackgroundPanelNodeName}/{RootNodeName}/{VariableListNodeName}");
	}

	private void ReleaseUiState()
	{
		if (_addButton is not null && IsInstanceValid(_addButton))
		{
			_addButton.Pressed -= OnAddPressed;
		}

		ClearVariableList();

		if (_creationDialog is not null && IsInstanceValid(_creationDialog))
		{
			_creationDialog.Confirmed -= OnCreationConfirmed;
			_creationDialog.Canceled -= OnCreationCanceled;
			_creationDialog.Free();
		}

		_creationDialog = null;
		_newNameEdit = null;
		_newTypeDropdown = null;
		_newValueShapeDropdown = null;
		_root = null;
		_variableList = null;
		_addButton = null;
	}

	private void FreeAllChildren()
	{
		for (int i = GetChildCount() - 1; i >= 0; i--)
		{
			Node child = GetChild(i);
			RemoveChild(child);
			child.Free();
		}
	}

	private void OnSharedVariableHighlightChanged()
	{
		SyncSelectedVariableFromHighlightState();
		RefreshVariableSelectionVisuals();
	}

	private void SyncSelectedVariableFromHighlightState()
	{
		if (GetEditedObject() is not ForgeSharedVariableSet sharedVariableSet)
		{
			_selectedVariableName = null;
			return;
		}

		if (SharedVariableHighlightState.TryGetActiveSelection(out string selectedSetPath, out string variableName)
			&& string.Equals(selectedSetPath, sharedVariableSet.ResourcePath, System.StringComparison.Ordinal))
		{
			_selectedVariableName = variableName;
			return;
		}

		_selectedVariableName = null;
	}

	private void AddVariableRow(Array<ForgeSharedVariableDefinition> definitions, int index)
	{
		if (_variableList is null || index < 0 || index >= definitions.Count)
		{
			return;
		}

		ForgeSharedVariableDefinition def = definitions[index];

		var rowContainer = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		_variableList.AddChild(rowContainer);

		var headerRow = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		rowContainer.AddChild(headerRow);

		bool isSelected = _selectedVariableName == def.VariableName;

		var nameButton = new Button
		{
			Text = def.VariableName,
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			Flat = true,
			ToggleMode = true,
			ButtonPressed = isSelected,
			Alignment = HorizontalAlignment.Left,
		};

		nameButton.SetMeta(VariableNameButtonMetaKey, def.VariableName);
		UpdateVariableNameButtonAppearance(nameButton, isSelected);
		nameButton.AddThemeFontOverride(
			"font",
			EditorInterface.Singleton.GetEditorTheme().GetFont("bold", "EditorFonts"));
		nameButton.Toggled += pressed => SetSelectedVariable(def.VariableName, pressed);
		headerRow.AddChild(nameButton);

		var typeLabel = new Label
		{
			Text = $"({StatescriptVariableTypeConverter.GetDisplayName(def.VariableType)}"
				+ (def.IsArray ? "[])" : ")"),
		};

		typeLabel.AddThemeColorOverride("font_color", new Color(0.6f, 0.6f, 0.6f));
		headerRow.AddChild(typeLabel);

		int capturedIndex = index;

		var deleteButton = new Button
		{
			Icon = _removeIcon,
			Flat = true,
			TooltipText = "Remove Variable",
			CustomMinimumSize = new Vector2(28, 28),
		};

		deleteButton.Pressed += () => OnDeletePressed(capturedIndex);
		headerRow.AddChild(deleteButton);

		if (!def.IsArray)
		{
			Control valueEditor = CreateValueEditor(def);
			rowContainer.AddChild(valueEditor);
		}
		else
		{
			VBoxContainer arrayEditor = CreateArrayValueEditor(def);
			rowContainer.AddChild(arrayEditor);
		}

		rowContainer.AddChild(new HSeparator());
	}

	private void SetSelectedVariable(string variableName, bool selected)
	{
		if (selected)
		{
			_selectedVariableName = variableName;
		}
		else if (_selectedVariableName == variableName)
		{
			_selectedVariableName = null;
		}

		if (GetEditedObject() is ForgeSharedVariableSet sharedVariableSet)
		{
			SharedVariableHighlightState.SetInspectorContext(sharedVariableSet.ResourcePath);
			SharedVariableHighlightState.SetSelection(sharedVariableSet.ResourcePath, _selectedVariableName);
		}
		else
		{
			SharedVariableHighlightState.SetSelection(null, null);
		}

		RefreshVariableSelectionVisuals();
	}

	private void RefreshVariableSelectionVisuals()
	{
		if (_variableList is null)
		{
			return;
		}

		RefreshVariableSelectionVisualsRecursive(_variableList);
	}

	private void RefreshVariableSelectionVisualsRecursive(Node parent)
	{
		foreach (Node child in parent.GetChildren())
		{
			if (child is Button button && button.HasMeta(VariableNameButtonMetaKey))
			{
				string variableName = button.GetMeta(VariableNameButtonMetaKey).AsString();
				bool isSelected = _selectedVariableName == variableName;
				button.SetPressedNoSignal(isSelected);
				UpdateVariableNameButtonAppearance(button, isSelected);
			}

			RefreshVariableSelectionVisualsRecursive(child);
		}
	}

	private Control CreateValueEditor(ForgeSharedVariableDefinition def)
	{
		if (def.VariableType == StatescriptVariableType.Entity)
		{
			var info = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
			info.AddChild(new Label { Text = "Runtime-assigned entity reference." });
			return info;
		}

		if (def.VariableType == StatescriptVariableType.Bool)
		{
			var hBox = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };

			hBox.AddChild(StatescriptEditorControls.CreateBoolEditor(
				def.InitialValue.AsBool(),
				x => SetVariableValue(def, Variant.From(x))));

			return hBox;
		}

		if (StatescriptEditorControls.IsIntegerType(def.VariableType)
			|| StatescriptEditorControls.IsFloatType(def.VariableType))
		{
			var hBox = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };

			EditorSpinSlider spin = StatescriptEditorControls.CreateNumericSpinSlider(
				def.VariableType,
				def.InitialValue.AsDouble(),
				onChanged: x =>
				{
					Variant newValue = StatescriptEditorControls.IsIntegerType(def.VariableType)
						? Variant.From((long)x)
						: Variant.From(x);
					SetVariableValue(def, newValue);
				});

			hBox.AddChild(spin);
			return hBox;
		}

		if (StatescriptEditorControls.IsVectorType(def.VariableType))
		{
			return StatescriptEditorControls.CreateVectorEditor(
				def.VariableType,
				x => StatescriptEditorControls.GetVectorComponent(
					def.InitialValue,
					def.VariableType,
					x),
				onChanged: x =>
				{
					Variant newValue = StatescriptEditorControls.BuildVectorVariant(
						def.VariableType,
						x);
					SetVariableValue(def, newValue);
				});
		}

		var fallback = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		fallback.AddChild(new Label { Text = def.VariableType.ToString() });
		return fallback;
	}

	private VBoxContainer CreateArrayValueEditor(ForgeSharedVariableDefinition def)
	{
		var vBox = new VBoxContainer
		{
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
		};

		var headerRow = new HBoxContainer();
		vBox.AddChild(headerRow);

		bool isExpanded = _expandedArrays.Contains(def.VariableName);

		var elementsContainer = new VBoxContainer
		{
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			Visible = isExpanded,
		};

		var toggleButton = new Button
		{
			Text = $"Array (size {def.InitialArrayValues.Count})",
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
				_expandedArrays.Add(def.VariableName);
			}
			else
			{
				_expandedArrays.Remove(def.VariableName);
			}

			if (_undoRedo is not null)
			{
				_undoRedo.CreateAction("Toggle Array Expand");
				_undoRedo.AddDoMethod(
					this,
					MethodName.DoSetArrayExpanded,
					def.VariableName,
					x);
				_undoRedo.AddUndoMethod(
					this,
					MethodName.DoSetArrayExpanded,
					def.VariableName,
					wasExpanded);
				_undoRedo.CommitAction(false);
			}
		};

		headerRow.AddChild(toggleButton);

		if (def.VariableType == StatescriptVariableType.Entity)
		{
			vBox.AddChild(new Label { Text = "Runtime-assigned entity references." });
			return vBox;
		}

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
				StatescriptVariableTypeConverter.CreateDefaultGodotVariant(def.VariableType);
			AddArrayElement(def, defaultValue);
		};

		headerRow.AddChild(addElementButton);

		vBox.AddChild(elementsContainer);

		for (int i = 0; i < def.InitialArrayValues.Count; i++)
		{
			int capturedIndex = i;

			if (def.VariableType == StatescriptVariableType.Bool)
			{
				var elementRow = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
				elementsContainer.AddChild(elementRow);
				elementRow.AddChild(new Label { Text = $"[{i}]" });

				elementRow.AddChild(StatescriptEditorControls.CreateBoolEditor(
					def.InitialArrayValues[i].AsBool(),
					x => SetArrayElementValue(def, capturedIndex, Variant.From(x))));

				AddArrayElementRemoveButton(elementRow, def, capturedIndex);
			}
			else if (StatescriptEditorControls.IsVectorType(def.VariableType))
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

				AddArrayElementRemoveButton(labelRow, def, capturedIndex);

				VBoxContainer vectorEditor = StatescriptEditorControls.CreateVectorEditor(
					def.VariableType,
					x => StatescriptEditorControls.GetVectorComponent(
						def.InitialArrayValues[capturedIndex],
						def.VariableType,
						x),
					x =>
					{
						Variant newValue = StatescriptEditorControls.BuildVectorVariant(
							def.VariableType,
							x);
						SetArrayElementValue(def, capturedIndex, newValue);
					});

				elementVBox.AddChild(vectorEditor);
			}
			else
			{
				var elementRow = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
				elementsContainer.AddChild(elementRow);
				elementRow.AddChild(new Label { Text = $"[{i}]" });

				EditorSpinSlider elementSpin = StatescriptEditorControls.CreateNumericSpinSlider(
					def.VariableType,
					def.InitialArrayValues[i].AsDouble(),
					onChanged: x =>
					{
						Variant newValue = StatescriptEditorControls.IsIntegerType(def.VariableType)
							? Variant.From((long)x)
							: Variant.From(x);
						SetArrayElementValue(def, capturedIndex, newValue);
					});

				elementRow.AddChild(elementSpin);
				AddArrayElementRemoveButton(elementRow, def, capturedIndex);
			}
		}

		return vBox;
	}

	private void AddArrayElementRemoveButton(
		HBoxContainer row,
		ForgeSharedVariableDefinition def,
		int elementIndex)
	{
		var removeElementButton = new Button
		{
			Icon = _removeIcon,
			Flat = true,
			CustomMinimumSize = new Vector2(24, 24),
		};

		removeElementButton.Pressed += () => RemoveArrayElement(def, elementIndex);

		row.AddChild(removeElementButton);
	}

	private void SetVariableValue(ForgeSharedVariableDefinition def, Variant newValue)
	{
		Variant oldValue = def.InitialValue;

		def.InitialValue = newValue;
		NotifyChanged();

		if (_undoRedo is not null)
		{
			_undoRedo.CreateAction($"Change Shared Variable '{def.VariableName}'");
			_undoRedo.AddDoMethod(this, MethodName.ApplyVariableValue, def, newValue);
			_undoRedo.AddUndoMethod(this, MethodName.ApplyVariableValue, def, oldValue);
			_undoRedo.CommitAction(false);
		}
	}

	private void SetArrayElementValue(ForgeSharedVariableDefinition def, int index, Variant newValue)
	{
		Variant oldValue = def.InitialArrayValues[index];

		def.InitialArrayValues[index] = newValue;
		NotifyChanged();

		if (_undoRedo is not null)
		{
			_undoRedo.CreateAction($"Change Shared Variable '{def.VariableName}' Element [{index}]");
			_undoRedo.AddDoMethod(this, MethodName.ApplyArrayElementValue, def, index, newValue);
			_undoRedo.AddUndoMethod(this, MethodName.ApplyArrayElementValue, def, index, oldValue);
			_undoRedo.CommitAction(false);
		}
	}

	private void AddArrayElement(ForgeSharedVariableDefinition def, Variant value)
	{
		bool wasExpanded = _expandedArrays.Contains(def.VariableName);

		if (_undoRedo is not null)
		{
			_undoRedo.CreateAction($"Add Element to '{def.VariableName}'");
			_undoRedo.AddDoMethod(this, MethodName.DoAddArrayElement, def, value);
			_undoRedo.AddUndoMethod(this, MethodName.UndoAddArrayElement, def, wasExpanded);
			_undoRedo.CommitAction();
		}
		else
		{
			DoAddArrayElement(def, value);
		}
	}

	private void RemoveArrayElement(ForgeSharedVariableDefinition def, int index)
	{
		if (index < 0 || index >= def.InitialArrayValues.Count)
		{
			return;
		}

		Variant oldValue = def.InitialArrayValues[index];

		if (_undoRedo is not null)
		{
			_undoRedo.CreateAction($"Remove Element [{index}] from '{def.VariableName}'");
			_undoRedo.AddDoMethod(this, MethodName.DoRemoveArrayElement, def, index);
			_undoRedo.AddUndoMethod(this, MethodName.UndoRemoveArrayElement, def, index, oldValue);
			_undoRedo.CommitAction();
		}
		else
		{
			DoRemoveArrayElement(def, index);
		}
	}

	private void OnAddPressed()
	{
		ShowCreationDialog();
	}

	private void ShowCreationDialog()
	{
		_creationDialog?.QueueFree();

		_creationDialog = new AcceptDialog
		{
			Title = "Add Shared Variable",
			Size = new Vector2I(300, 130),
			Exclusive = true,
		};

		var vBox = new VBoxContainer();
		_creationDialog.AddChild(vBox);

		var nameRow = new HBoxContainer();
		vBox.AddChild(nameRow);
		nameRow.AddChild(new Label { Text = "Name:", CustomMinimumSize = new Vector2(60, 0) });

		_newNameEdit = new LineEdit
		{
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			PlaceholderText = "variable name",
		};

		nameRow.AddChild(_newNameEdit);

		var typeRow = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		typeRow.AddThemeConstantOverride("separation", 5);
		vBox.AddChild(typeRow);
		typeRow.AddChild(new Label { Text = "Type:", CustomMinimumSize = new Vector2(60, 0) });

		_newTypeDropdown = StatescriptEditorControls.CreateVariableTypeDropdown(StatescriptVariableType.Int);

		typeRow.AddChild(_newTypeDropdown);
		_newValueShapeDropdown = StatescriptEditorControls.CreateValueShapeDropdown(false);
		typeRow.AddChild(_newValueShapeDropdown);

		_creationDialog.Confirmed += OnCreationConfirmed;
		_creationDialog.Canceled += OnCreationCanceled;

		EditorInterface.Singleton.PopupDialogCentered(_creationDialog);
	}

	private void OnCreationConfirmed()
	{
		if (_newNameEdit is null || _newTypeDropdown is null || _newValueShapeDropdown is null)
		{
			return;
		}

		string name = _newNameEdit.Text.Trim();

		if (string.IsNullOrEmpty(name))
		{
			return;
		}

		var variableType = (StatescriptVariableType)_newTypeDropdown.GetItemId(_newTypeDropdown.Selected);

		var newDef = new ForgeSharedVariableDefinition
		{
			VariableName = name,
			VariableType = variableType,
			IsArray = _newValueShapeDropdown.GetSelectedId() == 1,
			InitialValue = StatescriptVariableTypeConverter.CreateDefaultGodotVariant(variableType),
		};

		Array<ForgeSharedVariableDefinition> definitions = GetDefinitions();

		if (_undoRedo is not null)
		{
			_undoRedo.CreateAction("Add Shared Variable");
			_undoRedo.AddDoMethod(this, MethodName.DoAddVariable, definitions, newDef);
			_undoRedo.AddUndoMethod(this, MethodName.UndoAddVariable, definitions, newDef);
			_undoRedo.CommitAction();
		}
		else
		{
			DoAddVariable(definitions, newDef);
		}

		CleanupCreationDialog();
	}

	private void OnCreationCanceled()
	{
		CleanupCreationDialog();
	}

	private void CleanupCreationDialog()
	{
		_creationDialog?.QueueFree();
		_creationDialog = null;
		_newNameEdit = null;
		_newTypeDropdown = null;
		_newValueShapeDropdown = null;
	}

	private void OnDeletePressed(int index)
	{
		Array<ForgeSharedVariableDefinition> definitions = GetDefinitions();

		if (index < 0 || index >= definitions.Count)
		{
			return;
		}

		ForgeSharedVariableDefinition variable = definitions[index];

		if (_undoRedo is not null)
		{
			_undoRedo.CreateAction("Remove Shared Variable");
			_undoRedo.AddDoMethod(this, MethodName.DoRemoveVariable, definitions, variable, index);
			_undoRedo.AddUndoMethod(this, MethodName.UndoRemoveVariable, definitions, variable, index);
			_undoRedo.CommitAction();
		}
		else
		{
			DoRemoveVariable(definitions, index);
		}
	}

	private void ApplyVariableValue(ForgeSharedVariableDefinition def, Variant value)
	{
		def.InitialValue = value;
		NotifyChanged();
		RebuildList();
	}

	private void ApplyArrayElementValue(ForgeSharedVariableDefinition def, int index, Variant value)
	{
		def.InitialArrayValues[index] = value;
		NotifyChanged();
		RebuildList();
	}

	private void DoAddVariable(Array<ForgeSharedVariableDefinition> definitions, ForgeSharedVariableDefinition def)
	{
		definitions.Add(def);
		NotifyChanged();
		RebuildList();
	}

	private void UndoAddVariable(Array<ForgeSharedVariableDefinition> definitions, ForgeSharedVariableDefinition def)
	{
		definitions.Remove(def);
		NotifyChanged();
		RebuildList();
	}

	private void DoRemoveVariable(
		Array<ForgeSharedVariableDefinition> definitions,
		int index)
	{
		definitions.RemoveAt(index);
		NotifyChanged();
		RebuildList();
	}

	private void UndoRemoveVariable(
		Array<ForgeSharedVariableDefinition> definitions,
		ForgeSharedVariableDefinition sharedVariableDefinition,
		int index)
	{
		if (index >= definitions.Count)
		{
			definitions.Add(sharedVariableDefinition);
		}
		else
		{
			definitions.Insert(index, sharedVariableDefinition);
		}

		NotifyChanged();
		RebuildList();
	}

	private void DoAddArrayElement(ForgeSharedVariableDefinition sharedVariableDefinition, Variant value)
	{
		sharedVariableDefinition.InitialArrayValues.Add(value);
		_expandedArrays.Add(sharedVariableDefinition.VariableName);
		NotifyChanged();
		RebuildList();
	}

	private void UndoAddArrayElement(ForgeSharedVariableDefinition sharedVariableDefinition, bool wasExpanded)
	{
		if (sharedVariableDefinition.InitialArrayValues.Count > 0)
		{
			sharedVariableDefinition.InitialArrayValues.RemoveAt(sharedVariableDefinition.InitialArrayValues.Count - 1);
		}

		if (!wasExpanded)
		{
			_expandedArrays.Remove(sharedVariableDefinition.VariableName);
		}

		NotifyChanged();
		RebuildList();
	}

	private void DoRemoveArrayElement(ForgeSharedVariableDefinition sharedVariableDefinition, int index)
	{
		sharedVariableDefinition.InitialArrayValues.RemoveAt(index);
		NotifyChanged();
		RebuildList();
	}

	private void UndoRemoveArrayElement(
		ForgeSharedVariableDefinition sharedVariableDefinition,
		int index,
		Variant value)
	{
		if (index >= sharedVariableDefinition.InitialArrayValues.Count)
		{
			sharedVariableDefinition.InitialArrayValues.Add(value);
		}
		else
		{
			sharedVariableDefinition.InitialArrayValues.Insert(index, value);
		}

		NotifyChanged();
		RebuildList();
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

		RebuildList();
	}
}
#endif
