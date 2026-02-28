// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Statescript;

/// <summary>
/// Shared factory methods for creating value-editor controls used by both the variable panel and resolver editors.
/// </summary>
internal static class StatescriptEditorControls
{
	private static readonly Color _axisXColor = new(0.96f, 0.37f, 0.37f);
	private static readonly Color _axisYColor = new(0.54f, 0.83f, 0.01f);
	private static readonly Color _axisZColor = new(0.33f, 0.55f, 0.96f);
	private static readonly Color _axisWColor = new(0.66f, 0.66f, 0.66f);

	/// <summary>
	/// Returns <see langword="true"/> for integer-like variable types.
	/// </summary>
	/// <param name="type">The variable type to check.</param>
	public static bool IsIntegerType(StatescriptVariableType type)
	{
		return type is StatescriptVariableType.Int or StatescriptVariableType.UInt
			or StatescriptVariableType.Long or StatescriptVariableType.ULong
			or StatescriptVariableType.Short or StatescriptVariableType.UShort
			or StatescriptVariableType.Byte or StatescriptVariableType.SByte
			or StatescriptVariableType.Char;
	}

	/// <summary>
	/// Returns <see langword="true"/> for floating-point variable types.
	/// </summary>
	/// <param name="type">The variable type to check.</param>
	public static bool IsFloatType(StatescriptVariableType type)
	{
		return type is StatescriptVariableType.Float or StatescriptVariableType.Double
			or StatescriptVariableType.Decimal;
	}

	/// <summary>
	/// Returns <see langword="true"/> for multi-component vector/quaternion/plane variable types.
	/// </summary>
	/// <param name="type">The variable type to check.</param>
	public static bool IsVectorType(StatescriptVariableType type)
	{
		return type is StatescriptVariableType.Vector2 or StatescriptVariableType.Vector3
			or StatescriptVariableType.Vector4 or StatescriptVariableType.Plane
			or StatescriptVariableType.Quaternion;
	}

	/// <summary>
	/// Creates a <see cref="PanelContainer"/> wrapping a <see cref="CheckBox"/> for boolean editing.
	/// </summary>
	/// <param name="value">The initial value of the boolean variable.</param>
	/// <param name="onChanged">An action to invoke when the boolean value changes.</param>
	/// <returns>A <see cref="PanelContainer"/> containing the boolean editor control.</returns>
	public static PanelContainer CreateBoolEditor(bool value, Action<bool> onChanged)
	{
		var container = new PanelContainer
		{
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
		};

		Control baseControl = EditorInterface.Singleton.GetBaseControl();
		var style = baseControl.GetThemeStylebox("normal", "LineEdit").Duplicate() as StyleBox;
		style!.SetContentMarginAll(0);
		container.AddThemeStyleboxOverride("panel", style);

		var checkButton = new CheckBox
		{
			Text = "On",
			ButtonPressed = value,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
		};

		checkButton.Toggled += x => onChanged(x);
		container.AddChild(checkButton);
		return container;
	}

	/// <summary>
	/// Creates an <see cref="EditorSpinSlider"/> configured for the given numeric variable type.
	/// </summary>
	/// <param name="type">The type of the numeric variable.</param>
	/// <param name="value">The initial value of the numeric variable.</param>
	/// <param name="onChanged">An action invoked on value change.</param>
	/// <returns>An <see cref="EditorSpinSlider"/> configured for the specified numeric variable type.</returns>
	public static EditorSpinSlider CreateNumericSpinSlider(
		StatescriptVariableType type,
		double value,
		Action<double>? onChanged = null)
	{
		NumericConfig config = GetNumericConfig(type);

		var spin = new EditorSpinSlider
		{
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
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			Value = value,
		};

		var isDragging = false;

		if (onChanged is not null)
		{
			spin.ValueChanged += x =>
			{
				if (!isDragging)
				{
					onChanged?.Invoke(x);
				}
			};
		}

		spin.Grabbed += () =>
		{
			isDragging = true;
		};

		spin.Ungrabbed += () =>
		{
			isDragging = false;
			onChanged?.Invoke(spin.Value);
		};

		spin.FocusExited += () =>
		{
			isDragging = false;
		};

		return spin;
	}

