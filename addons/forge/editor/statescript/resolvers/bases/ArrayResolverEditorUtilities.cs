// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using Gamesmiths.Forge.Godot.Core.Statescript;
using Gamesmiths.Forge.Godot.Resources.Statescript;
using ForgeVariant128 = Gamesmiths.Forge.Statescript.Variant128;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers.Bases;

/// <summary>
/// Shared type-compatibility helpers for array-operation resolver editors.
/// </summary>
internal static class ArrayResolverEditorUtilities
{
	/// <summary>
	/// Checks whether an expected element type can be produced by array-transformation editors: any authorable value
	/// type, any registered object variable type, or the wildcard types.
	/// </summary>
	/// <param name="expectedType">The expected element type.</param>
	/// <returns><see langword="true"/> when the element type is supported.</returns>
	internal static bool IsSupportedElementType(Type expectedType)
	{
		return expectedType == typeof(ForgeVariant128)
			|| expectedType == typeof(object)
			|| StatescriptVariableTypeConverter.TryFromSystemType(expectedType, out _)
			|| StatescriptObjectVariableTypeRegistry.IsObjectType(expectedType);
	}

	/// <summary>
	/// Checks whether an expected type accepts a numeric result.
	/// </summary>
	/// <param name="expectedType">The expected type.</param>
	/// <returns><see langword="true"/> when the type is numeric or the wildcard type.</returns>
	internal static bool IsNumericExpectedType(Type expectedType)
	{
		return expectedType == typeof(ForgeVariant128)
			|| expectedType == typeof(byte)
			|| expectedType == typeof(sbyte)
			|| expectedType == typeof(short)
			|| expectedType == typeof(ushort)
			|| expectedType == typeof(int)
			|| expectedType == typeof(uint)
			|| expectedType == typeof(long)
			|| expectedType == typeof(ulong)
			|| expectedType == typeof(float)
			|| expectedType == typeof(double)
			|| expectedType == typeof(decimal);
	}
}
#endif
