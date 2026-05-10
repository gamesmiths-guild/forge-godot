// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using Gamesmiths.Forge.Godot.Resources.Statescript;
using ForgeVariant128 = Gamesmiths.Forge.Statescript.Variant128;
using SysPlane = System.Numerics.Plane;
using SysQuaternion = System.Numerics.Quaternion;
using SysVector2 = System.Numerics.Vector2;
using SysVector3 = System.Numerics.Vector3;
using SysVector4 = System.Numerics.Vector4;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers.Bases;

internal static class ResolverEditorCompatibility
{
	public static readonly Type[] FloatOperandExpectedTypes = [typeof(int), typeof(float), typeof(double)];

	public static bool IsNumericType(Type expectedType)
	{
		if (!StatescriptVariableTypeConverter.TryFromSystemType(expectedType, out StatescriptVariableType variableType))
		{
			return false;
		}

		return StatescriptEditorControls.IsIntegerType(variableType)
			|| StatescriptEditorControls.IsFloatType(variableType);
	}

	public static bool IsFloatType(Type expectedType)
	{
		if (!StatescriptVariableTypeConverter.TryFromSystemType(expectedType, out StatescriptVariableType variableType))
		{
			return false;
		}

		return StatescriptEditorControls.IsFloatType(variableType);
	}

	public static bool IsVectorType(Type expectedType)
	{
		return expectedType == typeof(SysVector2)
			|| expectedType == typeof(SysVector3)
			|| expectedType == typeof(SysVector4);
	}

	public static bool IsQuaternionType(Type expectedType)
	{
		return expectedType == typeof(SysQuaternion);
	}

	public static bool IsPlaneType(Type expectedType)
	{
		return expectedType == typeof(SysPlane);
	}

	public static bool IsVectorPlaneOrQuaternionType(Type expectedType)
	{
		return IsVectorType(expectedType) || IsPlaneType(expectedType) || IsQuaternionType(expectedType);
	}

	public static bool IsNumericOrVectorType(Type expectedType)
	{
		return IsNumericType(expectedType) || IsVectorType(expectedType);
	}

	public static bool IsNumericVectorOrQuaternionType(Type expectedType)
	{
		return IsNumericOrVectorType(expectedType) || IsQuaternionType(expectedType);
	}

	public static bool AreExpectedTypesCompatible(Type candidateExpectedType, Type allowedExpectedType)
	{
		return candidateExpectedType == allowedExpectedType
			|| candidateExpectedType == typeof(ForgeVariant128)
			|| allowedExpectedType == typeof(ForgeVariant128)
			|| StatescriptVariableTypeConverter.IsCompatible(allowedExpectedType, candidateExpectedType);
	}
}
#endif
