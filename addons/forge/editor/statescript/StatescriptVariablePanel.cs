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
internal sealed partial class StatescriptVariablePanel : VBoxContainer, ISerializationListener
{
	private static readonly Color _axisXColor = new(0.96f, 0.37f, 0.37f);
	private static readonly Color _axisYColor = new(0.54f, 0.83f, 0.01f);
	private static readonly Color _axisZColor = new(0.33f, 0.55f, 0.96f);
	private static readonly Color _axisWColor = new(0.66f, 0.66f, 0.66f);

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
		CustomMinimumSize = new Vector2(260, 0);

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
		// Nothing to restore — dialog fields are transient.
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
			StatescriptVariableType.Vector4 => 4,
			StatescriptVariableType.Plane => 4,
			StatescriptVariableType.Quaternion => 4,
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
			_ => 4,
		};
	}

	private static string[] GetVectorComponentLabels(StatescriptVariableType type)
	{
		return type switch
		{
			StatescriptVariableType.Vector2 => ["x", "y"],
			StatescriptVariableType.Vector3 => ["x", "y", "z"],
			StatescriptVariableType.Plane => ["x", "y", "z", "d"],
			StatescriptVariableType.Vector4 => ["x", "y", "z", "w"],
			StatescriptVariableType.Quaternion => ["x", "y", "z", "w"],
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
			_ => ["x", "y", "z", "w"],
		};
	}

	private static Color GetComponentColor(int index)
	{
		return index switch
		{
			0 => _axisXColor,
			1 => _axisYColor,
			2 => _axisZColor,
			_ => _axisWColor,
		};
	}

	private static NumericConfig GetNumericConfig(StatescriptVariableType type)
	{
		// Types whose full range fits comfortably in a double can use exact min/max with clamping.
		// Types with extreme ranges (long, ulong, float, double, decimal) use a reasonable default
		// range and rely on AllowGreater/AllowLesser to permit values outside the slider range.
		return type switch
		{
			StatescriptVariableType.Byte => new NumericConfig(byte.MinValue, byte.MaxValue, 1, true, false),
			StatescriptVariableType.SByte => new NumericConfig(sbyte.MinValue, sbyte.MaxValue, 1, true, false),
			StatescriptVariableType.Char => new NumericConfig(char.MinValue, char.MaxValue, 1, true, false),
			StatescriptVariableType.Short => new NumericConfig(short.MinValue, short.MaxValue, 1, true, false),
			StatescriptVariableType.UShort => new NumericConfig(ushort.MinValue, ushort.MaxValue, 1, true, false),
			StatescriptVariableType.Int => new NumericConfig(int.MinValue, int.MaxValue, 1, true, false),
			StatescriptVariableType.UInt => new NumericConfig(uint.MinValue, uint.MaxValue, 1, true, false),
			StatescriptVariableType.Long => new NumericConfig(-1e15, 1e15, 1, true, true),
			StatescriptVariableType.ULong => new NumericConfig(0, 1e15, 1, true, true),
			StatescriptVariableType.Float => new NumericConfig(-1e10, 1e10, 0.001, false, true),
			StatescriptVariableType.Double => new NumericConfig(-1e10, 1e10, 0.001, false, true),
			StatescriptVariableType.Decimal => new NumericConfig(-1e10, 1e10, 0.001, false, true),
			StatescriptVariableType.Bool => throw new NotImplementedException(),
			StatescriptVariableType.Vector2 => throw new NotImplementedException(),
			StatescriptVariableType.Vector3 => throw new NotImplementedException(),
			StatescriptVariableType.Vector4 => throw new NotImplementedException(),
			StatescriptVariableType.Plane => throw new NotImplementedException(),
			StatescriptVariableType.Quaternion => throw new NotImplementedException(),
			_ => new NumericConfig(-1e10, 1e10, 0.001, false, true),
		};
	}

	private record struct NumericConfig(double MinValue, double MaxValue, double Step, bool IsInteger, bool AllowBeyondRange);

	private static EditorSpinSlider CreateNumericSpinSlider(
		StatescriptVariableType type,
		double value)
	{
		NumericConfig config = GetNumericConfig(type);

		return new EditorSpinSlider
		{
			Value = value,
			Step = config.Step,
			Rounded = config.IsInteger,
			EditingInteger = config.IsInteger,
			MinValue = config.MinValue,
			MaxValue = config.MaxValue,
			AllowGreater = config.AllowBeyondRange,
			AllowLesser = config.AllowBeyondRange,
			ControlState = config.IsInteger
				? EditorSpinSlider.ControlStateEnum.Default
				: EditorSpinSlider.ControlStateEnum.Hide,
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
		};
	}

	private static EditorSpinSlider CreateVectorComponentSpinSlider(
		string label,
		Color labelColor,
		double value)
	{
		var spin = new EditorSpinSlider
		{
			Label = label,
			Value = value,
			Step = 0.001,
			Rounded = false,
			EditingInteger = false,
			AllowGreater = true,
			AllowLesser = true,
			Flat = false,
			ControlState = EditorSpinSlider.ControlStateEnum.Hide,
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			SizeFlagsStretchRatio = 1,
		};

		spin.AddThemeColorOverride("label_color", labelColor);

		return spin;
	}

	private static PanelContainer CreateBoolEditor(bool value, Action<bool> onChanged)
	{
		var container = new PanelContainer
		{
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
		};

		Control baseControl = EditorInterface.Singleton.GetBaseControl();
		var tabBarStyle = baseControl.GetThemeStylebox("normal", "LineEdit").Duplicate() as StyleBox;
		tabBarStyle!.SetContentMarginAll(0);
		container.AddThemeStyleboxOverride("panel", tabBarStyle);

		var checkButton = new CheckBox
		{
			Text = "On",
			ButtonPressed = value,
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
		};

		checkButton.Toggled += x =>
		{
			onChanged(x);
		};

		container.AddChild(checkButton);
		return container;
	}

	private static Control CreateScalarValueEditor(StatescriptGraphVariable variable)
	{
		if (variable.VariableType == StatescriptVariableType.Bool)
		{
			var hBox = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
			hBox.AddChild(new Label { Text = "Value:" });

			hBox.AddChild(CreateBoolEditor(
				variable.InitialValue.AsBool(),
				x =>
				{
					variable.InitialValue = Variant.From(x);
					variable.EmitChanged();
				}));

			return hBox;
		}

		if (IsIntegerType(variable.VariableType) || IsFloatType(variable.VariableType))
		{
			var hBox = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
			hBox.AddChild(new Label { Text = "Value:" });

			EditorSpinSlider spin = CreateNumericSpinSlider(
				variable.VariableType,
				variable.InitialValue.AsDouble());

			spin.ValueChanged += value =>
			{
				variable.InitialValue = IsIntegerType(variable.VariableType)
					? Variant.From((long)value)
					: Variant.From(value);
				variable.EmitChanged();
			};

			hBox.AddChild(spin);
			return hBox;
		}

		if (IsVectorType(variable.VariableType))
		{
			return CreateVectorEditor(
				variable.VariableType,
				x => GetVectorComponent(variable.InitialValue, variable.VariableType, x),
				x => SetVectorValue(variable, x));
		}

		var fallback = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		fallback.AddChild(new Label { Text = variable.VariableType.ToString() });
		return fallback;
	}

	private static VBoxContainer CreateVectorEditor(
		StatescriptVariableType type,
		Func<int, double> getComponent,
		Action<double[]> onChanged)
	{
		var componentCount = GetVectorComponentCount(type);
		var labels = GetVectorComponentLabels(type);
		var vBox = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };

		var row = new HBoxContainer
		{
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
		};

		row.AddThemeConstantOverride("separation", 0);

		var panelContainer = new PanelContainer
		{
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
		};

		Control baseControl = EditorInterface.Singleton.GetBaseControl();
		var tabBarStyle = baseControl.GetThemeStylebox("normal", "LineEdit").Duplicate() as StyleBox;
		tabBarStyle!.SetContentMarginAll(0);
		panelContainer.AddThemeStyleboxOverride("panel", tabBarStyle);

		vBox.AddChild(panelContainer);
		panelContainer.AddChild(row);

		var values = new double[componentCount];

		for (var i = 0; i < componentCount; i++)
		{
			values[i] = getComponent(i);

			EditorSpinSlider spin = CreateVectorComponentSpinSlider(
				labels[i],
				GetComponentColor(i),
				values[i]);

			var capturedI = i;

			spin.ValueChanged += x =>
			{
				values[capturedI] = x;
				onChanged(values);
			};

			row.AddChild(spin);
		}

		return vBox;
	}

	private static double GetVectorComponent(Variant value, StatescriptVariableType type, int index)
	{
		return type switch
		{
			StatescriptVariableType.Vector2 => index == 0
				? value.AsVector2().X
				: value.AsVector2().Y,
			StatescriptVariableType.Vector3 => index switch
			{
				0 => value.AsVector3().X,
				1 => value.AsVector3().Y,
				_ => value.AsVector3().Z,
			},
			StatescriptVariableType.Vector4 => index switch
			{
				0 => value.AsVector4().X,
				1 => value.AsVector4().Y,
				2 => value.AsVector4().Z,
				_ => value.AsVector4().W,
			},
			StatescriptVariableType.Plane => index switch
			{
				0 => value.AsPlane().Normal.X,
				1 => value.AsPlane().Normal.Y,
				2 => value.AsPlane().Normal.Z,
				_ => value.AsPlane().D,
			},
			StatescriptVariableType.Quaternion => index switch
			{
				0 => value.AsQuaternion().X,
				1 => value.AsQuaternion().Y,
				2 => value.AsQuaternion().Z,
				_ => value.AsQuaternion().W,
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

	private static Variant BuildVectorVariant(StatescriptVariableType type, double[] values)
	{
		return type switch
		{
			StatescriptVariableType.Vector2 => Variant.From(
				new Vector2((float)values[0], (float)values[1])),
			StatescriptVariableType.Vector3 => Variant.From(
				new Vector3(
					(float)values[0],
					(float)values[1],
					(float)values[2])),
			StatescriptVariableType.Vector4 => Variant.From(
				new Vector4(
					(float)values[0],
					(float)values[1],
					(float)values[2],
					(float)values[3])),
			StatescriptVariableType.Plane => Variant.From(
				new Plane(
					new Vector3(
						(float)values[0],
						(float)values[1],
						(float)values[2]),
					(float)values[3])),
			StatescriptVariableType.Quaternion => Variant.From(
				new Quaternion(
					(float)values[0],
					(float)values[1],
					(float)values[2],
					(float)values[3])),
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
			_ => Variant.From(0),
		};
	}

	private static void SetVectorValue(StatescriptGraphVariable variable, double[] values)
	{
		variable.InitialValue = BuildVectorVariant(variable.VariableType, values);
		variable.EmitChanged();
	}

	private static void SetArrayVectorValue(
		StatescriptGraphVariable variable,
		int arrayIndex,
		double[] values)
	{
		variable.InitialArrayValues[arrayIndex] = BuildVectorVariant(
			variable.VariableType,
			values);
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

				elementRow.AddChild(CreateBoolEditor(
					variable.InitialArrayValues[i].AsBool(),
					x =>
					{
						variable.InitialArrayValues[capturedIndex] = Variant.From(x);
						variable.EmitChanged();
					}));

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
				var labels = GetVectorComponentLabels(variable.VariableType);
				var values = new double[componentCount];

				var panelContainer = new PanelContainer
				{
					SizeFlagsHorizontal = SizeFlags.ExpandFill,
				};

				Control baseControl = EditorInterface.Singleton.GetBaseControl();
				var tabBarStyle = baseControl.GetThemeStylebox("normal", "LineEdit").Duplicate() as StyleBox;
				tabBarStyle!.SetContentMarginAll(0);
				panelContainer.AddThemeStyleboxOverride("panel", tabBarStyle);

				elementVBox.AddChild(panelContainer);

				var row = new HBoxContainer
				{
					SizeFlagsHorizontal = SizeFlags.ExpandFill,
				};

				row.AddThemeConstantOverride("separation", 0);
				panelContainer.AddChild(row);

				for (var j = 0; j < componentCount; j++)
				{
					values[j] = GetVectorComponent(
						variable.InitialArrayValues[capturedIndex],
						variable.VariableType,
						j);

					EditorSpinSlider spin = CreateVectorComponentSpinSlider(
						labels[j],
						GetComponentColor(j),
						values[j]);

					var capturedJ = j;
					spin.ValueChanged += x =>
					{
						values[capturedJ] = x;
						SetArrayVectorValue(variable, capturedIndex, values);
					};

					row.AddChild(spin);
				}
			}
			else
			{
				var elementRow = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
				vBox.AddChild(elementRow);
				elementRow.AddChild(new Label { Text = $"[{i}]" });

				EditorSpinSlider elementSpin = CreateNumericSpinSlider(
					variable.VariableType,
					variable.InitialArrayValues[i].AsDouble());

				elementSpin.ValueChanged += value =>
				{
					variable.InitialArrayValues[capturedIndex] = IsIntegerType(variable.VariableType)
						? Variant.From((long)value)
						: Variant.From(value);
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
}
#endif
