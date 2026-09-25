// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using ForgeVariant128 = Gamesmiths.Forge.Statescript.Variant128;
using SysVector2 = System.Numerics.Vector2;
using SysVector3 = System.Numerics.Vector3;
using SysVector4 = System.Numerics.Vector4;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers.Bases;

internal abstract partial class NumericOrVectorBinaryResolverEditorBase : BinaryNestedResolverEditorBase
{
	protected override Type[] FactoryExpectedTypes =>
	[
		typeof(int),
		typeof(float),
		typeof(double),
		typeof(SysVector2),
		typeof(SysVector3),
		typeof(SysVector4),
	];

	protected override Type NestedExpectedType => typeof(ForgeVariant128);

	public override bool IsCompatibleWith(Type expectedType)
	{
		return expectedType == typeof(ForgeVariant128)
			|| ResolverEditorCompatibility.IsNumericOrVectorType(expectedType);
	}
}
#endif
