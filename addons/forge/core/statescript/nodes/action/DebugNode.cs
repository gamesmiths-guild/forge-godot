// Copyright © Gamesmiths Guild.

using System;
using System.Collections.Generic;
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Godot;

namespace Gamesmiths.Forge.Statescript.Nodes.Action;

/// <summary>
/// Action node that resolves an input value of any supported type and prints it through
/// <see cref="GD.Print(params Variant[])"/>.
/// Useful for validating resolver chains while testing Statescript graphs in the editor.
/// </summary>
public sealed class DebugNode : ActionNode
{
	private readonly StatescriptVariableType _valueType;

	/// <inheritdoc/>
	public override string Description => "Prints the resolved input value to the Godot console for debugging.";

	public DebugNode(StatescriptVariableType valueType = StatescriptVariableType.Int)
	{
		_valueType = valueType;
	}

	/// <inheritdoc/>
	protected override void DefineParameters(
		List<InputProperty> inputProperties,
		List<OutputVariable> outputVariables)
	{
		inputProperties.Add(new InputProperty("Value", StatescriptVariableTypeConverter.ToSystemType(_valueType)));
	}

	/// <inheritdoc/>
	protected override void Execute(GraphContext graphContext)
	{
		if (!graphContext.TryResolveVariant(InputProperties[0].BoundName, out Variant128 value))
		{
			GD.Print("[Statescript Debug] <unresolved>");
			return;
		}

		GD.Print("[Statescript Debug] ", FormatValue(value));
	}

	private string FormatValue(Variant128 value)
	{
		return _valueType switch
		{
			StatescriptVariableType.Bool => value.AsBool().ToString(),
			StatescriptVariableType.Byte => value.AsByte().ToString(
				System.Globalization.CultureInfo.InvariantCulture),
			StatescriptVariableType.SByte => value.AsSByte().ToString(
				System.Globalization.CultureInfo.InvariantCulture),
			StatescriptVariableType.Char => value.AsChar().ToString(),
			StatescriptVariableType.Decimal => value.AsDecimal().ToString(
				System.Globalization.CultureInfo.InvariantCulture),
			StatescriptVariableType.Double => value.AsDouble().ToString(
				System.Globalization.CultureInfo.InvariantCulture),
			StatescriptVariableType.Float => value.AsFloat().ToString(
				System.Globalization.CultureInfo.InvariantCulture),
			StatescriptVariableType.Int => value.AsInt().ToString(
				System.Globalization.CultureInfo.InvariantCulture),
			StatescriptVariableType.UInt => value.AsUInt().ToString(
				System.Globalization.CultureInfo.InvariantCulture),
			StatescriptVariableType.Long => value.AsLong().ToString(
				System.Globalization.CultureInfo.InvariantCulture),
			StatescriptVariableType.ULong => value.AsULong().ToString(
				System.Globalization.CultureInfo.InvariantCulture),
			StatescriptVariableType.Short => value.AsShort().ToString(
				System.Globalization.CultureInfo.InvariantCulture),
			StatescriptVariableType.UShort => value.AsUShort().ToString(
				System.Globalization.CultureInfo.InvariantCulture),
			StatescriptVariableType.Vector2 => value.AsVector2().ToString(),
			StatescriptVariableType.Vector3 => value.AsVector3().ToString(),
			StatescriptVariableType.Vector4 => value.AsVector4().ToString(),
			StatescriptVariableType.Plane => value.AsPlane().ToString(),
			StatescriptVariableType.Quaternion => value.AsQuaternion().ToString(),
			_ => Convert.ToHexString(value.ToBytes()),
		};
	}
}
