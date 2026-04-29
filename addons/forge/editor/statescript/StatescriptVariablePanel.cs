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
	private const string VariableNameButtonMetaKey = "_variable_name_button";

	private static readonly Color _variableColor = new(0xe5c07bff);
	private static readonly Color _highlightColor = new(0x56b6c2ff);

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

	private string? _selectedVariableName;

	/// <summary>
	/// Raised when any variable is added, removed, or its value changes.
	/// </summary>
	public event Action? VariablesChanged;

	/// <summary>
	/// Raised when an undo/redo action modifies the variable panel, so the dock can auto-expand it.
	/// </summary>
	public event Action? VariableUndoRedoPerformed;

	/// <summary>
	/// Raised when the user selects or deselects a variable for highlighting.
	/// </summary>
	public event Action<string?>? VariableHighlightChanged;

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

		if (_addButton is not null)
		{
			_addButton.Pressed -= OnAddPressed;
		}

		_creationDialog?.QueueFree();
		_creationDialog = null;
		_newNameEdit = null;
		_newTypeDropdown = null;
		_newArrayToggle = null;
	}

	public void OnBeforeSerialize()
	{
		if (_addButton is not null)
		{
			_addButton.Pressed -= OnAddPressed;
		}

		if (_variableList is not null)
		{
			foreach (Node child in _variableList.GetChildren())
			{
				_variableList.RemoveChild(child);
				child.Free();
			}
		}

		_creationDialog?.Free();
		_creationDialog = null;
		_newNameEdit = null;
		_newTypeDropdown = null;
		_newArrayToggle = null;
	}

	public void OnAfterDeserialize()
	{
		if (_addButton is not null)
		{
			_addButton.Pressed += OnAddPressed;
		}

		RebuildList();
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
			_variableList.RemoveChild(child);
			child.Free();
		}

		if (_graph is null)
		{
			return;
		}

		for (int i = 0; i < _graph.Variables.Count; i++)
		{
			AddVariableRow(_graph.Variables[i], i);
		}
	}

	private static void UpdateVariableNameButtonAppearance(Button button, bool isSelected)
	{
		Color buttonColor = isSelected ? _highlightColor : _variableColor;
		button.AddThemeColorOverride("font_color", buttonColor);
		button.AddThemeColorOverride("font_pressed_color", _highlightColor);
		button.AddThemeColorOverride("font_hover_color", buttonColor.Lightened(0.2f));
		button.AddThemeColorOverride("font_hover_pressed_color", _highlightColor.Lightened(0.2f));
	}

	private void SaveExpandedArrayState()
	{
		if (_graph is null)
		{
			return;
		}

		string[] packed = new string[_expandedArrays.Count];
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
			foreach (string name in meta.AsStringArray())
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

		bool isSelected = _selectedVariableName == variable.VariableName;

		var nameButton = new Button
		{
			Text = variable.VariableName,
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			Flat = true,
			ToggleMode = true,
			ButtonPressed = isSelected,
			Alignment = HorizontalAlignment.Left,
		};

		nameButton.SetMeta(VariableNameButtonMetaKey, variable.VariableName);
		UpdateVariableNameButtonAppearance(nameButton, isSelected);
		nameButton.AddThemeFontOverride(
			"font",
			EditorInterface.Singleton.GetEditorTheme().GetFont("bold", "EditorFonts"));

		nameButton.Toggled += pressed =>
		{
			SetSelectedVariable(variable.VariableName, pressed);
		};

		headerRow.AddChild(nameButton);

		var typeLabel = new Label
		{
			Text = $"({StatescriptVariableTypeConverter.GetDisplayName(variable.VariableType)}"
				+ (variable.IsArray ? "[])" : ")"),
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

		RefreshVariableSelectionVisuals();
		VariableHighlightChanged?.Invoke(_selectedVariableName);
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

		for (int t = 0; t < allTypes.Length; t++)
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

		string name = _newNameEdit.Text.Trim();

		if (string.IsNullOrEmpty(name) || HasVariableNamed(name))
		{
			return;
		}

		int selectedIndex = _newTypeDropdown.Selected;
		if (selectedIndex < 0)
		{
			return;
		}

		int selectedId = _newTypeDropdown.GetItemId(selectedIndex);
		var varType = (StatescriptVariableType)selectedId;

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
			_undoRedo.AddDoMethod(this, MethodName.DoAddVariable, _graph, newVariable);
			_undoRedo.AddUndoMethod(this, MethodName.UndoAddVariable, _graph, newVariable);
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
			_undoRedo.AddDoMethod(this, MethodName.DoRemoveVariable, _graph, variable, index);
			_undoRedo.AddUndoMethod(this, MethodName.UndoRemoveVariable, _graph, variable, index);
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
		int counter = 1;
		string name = baseName;

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
}
#endif
