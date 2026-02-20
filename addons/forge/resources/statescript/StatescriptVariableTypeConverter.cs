// Copyright © Gamesmiths Guild.

using System;
using System.Collections.Generic;
using Gamesmiths.Forge.Statescript;
using GodotPlane = Godot.Plane;
using GodotQuaternion = Godot.Quaternion;
using GodotVariant = Godot.Variant;
using GodotVector2 = Godot.Vector2;
using GodotVector3 = Godot.Vector3;
using GodotVector4 = Godot.Vector4;
using SysVector2 = System.Numerics.Vector2;
using SysVector3 = System.Numerics.Vector3;
using SysVector4 = System.Numerics.Vector4;

namespace Gamesmiths.Forge.Godot.Resources.Statescript;

/// <summary>
/// Provides conversion utilities between <see cref="StatescriptVariableType"/>, <see cref="Type"/>, and Godot
/// <see cref="GodotVariant"/> for use in the editor and at graph build time.
/// </summary>
public static class StatescriptVariableTypeConverter
{
	private static readonly Dictionary<StatescriptVariableType, Type> _typeMap = new()
	{
		[StatescriptVariableType.Bool] = typeof(bool),
		[StatescriptVariableType.Byte] = typeof(byte),
		[StatescriptVariableType.SByte] = typeof(sbyte),
		[StatescriptVariableType.Char] = typeof(char),
		[StatescriptVariableType.Decimal] = typeof(decimal),
		[StatescriptVariableType.Double] = typeof(double),
		[StatescriptVariableType.Float] = typeof(float),
		[StatescriptVariableType.Int] = typeof(int),
		[StatescriptVariableType.UInt] = typeof(uint),
		[StatescriptVariableType.Long] = typeof(long),
		[StatescriptVariableType.ULong] = typeof(ulong),
		[StatescriptVariableType.Short] = typeof(short),
		[StatescriptVariableType.UShort] = typeof(ushort),
		[StatescriptVariableType.Vector2] = typeof(SysVector2),
		[StatescriptVariableType.Vector3] = typeof(SysVector3),
		[StatescriptVariableType.Vector4] = typeof(SysVector4),
		[StatescriptVariableType.Plane] = typeof(System.Numerics.Plane),
		[StatescriptVariableType.Quaternion] = typeof(System.Numerics.Quaternion),
	};

	private static readonly Dictionary<Type, StatescriptVariableType> _reverseTypeMap = [];

	static StatescriptVariableTypeConverter()
	{
		foreach (KeyValuePair<StatescriptVariableType, Type> kvp in _typeMap)
		{
			_reverseTypeMap[kvp.Value] = kvp.Key;
		}
	}

	/// <summary>
	/// Gets all supported variable type values.
	/// </summary>
	/// <returns>All values of <see cref="StatescriptVariableType"/>.</returns>
	public static StatescriptVariableType[] GetAllTypes()
	{
		return (StatescriptVariableType[])Enum.GetValues(typeof(StatescriptVariableType));
	}

	/// <summary>
	/// Converts a <see cref="StatescriptVariableType"/> to the corresponding <see cref="Type"/>.
	/// </summary>
	/// <param name="variableType">The variable type enum value.</param>
	/// <returns>The corresponding CLR type.</returns>
	public static Type ToSystemType(StatescriptVariableType variableType)
	{
		return _typeMap[variableType];
	}

	/// <summary>
	/// Tries to find the <see cref="StatescriptVariableType"/> for the given <see cref="Type"/>.
	/// </summary>
	/// <param name="type">The CLR type to look up.</param>
	/// <param name="variableType">The corresponding variable type if found.</param>
	/// <returns><see langword="true"/> if a matching variable type was found.</returns>
	public static bool TryFromSystemType(Type type, out StatescriptVariableType variableType)
	{
		return _reverseTypeMap.TryGetValue(type, out variableType);
	}

	/// <summary>
	/// Checks whether the given <see cref="Type"/> is compatible with the specified variable type.
	/// For <see cref="Variant128"/> (wildcard type), all types are compatible.
	/// </summary>
	/// <param name="expectedType">The expected type from the node declaration.</param>
	/// <param name="variableType">The variable type to check.</param>
	/// <returns><see langword="true"/> if the types are compatible.</returns>
	public static bool IsCompatible(Type expectedType, StatescriptVariableType variableType)
	{
		if (expectedType == typeof(Variant128))
		{
			return true;
		}

		Type actualType = ToSystemType(variableType);
		return expectedType.IsAssignableFrom(actualType);
	}

