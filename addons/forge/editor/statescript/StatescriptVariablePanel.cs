// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Statescript;

/// <summary>
/// Right-side panel for editing graph variables. Lists all variables in the current graph and allows adding, removing,
/// renaming, changing types, and setting initial values.
/// </summary>
[Tool]
internal sealed partial class StatescriptVariablePanel : VBoxContainer
{
	private StatescriptGraph? _graph;
	private VBoxContainer? _variableList;
	private Button? _addButton;

	/// <summary>
	/// Raised when any variable is added, removed, renamed, or its type changes.
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

		// Row 1: Name + Delete button.
		var nameRow = new HBoxContainer
		{
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
		};

		rowContainer.AddChild(nameRow);

		var nameEdit = new LineEdit
		{
			Text = variable.VariableName,
			PlaceholderText = "Variable Name",
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			CustomMinimumSize = new Vector2(100, 0),
		};

		var capturedIndex = index;
		nameEdit.TextSubmitted += x => OnNameChanged(capturedIndex, x, nameEdit);
		nameEdit.FocusExited += () => OnNameChanged(capturedIndex, nameEdit.Text, nameEdit);
		nameRow.AddChild(nameEdit);

		var deleteButton = new Button
		{
			Text = "✕",
			TooltipText = "Remove Variable",
			CustomMinimumSize = new Vector2(28, 28),
		};

		deleteButton.Pressed += () => OnDeletePressed(capturedIndex);
		nameRow.AddChild(deleteButton);

		// Row 2: Type dropdown + Array toggle.
		var typeRow = new HBoxContainer
		{
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
		};

		rowContainer.AddChild(typeRow);

		var typeDropdown = new OptionButton
		{
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
		};

		StatescriptVariableType[] allTypes = StatescriptVariableTypeConverter.GetAllTypes();
		for (var t = 0; t < allTypes.Length; t++)
		{
			typeDropdown.AddItem(StatescriptVariableTypeConverter.GetDisplayName(allTypes[t]), t);
		}

		typeDropdown.Selected = (int)variable.VariableType;
		typeDropdown.ItemSelected += x => OnTypeChanged(capturedIndex, (StatescriptVariableType)(int)x);
		typeRow.AddChild(typeDropdown);

		var arrayToggle = new CheckButton
		{
			Text = "Array",
			ButtonPressed = variable.IsArray,
		};

		arrayToggle.Toggled += x => OnArrayToggled(capturedIndex, x);
		typeRow.AddChild(arrayToggle);

		// Row 3: Initial value editor.
		if (!variable.IsArray)
		{
			Control? valueEditor = CreateScalarValueEditor(variable);
			if (valueEditor is not null)
			{
				rowContainer.AddChild(valueEditor);
			}
		}
		else
		{
			Control arrayEditor = CreateArrayValueEditor(variable);
			rowContainer.AddChild(arrayEditor);
		}

		// Separator between variables.
		var separator = new HSeparator();
		rowContainer.AddChild(separator);
	}

	private Control? CreateScalarValueEditor(StatescriptGraphVariable variable)
	{
		var hBox = new HBoxContainer
		{
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
		};

		var label = new Label { Text = "Value:" };
		hBox.AddChild(label);

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

	private Control CreateArrayValueEditor(StatescriptGraphVariable variable)
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

	private static bool IsIntegerType(StatescriptVariableType type)
	{
		return type is StatescriptVariableType.Int or StatescriptVariableType.UInt
			or StatescriptVariableType.Long or StatescriptVariableType.ULong
			or StatescriptVariableType.Short or StatescriptVariableType.UShort
			or StatescriptVariableType.Byte or StatescriptVariableType.SByte
			or StatescriptVariableType.Char;
	}

	private void OnAddPressed()
	{
		if (_graph is null)
		{
			return;
		}

		var newVariable = new StatescriptGraphVariable
		{
			VariableName = GenerateUniqueName(),
			VariableType = StatescriptVariableType.Int,
			InitialValue = Variant.From(0),
		};

		_graph.Variables.Add(newVariable);
		RebuildList();
		VariablesChanged?.Invoke();
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

	private void OnNameChanged(int index, string newName, LineEdit _)
	{
		if (_graph is null || index < 0 || index >= _graph.Variables.Count)
		{
			return;
		}

		var oldName = _graph.Variables[index].VariableName;
		if (oldName == newName)
		{
			return;
		}

		_graph.Variables[index].VariableName = newName;

		// Update all node property bindings that reference the old name.
		UpdateReferencesToVariable(oldName, newName);

		VariablesChanged?.Invoke();
	}

	private void OnTypeChanged(int index, StatescriptVariableType newType)
	{
		if (_graph is null || index < 0 || index >= _graph.Variables.Count)
		{
			return;
		}

		StatescriptGraphVariable variable = _graph.Variables[index];
		StatescriptVariableType oldType = variable.VariableType;

		if (oldType == newType)
		{
			return;
		}

		variable.VariableType = newType;
		variable.InitialValue = StatescriptVariableTypeConverter.CreateDefaultGodotVariant(newType);
		variable.InitialArrayValues.Clear();

		// Clear bindings that are no longer type-compatible.
		ClearIncompatibleReferences(variable.VariableName, newType);

		RebuildList();
		VariablesChanged?.Invoke();
	}

	private void OnArrayToggled(int index, bool isArray)
	{
		if (_graph is null || index < 0 || index >= _graph.Variables.Count)
		{
			return;
		}

		StatescriptGraphVariable variable = _graph.Variables[index];
		variable.IsArray = isArray;

		if (isArray)
		{
			variable.InitialArrayValues.Clear();
		}
		else
		{
			variable.InitialValue = StatescriptVariableTypeConverter.CreateDefaultGodotVariant(variable.VariableType);
		}

		RebuildList();
		VariablesChanged?.Invoke();
	}

	private void UpdateReferencesToVariable(string oldName, string newName)
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
					&& varRes.VariableName == oldName)
				{
					varRes.VariableName = newName;
				}
			}
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

	private void ClearIncompatibleReferences(string variableName, StatescriptVariableType newType)
	{
		if (_graph is null)
		{
			return;
		}

		foreach (StatescriptNode node in _graph.Nodes)
		{
			if (string.IsNullOrEmpty(node.RuntimeTypeName))
			{
				continue;
			}

			StatescriptNodeDiscovery.NodeTypeInfo? typeInfo =
				StatescriptNodeDiscovery.FindByRuntimeTypeName(node.RuntimeTypeName);

			if (typeInfo is null)
			{
				continue;
			}

			foreach (StatescriptNodeProperty binding in node.PropertyBindings)
			{
				if (binding.Resolver is not VariableResolverResource varRes
					|| varRes.VariableName != variableName)
				{
					continue;
				}

				// Check type compatibility based on binding direction.
				Type expectedType;
				if (binding.Direction == StatescriptPropertyDirection.Input
					&& binding.PropertyIndex < typeInfo.InputPropertiesInfo.Length)
				{
					expectedType = typeInfo.InputPropertiesInfo[binding.PropertyIndex].ExpectedType;
				}
				else if (binding.Direction == StatescriptPropertyDirection.Output
					&& binding.PropertyIndex < typeInfo.OutputVariablesInfo.Length)
				{
					expectedType = typeInfo.OutputVariablesInfo[binding.PropertyIndex].ValueType;
				}
				else
				{
					continue;
				}

				if (!StatescriptVariableTypeConverter.IsCompatible(expectedType, newType))
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
