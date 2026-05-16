// Copyright © Gamesmiths Guild.

using System;
using System.Collections.Generic;
using System.Globalization;
using Gamesmiths.Forge.Core;
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
		StringKey boundName = InputProperties[0].BoundName;

		if (_valueType == StatescriptVariableType.Entity)
		{
			if (graphContext.TryResolveReferenceArray(boundName, out IForgeEntity?[]? entities))
			{
				GD.Print("[Statescript Debug] ", FormatEntityArray(entities));
				return;
			}

			if (graphContext.TryResolveReference(boundName, out IForgeEntity? entity))
			{
				GD.Print("[Statescript Debug] ", FormatEntity(entity));
				return;
			}

			GD.Print("[Statescript Debug] <unresolved>");
			return;
		}

		if (graphContext.TryResolveArray(boundName, out Variant128[]? values))
		{
			GD.Print("[Statescript Debug] ", FormatArray(values));
			return;
		}

		if (graphContext.TryResolveVariant(boundName, out Variant128 value))
		{
			GD.Print("[Statescript Debug] ", FormatValue(value));
			return;
		}

		GD.Print("[Statescript Debug] <unresolved>");
	}

	private static string FormatEntityArray(IForgeEntity?[]? values)
	{
		if (values is null || values.Length == 0)
		{
			return "[]";
		}

		string[] formatted = new string[values.Length];
		for (int i = 0; i < values.Length; i++)
		{
			formatted[i] = FormatEntity(values[i]);
		}

		return $"[{string.Join(", ", formatted)}]";
	}

	private static string FormatEntity(IForgeEntity? entity)
	{
		if (entity is null)
		{
			return "<null>";
		}

		if (entity is global::Godot.Node node)
		{
			return node.GetPath().ToString();
		}

		return entity.GetType().FullName ?? entity.GetType().Name;
	}

	private string FormatValue(Variant128 value)
	{
		return _valueType switch
		{
			StatescriptVariableType.Bool => value.AsBool().ToString(),
			StatescriptVariableType.Byte => value.AsByte().ToString(
				CultureInfo.InvariantCulture),
			StatescriptVariableType.SByte => value.AsSByte().ToString(
				CultureInfo.InvariantCulture),
			StatescriptVariableType.Char => value.AsChar().ToString(),
			StatescriptVariableType.Decimal => value.AsDecimal().ToString(
				CultureInfo.InvariantCulture),
			StatescriptVariableType.Double => value.AsDouble().ToString(
				CultureInfo.InvariantCulture),
			StatescriptVariableType.Float => value.AsFloat().ToString(
				CultureInfo.InvariantCulture),
			StatescriptVariableType.Int => value.AsInt().ToString(
				CultureInfo.InvariantCulture),
			StatescriptVariableType.UInt => value.AsUInt().ToString(
				CultureInfo.InvariantCulture),
			StatescriptVariableType.Long => value.AsLong().ToString(
				CultureInfo.InvariantCulture),
			StatescriptVariableType.ULong => value.AsULong().ToString(
				CultureInfo.InvariantCulture),
			StatescriptVariableType.Short => value.AsShort().ToString(
				CultureInfo.InvariantCulture),
			StatescriptVariableType.UShort => value.AsUShort().ToString(
				CultureInfo.InvariantCulture),
			StatescriptVariableType.Vector2 => value.AsVector2().ToString(),
			StatescriptVariableType.Vector3 => value.AsVector3().ToString(),
			StatescriptVariableType.Vector4 => value.AsVector4().ToString(),
			StatescriptVariableType.Plane => value.AsPlane().ToString(),
			StatescriptVariableType.Quaternion => value.AsQuaternion().ToString(),
			_ => Convert.ToHexString(value.ToBytes()),
		};
	}

	private string FormatArray(Variant128[] values)
	{
		if (values.Length == 0)
		{
			return "[]";
		}

		string[] formatted = new string[values.Length];
		for (int i = 0; i < values.Length; i++)
		{
			formatted[i] = FormatValue(values[i]);
		}

		return $"[{string.Join(", ", formatted)}]";
	}
}
