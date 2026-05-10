// Copyright © Gamesmiths Guild.

using System;
using System.Globalization;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;

namespace Gamesmiths.Forge.Godot.Resources.Statescript;

internal static class StatescriptNumericCompatibility
{
	public static bool CanCoerce(Type sourceType, Type targetType)
	{
		if (sourceType == targetType)
		{
			return true;
		}

		if (!IsNumericType(sourceType) || !IsNumericType(targetType))
		{
			return false;
		}

		if (IsIntegralType(targetType))
		{
			return IsIntegralType(sourceType);
		}

		return IsFloatingPointType(targetType);
	}

	public static Variant128 Coerce(Variant128 value, Type sourceType, Type targetType)
	{
		if (!CanCoerce(sourceType, targetType))
		{
			throw new InvalidOperationException(
				$"Cannot coerce Statescript numeric value from '{sourceType}' to '{targetType}'.");
		}

		object numericValue = GetNumericValue(sourceType, value);
		return targetType switch
		{
			_ when targetType == typeof(byte) => new Variant128(
				Convert.ToByte(numericValue, CultureInfo.InvariantCulture)),
			_ when targetType == typeof(sbyte) => new Variant128(
				Convert.ToSByte(numericValue, CultureInfo.InvariantCulture)),
			_ when targetType == typeof(char) => new Variant128(
				Convert.ToChar(numericValue, CultureInfo.InvariantCulture)),
			_ when targetType == typeof(short) => new Variant128(
				Convert.ToInt16(numericValue, CultureInfo.InvariantCulture)),
			_ when targetType == typeof(ushort) => new Variant128(
				Convert.ToUInt16(numericValue, CultureInfo.InvariantCulture)),
			_ when targetType == typeof(int) => new Variant128(
				Convert.ToInt32(numericValue, CultureInfo.InvariantCulture)),
			_ when targetType == typeof(uint) => new Variant128(
				Convert.ToUInt32(numericValue, CultureInfo.InvariantCulture)),
			_ when targetType == typeof(long) => new Variant128(
				Convert.ToInt64(numericValue, CultureInfo.InvariantCulture)),
			_ when targetType == typeof(ulong) => new Variant128(
				Convert.ToUInt64(numericValue, CultureInfo.InvariantCulture)),
			_ when targetType == typeof(float) => new Variant128(
				Convert.ToSingle(numericValue, CultureInfo.InvariantCulture)),
			_ when targetType == typeof(double) => new Variant128(
				Convert.ToDouble(numericValue, CultureInfo.InvariantCulture)),
			_ when targetType == typeof(decimal) => new Variant128(
				Convert.ToDecimal(numericValue, CultureInfo.InvariantCulture)),
			_ => throw new InvalidOperationException(
				$"Cannot coerce Statescript numeric value to unsupported target type '{targetType}'."),
		};
	}

	public static bool IsNumericType(Type type)
	{
		return type == typeof(byte)
			|| type == typeof(sbyte)
			|| type == typeof(char)
			|| type == typeof(short)
			|| type == typeof(ushort)
			|| type == typeof(int)
			|| type == typeof(uint)
			|| type == typeof(long)
			|| type == typeof(ulong)
			|| type == typeof(float)
			|| type == typeof(double)
			|| type == typeof(decimal);
	}

	private static bool IsFloatingPointType(Type type)
	{
		return type == typeof(float) || type == typeof(double) || type == typeof(decimal);
	}

	private static bool IsIntegralType(Type type)
	{
		return type == typeof(byte)
			|| type == typeof(sbyte)
			|| type == typeof(char)
			|| type == typeof(short)
			|| type == typeof(ushort)
			|| type == typeof(int)
			|| type == typeof(uint)
			|| type == typeof(long)
			|| type == typeof(ulong);
	}

	private static object GetNumericValue(Type type, Variant128 value)
	{
		if (type == typeof(byte))
		{
			return value.AsByte();
		}

		if (type == typeof(sbyte))
		{
			return value.AsSByte();
		}

		if (type == typeof(char))
		{
			return value.AsChar();
		}

		if (type == typeof(short))
		{
			return value.AsShort();
		}

		if (type == typeof(ushort))
		{
			return value.AsUShort();
		}

		if (type == typeof(int))
		{
			return value.AsInt();
		}

		if (type == typeof(uint))
		{
			return value.AsUInt();
		}

		if (type == typeof(long))
		{
			return value.AsLong();
		}

		if (type == typeof(ulong))
		{
			return value.AsULong();
		}

		if (type == typeof(float))
		{
			return value.AsFloat();
		}

		if (type == typeof(double))
		{
			return value.AsDouble();
		}

		if (type == typeof(decimal))
		{
			return value.AsDecimal();
		}

		throw new InvalidOperationException(
			$"Cannot read Statescript numeric value from unsupported source type '{type}'.");
	}
}
