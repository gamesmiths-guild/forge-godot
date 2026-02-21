// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Statescript;

/// <summary>
/// Right-side panel for editing graph variables. Variables are created with a name and type via a creation dialog.
/// Once created, only the initial value can be edited. To change name or type, delete and recreate the variable.
/// </summary>
[Tool]
internal sealed partial class StatescriptVariablePanel : VBoxContainer
{
	private StatescriptGraph? _graph;
	private VBoxContainer? _variableList;
	private Button? _addButton;

	// Creation dialog controls.
	private Window? _creationDialog;
	private LineEdit? _newNameEdit;
	private OptionButton? _newTypeDropdown;
	private CheckButton? _newArrayToggle;

	/// <summary>
	/// Raised when any variable is added, removed, or its value changes.
	/// </summary>
	public event Action? VariablesChanged;

	public override void _Ready()
	{
		base._Ready();

		SizeFlagsVertical = SizeFlags.ExpandFill;
		CustomMinimumSize = new Vector2(250, 0);

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
			Text = "+",
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

	/// <summary>
	/// Sets the graph to display variables for.
	/// </summary>
	/// <param name="graph">The graph resource, or null to clear.</param>
	public void SetGraph(StatescriptGraph? graph)
	{
		_graph = graph;
		RebuildList();
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

	private static HBoxContainer? CreateScalarValueEditor(StatescriptGraphVariable variable)
	{
		var hBox = new HBoxContainer
		{
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
		};

		hBox.AddChild(new Label { Text = "Value:" });

		switch (variable.VariableType)
		{
			case StatescriptVariableType.Bool:
				var checkBox = new CheckButton { ButtonPressed = variable.InitialValue.AsBool() };
				checkBox.Toggled += x =>
				{
					variable.InitialValue = Variant.From(x);
				};

				hBox.AddChild(checkBox);
				break;

			case StatescriptVariableType.Int:
			case StatescriptVariableType.Short:
			case StatescriptVariableType.Byte:
			case StatescriptVariableType.SByte:
			case StatescriptVariableType.UShort:
			case StatescriptVariableType.Char:
				var intSpin = new SpinBox
				{
					Value = variable.InitialValue.AsInt32(),
					Step = 1,
					Rounded = true,
					SizeFlagsHorizontal = SizeFlags.ExpandFill,
				};

				intSpin.ValueChanged += value =>
				{
					variable.InitialValue = Variant.From((int)value);
				};

				hBox.AddChild(intSpin);
				break;

			case StatescriptVariableType.UInt:
			case StatescriptVariableType.Long:
			case StatescriptVariableType.ULong:
				var longSpin = new SpinBox
				{
					Value = variable.InitialValue.AsInt64(),
					Step = 1,
					Rounded = true,
					SizeFlagsHorizontal = SizeFlags.ExpandFill,
				};

				longSpin.ValueChanged += value =>
				{
					variable.InitialValue = Variant.From((long)value);
				};

				hBox.AddChild(longSpin);
				break;

			case StatescriptVariableType.Float:
			case StatescriptVariableType.Double:
			case StatescriptVariableType.Decimal:
				var floatSpin = new SpinBox
				{
					Value = variable.InitialValue.AsDouble(),
					Step = 0.01,
					Rounded = false,
					SizeFlagsHorizontal = SizeFlags.ExpandFill,
				};

				floatSpin.ValueChanged += value =>
				{
					variable.InitialValue = Variant.From(value);
				};

				hBox.AddChild(floatSpin);
				break;

			case StatescriptVariableType.Vector2:
				CreateVectorSpins(hBox, variable, 2);
				break;

			case StatescriptVariableType.Vector3:
				CreateVectorSpins(hBox, variable, 3);
				break;

			case StatescriptVariableType.Vector4:
			case StatescriptVariableType.Plane:
			case StatescriptVariableType.Quaternion:
				CreateVectorSpins(hBox, variable, 4);
				break;

			default:
				hBox.AddChild(new Label { Text = variable.VariableType.ToString() });
				break;
		}

		return hBox;
	}

	private static void CreateVectorSpins(HBoxContainer parent, StatescriptGraphVariable variable, int components)
	{
		string[] labels = components switch
		{
			2 => ["X", "Y"],
			3 => ["X", "Y", "Z"],
			_ => ["X", "Y", "Z", "W"],
		};

		var spins = new SpinBox[components];

		for (var i = 0; i < components; i++)
		{
			parent.AddChild(new Label { Text = labels[i] });

			var spin = new SpinBox
			{
				Value = GetVectorComponent(variable, i),
				Step = 0.01,
				Rounded = false,
				SizeFlagsHorizontal = SizeFlags.ExpandFill,
				CustomMinimumSize = new Vector2(50, 0),
			};

			spins[i] = spin;
			spin.ValueChanged += _ => UpdateVectorValue(variable, spins);
			parent.AddChild(spin);
		}
	}

	private static double GetVectorComponent(StatescriptGraphVariable variable, int index)
	{
		return variable.VariableType switch
		{
			StatescriptVariableType.Vector2 => index == 0
				? variable.InitialValue.AsVector2().X
				: variable.InitialValue.AsVector2().Y,
			StatescriptVariableType.Vector3 => index switch
			{
				0 => variable.InitialValue.AsVector3().X,
				1 => variable.InitialValue.AsVector3().Y,
				_ => variable.InitialValue.AsVector3().Z,
			},
			StatescriptVariableType.Bool => throw new NotImplementedException(),
			StatescriptVariableType.Byte => throw new NotImplementedException(),
			StatescriptVariableType.SByte => throw new NotImplementedException(),
			StatescriptVariableType.Char => throw new NotImplementedException(),
			StatescriptVariableType.Decimal => throw new NotImplementedException(),
			StatescriptVariableType.Double => throw new NotImplementedException(),
			StatescriptVariableType.Float => throw new NotImplementedException(),
			StatescriptVariableType.Int => throw new NotImplementedException(),
			StatescriptVariableType.UInt => throw new NotImplementedException(),
			StatescriptVariableType.Long => throw new NotImplementedException(),
			StatescriptVariableType.ULong => throw new NotImplementedException(),
			StatescriptVariableType.Short => throw new NotImplementedException(),
			StatescriptVariableType.UShort => throw new NotImplementedException(),
			StatescriptVariableType.Vector4 => throw new NotImplementedException(),
			StatescriptVariableType.Plane => throw new NotImplementedException(),
			StatescriptVariableType.Quaternion => throw new NotImplementedException(),
			_ => index switch
			{
				0 => variable.InitialValue.AsVector4().X,
				1 => variable.InitialValue.AsVector4().Y,
				2 => variable.InitialValue.AsVector4().Z,
				_ => variable.InitialValue.AsVector4().W,
			},
		};
	}

	private static void UpdateVectorValue(StatescriptGraphVariable variable, SpinBox[] spins)
	{
		variable.InitialValue = variable.VariableType switch
		{
			StatescriptVariableType.Vector2 => Variant.From(
				new Vector2(
					(float)spins[0].Value,
					(float)spins[1].Value)),
			StatescriptVariableType.Vector3 => Variant.From(
				new Vector3(
					(float)spins[0].Value,
					(float)spins[1].Value,
					(float)spins[2].Value)),
			StatescriptVariableType.Vector4 => Variant.From(
				new Vector4(
					(float)spins[0].Value,
					(float)spins[1].Value,
					(float)spins[2].Value,
					(float)spins[3].Value)),
			StatescriptVariableType.Plane => Variant.From(
				new Plane(
					new Vector3(
						(float)spins[0].Value,
						(float)spins[1].Value,
						(float)spins[2].Value),
					(float)spins[3].Value)),
			StatescriptVariableType.Quaternion => Variant.From(
				new Quaternion(
					(float)spins[0].Value,
					(float)spins[1].Value,
					(float)spins[2].Value,
					(float)spins[3].Value)),
			StatescriptVariableType.Bool => throw new NotImplementedException(),
			StatescriptVariableType.Byte => throw new NotImplementedException(),
			StatescriptVariableType.SByte => throw new NotImplementedException(),
			StatescriptVariableType.Char => throw new NotImplementedException(),
			StatescriptVariableType.Decimal => throw new NotImplementedException(),
			StatescriptVariableType.Double => throw new NotImplementedException(),
			StatescriptVariableType.Float => throw new NotImplementedException(),
			StatescriptVariableType.Int => throw new NotImplementedException(),
			StatescriptVariableType.UInt => throw new NotImplementedException(),
			StatescriptVariableType.Long => throw new NotImplementedException(),
			StatescriptVariableType.ULong => throw new NotImplementedException(),
			StatescriptVariableType.Short => throw new NotImplementedException(),
			StatescriptVariableType.UShort => throw new NotImplementedException(),
			_ => variable.InitialValue,
		};
	}

	private static bool IsIntegerType(StatescriptVariableType type)
	{
		return type is StatescriptVariableType.Int or StatescriptVariableType.UInt
			or StatescriptVariableType.Long or StatescriptVariableType.ULong
			or StatescriptVariableType.Short or StatescriptVariableType.UShort
			or StatescriptVariableType.Byte or StatescriptVariableType.SByte
			or StatescriptVariableType.Char;
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

		// Row 1: Name label (read-only) + Type label + Delete button.
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
			Text = "✕",
			TooltipText = "Remove Variable",
			CustomMinimumSize = new Vector2(28, 28),
		};

		deleteButton.Pressed += () => OnDeletePressed(capturedIndex);
		headerRow.AddChild(deleteButton);

		// Row 2: Initial value editor.
		if (!variable.IsArray)
		{
			HBoxContainer? valueEditor = CreateScalarValueEditor(variable);

			if (valueEditor is not null)
			{
				rowContainer.AddChild(valueEditor);
			}
		}
		else
		{
			VBoxContainer arrayEditor = CreateArrayValueEditor(variable);
			rowContainer.AddChild(arrayEditor);
		}

		// Separator between variables.
		rowContainer.AddChild(new HSeparator());
	}

	private VBoxContainer CreateArrayValueEditor(StatescriptGraphVariable variable)
	{
		var vBox = new VBoxContainer
		{
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
		};

		var headerRow = new HBoxContainer();
		vBox.AddChild(headerRow);

		headerRow.AddChild(new Label { Text = $"Array ({variable.InitialArrayValues.Count} elements)" });

		var addElementButton = new Button
		{
			Text = "+",
			TooltipText = "Add Element",
			CustomMinimumSize = new Vector2(24, 24),
		};

		addElementButton.Pressed += () =>
		{
			variable.InitialArrayValues.Add(
				StatescriptVariableTypeConverter.CreateDefaultGodotVariant(variable.VariableType));
			RebuildList();
		};

		headerRow.AddChild(addElementButton);

		for (var i = 0; i < variable.InitialArrayValues.Count; i++)
		{
			var elementRow = new HBoxContainer
			{
				SizeFlagsHorizontal = SizeFlags.ExpandFill,
			};

			vBox.AddChild(elementRow);

			elementRow.AddChild(new Label { Text = $"[{i}]" });

			var capturedIndex = i;
			var elementSpin = new SpinBox
			{
				Value = variable.InitialArrayValues[i].AsDouble(),
				Step = IsIntegerType(variable.VariableType) ? 1 : 0.01,
				Rounded = IsIntegerType(variable.VariableType),
				SizeFlagsHorizontal = SizeFlags.ExpandFill,
				CustomMinimumSize = new Vector2(60, 0),
			};

			elementSpin.ValueChanged += value =>
			{
				if (IsIntegerType(variable.VariableType))
				{
					variable.InitialArrayValues[capturedIndex] = Variant.From((int)value);
				}
				else
				{
					variable.InitialArrayValues[capturedIndex] = Variant.From(value);
				}
			};

			elementRow.AddChild(elementSpin);

			var removeElementButton = new Button
			{
				Text = "✕",
				CustomMinimumSize = new Vector2(24, 24),
			};

			removeElementButton.Pressed += () =>
			{
				variable.InitialArrayValues.RemoveAt(capturedIndex);
				RebuildList();
			};

			elementRow.AddChild(removeElementButton);
		}

		return vBox;
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

		// Name row.
		var nameRow = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		vBox.AddChild(nameRow);

		nameRow.AddChild(new Label { Text = "Name:", CustomMinimumSize = new Vector2(60, 0) });

		_newNameEdit = new LineEdit
		{
			Text = GenerateUniqueName(),
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
		};

		nameRow.AddChild(_newNameEdit);

		// Type row.
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

		// Array toggle.
		var arrayRow = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		vBox.AddChild(arrayRow);

		arrayRow.AddChild(new Label { Text = "Array:", CustomMinimumSize = new Vector2(60, 0) });

		_newArrayToggle = new CheckButton();
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

		_graph.Variables.Add(newVariable);
		RebuildList();
		VariablesChanged?.Invoke();

		_creationDialog?.QueueFree();
		_creationDialog = null;
	}

	private void OnDeletePressed(int index)
	{
		if (_graph is null || index < 0 || index >= _graph.Variables.Count)
		{
			return;
		}

		var deletedName = _graph.Variables[index].VariableName;
		_graph.Variables.RemoveAt(index);

		// Clear any node property bindings that referenced the deleted variable.
		ClearReferencesToVariable(deletedName);

		RebuildList();
		VariablesChanged?.Invoke();
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
			return "Variable";
		}

		const string baseName = "Variable";
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
}
#endif
