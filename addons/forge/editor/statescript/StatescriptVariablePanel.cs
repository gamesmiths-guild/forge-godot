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

	private Window? _creationDialog;
	private LineEdit? _newNameEdit;
	private OptionButton? _newTypeDropdown;
	private CheckBox? _newArrayToggle;

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

	private static Control CreateScalarValueEditor(StatescriptGraphVariable variable)
	{
		switch (variable.VariableType)
		{
			case StatescriptVariableType.Bool:
				{
					var hBox = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
					hBox.AddChild(new Label { Text = "Value:" });
					var checkBox = new CheckBox { ButtonPressed = variable.InitialValue.AsBool() };
					checkBox.Toggled += x =>
					{
						variable.InitialValue = Variant.From(x);
						variable.EmitChanged();
					};

					hBox.AddChild(checkBox);
					return hBox;
				}

			case StatescriptVariableType.Int:
			case StatescriptVariableType.Short:
			case StatescriptVariableType.Byte:
			case StatescriptVariableType.SByte:
			case StatescriptVariableType.UShort:
			case StatescriptVariableType.Char:
				{
					var hBox = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
					hBox.AddChild(new Label { Text = "Value:" });
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
						variable.EmitChanged();
					};

					hBox.AddChild(intSpin);
					return hBox;
				}

			case StatescriptVariableType.UInt:
			case StatescriptVariableType.Long:
			case StatescriptVariableType.ULong:
				{
					var hBox = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
					hBox.AddChild(new Label { Text = "Value:" });
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
						variable.EmitChanged();
					};

					hBox.AddChild(longSpin);
					return hBox;
				}

			case StatescriptVariableType.Float:
			case StatescriptVariableType.Double:
			case StatescriptVariableType.Decimal:
				{
					var hBox = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
					hBox.AddChild(new Label { Text = "Value:" });
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
						variable.EmitChanged();
					};

					hBox.AddChild(floatSpin);
					return hBox;
				}

			case StatescriptVariableType.Vector2:
				return CreateVectorEditor(variable, 2);

			case StatescriptVariableType.Vector3:
				return CreateVectorEditor(variable, 3);

			case StatescriptVariableType.Vector4:
			case StatescriptVariableType.Plane:
			case StatescriptVariableType.Quaternion:
				return CreateVectorEditor(variable, 4);

			default:
				{
					var hBox = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
					hBox.AddChild(new Label { Text = variable.VariableType.ToString() });
					return hBox;
				}
		}
	}

	private static VBoxContainer CreateVectorEditor(StatescriptGraphVariable variable, int components)
	{
		var vBox = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };

		vBox.AddChild(new Label { Text = "Value:" });

		string[] labels = components switch
		{
			2 => ["X", "Y"],
			3 => ["X", "Y", "Z"],
			_ => ["X", "Y", "Z", "W"],
		};

		var spins = new SpinBox[components];

		for (var i = 0; i < components; i++)
		{
			var row = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
			row.AddChild(new Label { Text = labels[i], CustomMinimumSize = new Vector2(20, 0) });

			var spin = new SpinBox
			{
				Value = GetVectorComponent(variable, i),
				Step = 0.01,
				Rounded = false,
				SizeFlagsHorizontal = SizeFlags.ExpandFill,
			};

			spins[i] = spin;
			spin.ValueChanged += _ => UpdateVectorValue(variable, spins);
			row.AddChild(spin);
			vBox.AddChild(row);
		}

		return vBox;
	}

	private static double GetVectorComponent(StatescriptGraphVariable variable, int index)
	{
		return variable.VariableType switch
		{
			StatescriptVariableType.Vector2 => index switch
			{
				0 => variable.InitialValue.AsVector2().X,
				_ => variable.InitialValue.AsVector2().Y,
			},
			StatescriptVariableType.Vector3 => index switch
			{
				0 => variable.InitialValue.AsVector3().X,
				1 => variable.InitialValue.AsVector3().Y,
				_ => variable.InitialValue.AsVector3().Z,
			},
			StatescriptVariableType.Vector4 => index switch
			{
				0 => variable.InitialValue.AsVector4().X,
				1 => variable.InitialValue.AsVector4().Y,
				2 => variable.InitialValue.AsVector4().Z,
				_ => variable.InitialValue.AsVector4().W,
			},
			StatescriptVariableType.Plane => index switch
			{
				0 => variable.InitialValue.AsPlane().Normal.X,
				1 => variable.InitialValue.AsPlane().Normal.Y,
				2 => variable.InitialValue.AsPlane().Normal.Z,
				_ => variable.InitialValue.AsPlane().D,
			},
			StatescriptVariableType.Quaternion => index switch
			{
				0 => variable.InitialValue.AsQuaternion().X,
				1 => variable.InitialValue.AsQuaternion().Y,
				2 => variable.InitialValue.AsQuaternion().Z,
				_ => variable.InitialValue.AsQuaternion().W,
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
			_ => 0,
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

		variable.EmitChanged();
	}

	private static bool IsIntegerType(StatescriptVariableType type)
	{
		return type is StatescriptVariableType.Int or StatescriptVariableType.UInt
			or StatescriptVariableType.Long or StatescriptVariableType.ULong
			or StatescriptVariableType.Short or StatescriptVariableType.UShort
			or StatescriptVariableType.Byte or StatescriptVariableType.SByte
			or StatescriptVariableType.Char;
	}

	private static bool IsFloatType(StatescriptVariableType type)
	{
		return type is StatescriptVariableType.Float or StatescriptVariableType.Double
			or StatescriptVariableType.Decimal;
	}

	private static bool IsVectorType(StatescriptVariableType type)
	{
		return type is StatescriptVariableType.Vector2 or StatescriptVariableType.Vector3
			or StatescriptVariableType.Vector4 or StatescriptVariableType.Plane
			or StatescriptVariableType.Quaternion;
	}

	private static int GetVectorComponentCount(StatescriptVariableType type)
	{
		return type switch
		{
			StatescriptVariableType.Vector2 => 2,
			StatescriptVariableType.Vector3 => 3,
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
			_ => 4,
		};
	}

	private static double GetArrayVectorComponent(
		StatescriptGraphVariable variable,
		int arrayIndex,
		int componentIndex)
	{
		Variant val = variable.InitialArrayValues[arrayIndex];

		return variable.VariableType switch
		{
			StatescriptVariableType.Vector2 => componentIndex == 0
				? val.AsVector2().X
				: val.AsVector2().Y,
			StatescriptVariableType.Vector3 => componentIndex switch
			{
				0 => val.AsVector3().X,
				1 => val.AsVector3().Y,
				_ => val.AsVector3().Z,
			},
			StatescriptVariableType.Vector4 => componentIndex switch
			{
				0 => val.AsVector4().X,
				1 => val.AsVector4().Y,
				2 => val.AsVector4().Z,
				_ => val.AsVector4().W,
			},
			StatescriptVariableType.Plane => componentIndex switch
			{
				0 => val.AsPlane().Normal.X,
				1 => val.AsPlane().Normal.Y,
				2 => val.AsPlane().Normal.Z,
				_ => val.AsPlane().D,
			},
			StatescriptVariableType.Quaternion => componentIndex switch
			{
				0 => val.AsQuaternion().X,
				1 => val.AsQuaternion().Y,
				2 => val.AsQuaternion().Z,
				_ => val.AsQuaternion().W,
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
			_ => 0,
		};
	}

	private static void UpdateArrayVectorValue(
		StatescriptGraphVariable variable,
		int arrayIndex,
		SpinBox[] spins)
	{
		variable.InitialArrayValues[arrayIndex] = variable.VariableType switch
		{
			StatescriptVariableType.Vector2 => Variant.From(
				new Vector2((float)spins[0].Value, (float)spins[1].Value)),
			StatescriptVariableType.Vector3 => Variant.From(
				new Vector3((float)spins[0].Value, (float)spins[1].Value, (float)spins[2].Value)),
			StatescriptVariableType.Vector4 => Variant.From(
				new Vector4(
					(float)spins[0].Value,
					(float)spins[1].Value,
					(float)spins[2].Value,
					(float)spins[3].Value)),
			StatescriptVariableType.Plane => Variant.From(
				new Plane(
					new Vector3((float)spins[0].Value, (float)spins[1].Value, (float)spins[2].Value),
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
			_ => variable.InitialArrayValues[arrayIndex],
		};

		variable.EmitChanged();
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
			var capturedIndex = i;

			if (variable.VariableType == StatescriptVariableType.Bool)
			{
				var elementRow = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
				vBox.AddChild(elementRow);
				elementRow.AddChild(new Label { Text = $"[{i}]" });

				var toggle = new CheckBox
				{
					ButtonPressed = variable.InitialArrayValues[i].AsBool(),
				};

				toggle.Toggled += x =>
				{
					variable.InitialArrayValues[capturedIndex] = Variant.From(x);
					variable.EmitChanged();
				};

				elementRow.AddChild(toggle);
				AddArrayElementRemoveButton(elementRow, variable, capturedIndex);
			}
			else if (IsVectorType(variable.VariableType))
			{
				var elementVBox = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
				vBox.AddChild(elementVBox);

				var labelRow = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
				elementVBox.AddChild(labelRow);
				labelRow.AddChild(new Label
				{
					Text = $"[{i}]",
					SizeFlagsHorizontal = SizeFlags.ExpandFill,
				});

				AddArrayElementRemoveButton(labelRow, variable, capturedIndex);

				var componentCount = GetVectorComponentCount(variable.VariableType);
				string[] labels = componentCount switch
				{
					2 => ["X", "Y"],
					3 => ["X", "Y", "Z"],
					_ => ["X", "Y", "Z", "W"],
				};

				var spins = new SpinBox[componentCount];

				for (var c = 0; c < componentCount; c++)
				{
					var compRow = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
					compRow.AddChild(new Label
					{
						Text = $"  {labels[c]}",
						CustomMinimumSize = new Vector2(30, 0),
					});

					var spin = new SpinBox
					{
						Value = GetArrayVectorComponent(variable, capturedIndex, c),
						Step = 0.01,
						Rounded = false,
						SizeFlagsHorizontal = SizeFlags.ExpandFill,
					};

					spins[c] = spin;
					spin.ValueChanged += _ =>
						UpdateArrayVectorValue(variable, capturedIndex, spins);
					compRow.AddChild(spin);
					elementVBox.AddChild(compRow);
				}
			}
			else
			{
				var elementRow = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
				vBox.AddChild(elementRow);
				elementRow.AddChild(new Label { Text = $"[{i}]" });

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

					variable.EmitChanged();
				};

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
			Text = "✕",
			CustomMinimumSize = new Vector2(24, 24),
		};

		removeElementButton.Pressed += () =>
		{
			variable.InitialArrayValues.RemoveAt(elementIndex);
			RebuildList();
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
