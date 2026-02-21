// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Gamesmiths.Forge.Statescript;
using Godot;
using GodotPlane = Godot.Plane;
using GodotQuaternion = Godot.Quaternion;
using GodotVariant = Godot.Variant;
using GodotVector2 = Godot.Vector2;
using GodotVector3 = Godot.Vector3;
using GodotVector4 = Godot.Vector4;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers;

/// <summary>
/// Resolver editor that holds a constant (inline) value. The user edits the value directly in the node.
/// </summary>
[Tool]
internal sealed partial class VariantResolverEditor : NodeEditorProperty
{
	private StatescriptVariableType _valueType;
	private GodotVariant _currentValue;

	/// <inheritdoc/>
	public override string DisplayName => "Constant";

	/// <inheritdoc/>
	public override Type ValueType => typeof(Variant128);

	/// <inheritdoc/>
	public override string ResolverTypeId => "Variant";

	/// <inheritdoc/>
	public override bool IsCompatibleWith(Type expectedType)
	{
		// Constants can match any supported type.
		return true;
	}

	/// <inheritdoc/>
	public override void Setup(
		StatescriptGraph graph,
		StatescriptNodeProperty? property,
		Type expectedType,
		Action onChanged)
	{
		// Determine the variable type from expectedType.
		if (!StatescriptVariableTypeConverter.TryFromSystemType(expectedType, out _valueType))
		{
			// If expectedType is Variant128 (wildcard), default to Int.
			_valueType = StatescriptVariableType.Int;
		}

		// Restore value from existing binding.
		if (property?.Resolver is VariantResolverResource variantRes)
		{
			_currentValue = variantRes.Value;
			_valueType = variantRes.ValueType;
		}
		else
		{
			_currentValue = StatescriptVariableTypeConverter.CreateDefaultGodotVariant(_valueType);
		}

		CustomMinimumSize = new GodotVector2(200, 40);

		HBoxContainer valueEditor = CreateValueEditor(onChanged);
		AddChild(valueEditor);
	}

	/// <inheritdoc/>
	public override void SaveTo(StatescriptNodeProperty property)
	{
		property.Resolver = new VariantResolverResource
		{
			Value = _currentValue,
			ValueType = _valueType,
		};
	}

	private HBoxContainer CreateValueEditor(Action onChanged)
	{
		var hBox = new HBoxContainer
		{
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
		};

		switch (_valueType)
		{
			case StatescriptVariableType.Bool:
				var checkBox = new CheckButton { ButtonPressed = _currentValue.AsBool() };
				checkBox.Toggled += x =>
				{
					_currentValue = GodotVariant.From(x);
					onChanged();
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
					Value = _currentValue.AsInt32(),
					Step = 1,
					Rounded = true,
					SizeFlagsHorizontal = SizeFlags.ExpandFill,
					CustomMinimumSize = new GodotVector2(80, 0),
				};

				intSpin.ValueChanged += value =>
				{
					_currentValue = GodotVariant.From((int)value);
					onChanged();
				};

				hBox.AddChild(intSpin);
				break;

			case StatescriptVariableType.UInt:
			case StatescriptVariableType.Long:
			case StatescriptVariableType.ULong:
				var longSpin = new SpinBox
				{
					Value = _currentValue.AsInt64(),
					Step = 1,
					Rounded = true,
					SizeFlagsHorizontal = SizeFlags.ExpandFill,
					CustomMinimumSize = new GodotVector2(80, 0),
				};

				longSpin.ValueChanged += value =>
				{
					_currentValue = GodotVariant.From((long)value);
					onChanged();
				};

				hBox.AddChild(longSpin);
				break;

			case StatescriptVariableType.Float:
			case StatescriptVariableType.Double:
			case StatescriptVariableType.Decimal:
				var floatSpin = new SpinBox
				{
					Value = _currentValue.AsDouble(),
					Step = 0.01,
					Rounded = false,
					SizeFlagsHorizontal = SizeFlags.ExpandFill,
					CustomMinimumSize = new GodotVector2(80, 0),
				};

				floatSpin.ValueChanged += value =>
				{
					_currentValue = GodotVariant.From(value);
					onChanged();
				};

				hBox.AddChild(floatSpin);
				break;

			case StatescriptVariableType.Vector2:
				CreateVectorEditor(hBox, 2, onChanged);
				break;

			case StatescriptVariableType.Vector3:
				CreateVectorEditor(hBox, 3, onChanged);
				break;

			case StatescriptVariableType.Vector4:
			case StatescriptVariableType.Plane:
			case StatescriptVariableType.Quaternion:
				CreateVectorEditor(hBox, 4, onChanged);
				break;

			default:
				hBox.AddChild(new Label { Text = _valueType.ToString() });
				break;
		}

		return hBox;
	}

	private void CreateVectorEditor(HBoxContainer parent, int components, Action onChanged)
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
				Value = GetVectorComponent(i),
				Step = 0.01,
				Rounded = false,
				SizeFlagsHorizontal = SizeFlags.ExpandFill,
				CustomMinimumSize = new GodotVector2(50, 0),
			};

			spins[i] = spin;
			spin.ValueChanged += _ => OnVectorComponentChanged(spins, onChanged);
			parent.AddChild(spin);
		}
	}

	private double GetVectorComponent(int index)
	{
		return _valueType switch
		{
			StatescriptVariableType.Vector2 => index == 0
				? _currentValue.AsVector2().X
				: _currentValue.AsVector2().Y,
			StatescriptVariableType.Vector3 => index switch
			{
				0 => _currentValue.AsVector3().X,
				1 => _currentValue.AsVector3().Y,
				_ => _currentValue.AsVector3().Z,
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
				0 => _currentValue.AsVector4().X,
				1 => _currentValue.AsVector4().Y,
				2 => _currentValue.AsVector4().Z,
				_ => _currentValue.AsVector4().W,
			},
		};
	}

	private void OnVectorComponentChanged(SpinBox[] spins, Action onChanged)
	{
		_currentValue = _valueType switch
		{
			StatescriptVariableType.Vector2 => GodotVariant.From(
				new GodotVector2(
					(float)spins[0].Value,
					(float)spins[1].Value)),
			StatescriptVariableType.Vector3 => GodotVariant.From(
				new GodotVector3(
					(float)spins[0].Value,
					(float)spins[1].Value,
					(float)spins[2].Value)),
			StatescriptVariableType.Vector4 => GodotVariant.From(
				new GodotVector4(
					(float)spins[0].Value,
					(float)spins[1].Value,
					(float)spins[2].Value,
					(float)spins[3].Value)),
			StatescriptVariableType.Plane => GodotVariant.From(
				new GodotPlane(
					new GodotVector3(
						(float)spins[0].Value,
						(float)spins[1].Value,
						(float)spins[2].Value),
					(float)spins[3].Value)),
			StatescriptVariableType.Quaternion => GodotVariant.From(
				new GodotQuaternion(
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
			_ => _currentValue,
		};

		onChanged();
	}
}
#endif
