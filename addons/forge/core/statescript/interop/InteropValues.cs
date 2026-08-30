// Copyright © Gamesmiths Guild.

using System;
using System.Collections.Generic;
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Gamesmiths.Forge.Statescript;
using Godot;
using GodotArray = Godot.Collections.Array;
using Node = Godot.Node;

namespace Gamesmiths.Forge.Godot.Core.Statescript.Interop;

/// <summary>
/// Converts between the values a graph carries and the Godot variants the interop nodes and resolvers exchange with the
/// scene.
/// </summary>
/// <remarks>
/// One conversion table serves every interop item, so a property read, a property write, a method argument and a signal
/// argument all agree on what each <see cref="InteropValueType"/> means. Everything outside the table is reported by
/// the caller rather than converted silently.
/// </remarks>
internal static class InteropValues
{
	/// <summary>
	/// Gets the type an input or output of this kind declares to the graph.
	/// </summary>
	/// <param name="valueType">The interop value type.</param>
	/// <returns>The CLR type, which for <see cref="InteropValueType.None"/> is the one an unused row declares.
	/// </returns>
	public static Type ToClrType(InteropValueType valueType)
	{
		return valueType switch
		{
			InteropValueType.Node => typeof(Node),
			InteropValueType.None => typeof(double),
			_ => StatescriptVariableTypeConverter.ToSystemType(ToVariableType(valueType)),
		};
	}

	/// <summary>
	/// Gets whether values of this kind travel the object lane rather than the value lane.
	/// </summary>
	/// <param name="valueType">The interop value type.</param>
	/// <returns><see langword="true"/> for object-backed values.</returns>
	public static bool IsObjectLane(InteropValueType valueType)
	{
		return valueType == InteropValueType.Node;
	}

	/// <summary>
	/// Gets the kind a CLR type maps to, for seeding an authored type from the slot the resolver sits in.
	/// </summary>
	/// <param name="clrType">The type the slot expects.</param>
	/// <param name="valueType">The matching kind, when there is one.</param>
	/// <returns><see langword="false"/> for the wildcard and for anything outside the supported set, where the slot
	/// says nothing about which kind to read.</returns>
	public static bool TryFromClrType(Type clrType, out InteropValueType valueType)
	{
		if (clrType == typeof(Node))
		{
			valueType = InteropValueType.Node;
			return true;
		}

		if (!StatescriptVariableTypeConverter.TryFromSystemType(clrType, out StatescriptVariableType variableType))
		{
			valueType = InteropValueType.None;
			return false;
		}

		valueType = variableType switch
		{
			StatescriptVariableType.Bool => InteropValueType.Bool,
			StatescriptVariableType.Int => InteropValueType.Int,
			StatescriptVariableType.Vector2 => InteropValueType.Vector2,
			StatescriptVariableType.Vector3 => InteropValueType.Vector3,
			StatescriptVariableType.Vector4 => InteropValueType.Vector4,
			StatescriptVariableType.Plane => InteropValueType.Plane,
			StatescriptVariableType.Quaternion => InteropValueType.Quaternion,
			_ => InteropValueType.Float,
		};

		return true;
	}

	/// <summary>
	/// Gets the graph variable type a value-lane kind maps to.
	/// </summary>
	/// <param name="valueType">The interop value type.</param>
	/// <returns>The matching variable type.</returns>
	public static StatescriptVariableType ToVariableType(InteropValueType valueType)
	{
		return valueType switch
		{
			InteropValueType.Bool => StatescriptVariableType.Bool,
			InteropValueType.Int => StatescriptVariableType.Int,
			InteropValueType.Vector2 => StatescriptVariableType.Vector2,
			InteropValueType.Vector3 => StatescriptVariableType.Vector3,
			InteropValueType.Vector4 => StatescriptVariableType.Vector4,
			InteropValueType.Plane => StatescriptVariableType.Plane,
			InteropValueType.Quaternion => StatescriptVariableType.Quaternion,
			_ => StatescriptVariableType.Double,
		};
	}