	/// <summary>
	/// Creates a default <see cref="GodotVariant"/> for the given variable type.
	/// </summary>
	/// <param name="variableType">The variable type.</param>
	/// <returns>A Godot variant containing the default value.</returns>
	public static GodotVariant CreateDefaultGodotVariant(StatescriptVariableType variableType)
	{
		return variableType switch
		{
			StatescriptVariableType.Bool => GodotVariant.From(false),
			StatescriptVariableType.Byte => GodotVariant.From(0),
			StatescriptVariableType.SByte => GodotVariant.From(0),
			StatescriptVariableType.Char => GodotVariant.From(0),
			StatescriptVariableType.Decimal => GodotVariant.From(0.0),
			StatescriptVariableType.Double => GodotVariant.From(0.0),
			StatescriptVariableType.Float => GodotVariant.From(0.0f),
			StatescriptVariableType.Int => GodotVariant.From(0),
			StatescriptVariableType.UInt => GodotVariant.From(0),
			StatescriptVariableType.Long => GodotVariant.From(0L),
			StatescriptVariableType.ULong => GodotVariant.From(0L),
			StatescriptVariableType.Short => GodotVariant.From(0),
			StatescriptVariableType.UShort => GodotVariant.From(0),
			StatescriptVariableType.Vector2 => GodotVariant.From(GodotVector2.Zero),
			StatescriptVariableType.Vector3 => GodotVariant.From(GodotVector3.Zero),
			StatescriptVariableType.Vector4 => GodotVariant.From(GodotVector4.Zero),
			StatescriptVariableType.Plane => GodotVariant.From(new GodotPlane(0, 1, 0, 0)),
			StatescriptVariableType.Quaternion => GodotVariant.From(GodotQuaternion.Identity),
			_ => GodotVariant.From(0),
		};
	}

	/// <summary>
	/// Converts a Godot variant value to a Forge <see cref="Variant128"/> based on the variable type.
	/// </summary>
	/// <param name="godotValue">The Godot variant value.</param>
	/// <param name="variableType">The variable type that determines interpretation.</param>
	/// <returns>The corresponding <see cref="Variant128"/>.</returns>
	public static Variant128 GodotVariantToForge(GodotVariant godotValue, StatescriptVariableType variableType)
	{
		return variableType switch
		{
			StatescriptVariableType.Bool => new Variant128(godotValue.AsBool()),
			StatescriptVariableType.Byte => new Variant128((byte)godotValue.AsInt32()),
			StatescriptVariableType.SByte => new Variant128((sbyte)godotValue.AsInt32()),
			StatescriptVariableType.Char => new Variant128((char)godotValue.AsInt32()),
			StatescriptVariableType.Decimal => new Variant128((decimal)godotValue.AsDouble()),
			StatescriptVariableType.Double => new Variant128(godotValue.AsDouble()),
			StatescriptVariableType.Float => new Variant128(godotValue.AsSingle()),
			StatescriptVariableType.Int => new Variant128(godotValue.AsInt32()),
			StatescriptVariableType.UInt => new Variant128((uint)godotValue.AsInt64()),
			StatescriptVariableType.Long => new Variant128(godotValue.AsInt64()),
			StatescriptVariableType.ULong => new Variant128((ulong)godotValue.AsInt64()),
			StatescriptVariableType.Short => new Variant128((short)godotValue.AsInt32()),
			StatescriptVariableType.UShort => new Variant128((ushort)godotValue.AsInt32()),
			StatescriptVariableType.Vector2 => ToForgeVector2(godotValue.AsVector2()),
			StatescriptVariableType.Vector3 => ToForgeVector3(godotValue.AsVector3()),
			StatescriptVariableType.Vector4 => ToForgeVector4(godotValue.AsVector4()),
			StatescriptVariableType.Plane => ToForgePlane(godotValue.AsPlane()),
			StatescriptVariableType.Quaternion => ToForgeQuaternion(godotValue.AsQuaternion()),
			_ => default,
		};
	}

	/// <summary>
	/// Gets the display name for a variable type.
	/// </summary>
	/// <param name="variableType">The variable type.</param>
	/// <returns>A human-readable name for the type.</returns>
	public static string GetDisplayName(StatescriptVariableType variableType)
	{
		return variableType.ToString();
	}

	private static Variant128 ToForgeVector2(GodotVector2 v)
	{
		return new Variant128(new SysVector2(v.X, v.Y));
	}

	private static Variant128 ToForgeVector3(GodotVector3 v)
	{
		return new Variant128(new SysVector3(v.X, v.Y, v.Z));
	}

	private static Variant128 ToForgeVector4(GodotVector4 v)
	{
		return new Variant128(new SysVector4(v.X, v.Y, v.Z, v.W));
	}

	private static Variant128 ToForgePlane(GodotPlane p)
	{
		return new Variant128(new System.Numerics.Plane(p.Normal.X, p.Normal.Y, p.Normal.Z, p.D));
	}

	private static Variant128 ToForgeQuaternion(GodotQuaternion q)
	{
		return new Variant128(new System.Numerics.Quaternion(q.X, q.Y, q.Z, q.W));
	}
}