	/// <summary>
	/// Creates a panel with a row of labelled <see cref="EditorSpinSlider"/> controls for editing a vector value.
	/// </summary>
	/// <param name="type">The type of the vector/quaternion/plane.</param>
	/// <param name="getComponent">A function to retrieve the value of a specific component.</param>
	/// <param name="onChanged">An action to invoke when any component value changes.</param>
	/// <returns>A <see cref="VBoxContainer"/> containing the vector editor controls.</returns>
	public static VBoxContainer CreateVectorEditor(
		StatescriptVariableType type,
		Func<int, double> getComponent,
		Action<double[]>? onChanged)
	{
		var componentCount = GetVectorComponentCount(type);
		var labels = GetVectorComponentLabels(type);
		var vBox = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };

		var row = new HBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
		row.AddThemeConstantOverride("separation", 0);

		var panelContainer = new PanelContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };

		Control baseControl = EditorInterface.Singleton.GetBaseControl();
		var style = baseControl.GetThemeStylebox("normal", "LineEdit").Duplicate() as StyleBox;
		style!.SetContentMarginAll(0);
		panelContainer.AddThemeStyleboxOverride("panel", style);

		vBox.AddChild(panelContainer);
		panelContainer.AddChild(row);

		var values = new double[componentCount];
		var isDragging = false;

		for (var i = 0; i < componentCount; i++)
		{
			values[i] = getComponent(i);

			var spin = new EditorSpinSlider
			{
				Label = labels[i],
				Step = 0.001,
				Rounded = false,
				EditingInteger = false,
				AllowGreater = true,
				AllowLesser = true,
				Flat = false,
				ControlState = EditorSpinSlider.ControlStateEnum.Hide,
				SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
				SizeFlagsStretchRatio = 1,
				CustomMinimumSize = new Vector2(71, 0),
				Value = values[i],
			};

			spin.AddThemeColorOverride("label_color", GetComponentColor(i));

			var capturedI = i;

			spin.ValueChanged += x =>
			{
				values[capturedI] = x;

				if (!isDragging)
				{
					onChanged?.Invoke(values);
				}
			};

			spin.Grabbed += () =>
			{
				isDragging = true;
			};

			spin.Ungrabbed += () =>
			{
				isDragging = false;
				onChanged?.Invoke(values);
			};

			spin.FocusExited += () =>
			{
				isDragging = false;
			};

			row.AddChild(spin);
		}

		return vBox;
	}

	/// <summary>
	/// Reads a single component from a vector/quaternion/plane variant.
	/// </summary>
	/// <param name="value">The variant containing the vector/quaternion/plane value.</param>
	/// <param name="type">The type of the vector/quaternion/plane.</param>
	/// <param name="index">The index of the component to retrieve.</param>
	/// <exception cref="NotImplementedException">Exception thrown if the provided type is not a vector/quaternion/plane
	/// type.</exception>
	public static double GetVectorComponent(Variant value, StatescriptVariableType type, int index)
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

	/// <summary>
	/// Builds a Godot <see cref="Variant"/> from a component array for the given vector/quaternion/plane type.
	/// </summary>
	/// <param name="type">The type of the vector/quaternion/plane.</param>
	/// <param name="values">The array of component values.</param>
	/// <returns>A <see cref="Variant"/> representing the vector/quaternion/plane.</returns>
	/// <exception cref="NotImplementedException">Exception thrown if the provided type is not a vector/quaternion/plane
	/// type.</exception>
	public static Variant BuildVectorVariant(StatescriptVariableType type, double[] values)
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

	private readonly record struct NumericConfig(
		double MinValue,
		double MaxValue,
		double Step,
		bool IsInteger,
		bool AllowBeyondRange);
}
#endif