	/// <summary>
	/// Converts a value-lane value to the Godot variant to hand to the scene.
	/// </summary>
	/// <param name="value">The graph value.</param>
	/// <param name="valueType">The kind it was resolved as.</param>
	/// <returns>The Godot variant.</returns>
	public static Variant ToGodot(Variant128 value, InteropValueType valueType)
	{
		return StatescriptVariableTypeConverter.ForgeVariantToGodot(value, ToVariableType(valueType));
	}

	/// <summary>
	/// Converts a Godot variant read from the scene to a value-lane value.
	/// </summary>
	/// <param name="value">The Godot variant.</param>
	/// <param name="valueType">The kind to read it as.</param>
	/// <returns>The graph value.</returns>
	public static Variant128 FromGodot(Variant value, InteropValueType valueType)
	{
		return StatescriptVariableTypeConverter.GodotVariantToForge(value, ToVariableType(valueType));
	}

	/// <summary>
	/// Converts an object-lane value to the Godot variant to hand to the scene.
	/// </summary>
	/// <remarks>
	/// A freed node is written as nil rather than as a dangling reference, which is the same answer the Is Valid
	/// resolver gives for one.
	/// </remarks>
	/// <param name="value">The graph value.</param>
	/// <returns>The Godot variant.</returns>
	public static Variant ObjectToGodot(object? value)
	{
		return value is Node node && GodotObject.IsInstanceValid(node) ? Variant.From(node) : default;
	}

	/// <summary>
	/// Converts a Godot variant read from the scene to an object-lane value.
	/// </summary>
	/// <param name="value">The Godot variant.</param>
	/// <returns>The graph value, or <see langword="null"/> when the variant holds anything else.</returns>
	public static object? ObjectFromGodot(Variant value)
	{
		return value.Obj as Node;
	}

	/// <summary>
	/// Converts a value-lane array to the Godot array to hand to the scene.
	/// </summary>
	/// <param name="values">The graph values.</param>
	/// <param name="valueType">The kind they were resolved as.</param>
	/// <returns>The Godot array.</returns>
	public static Variant ToGodotArray(Variant128[] values, InteropValueType valueType)
	{
		var array = new GodotArray();

		foreach (Variant128 value in values)
		{
			array.Add(ToGodot(value, valueType));
		}

		return array;
	}

	/// <summary>
	/// Converts an object-lane array to the Godot array to hand to the scene.
	/// </summary>
	/// <param name="values">The graph values.</param>
	/// <returns>The Godot array.</returns>
	public static Variant ObjectToGodotArray(IReadOnlyList<object?> values)
	{
		var array = new GodotArray();

		for (int i = 0; i < values.Count; i++)
		{
			array.Add(ObjectToGodot(values[i]));
		}

		return array;
	}

	/// <summary>
	/// Converts a Godot array read from the scene to a value-lane array.
	/// </summary>
	/// <param name="value">The Godot variant, which must hold an array.</param>
	/// <param name="valueType">The kind to read the elements as.</param>
	/// <returns>The graph values, empty when the variant is not an array.</returns>
	public static Variant128[] FromGodotArray(Variant value, InteropValueType valueType)
	{
		if (value.VariantType != Variant.Type.Array)
		{
			return [];
		}

		GodotArray array = value.AsGodotArray();
		var values = new Variant128[array.Count];

		for (int i = 0; i < array.Count; i++)
		{
			values[i] = FromGodot(array[i], valueType);
		}

		return values;
	}

	/// <summary>
	/// Converts a Godot array read from the scene to an object-lane array.
	/// </summary>
	/// <param name="value">The Godot variant, which must hold an array.</param>
	/// <returns>The graph values, empty when the variant is not an array.</returns>
	public static object?[] ObjectFromGodotArray(Variant value)
	{
		if (value.VariantType != Variant.Type.Array)
		{
			return [];
		}

		GodotArray array = value.AsGodotArray();
		object?[] values = new object?[array.Count];

		for (int i = 0; i < array.Count; i++)
		{
			values[i] = ObjectFromGodot(array[i]);
		}

		return values;
	}
}
