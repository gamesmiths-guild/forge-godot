// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers.Bases;
using ForgeVariant128 = Gamesmiths.Forge.Statescript.Variant128;
using SysVector2 = System.Numerics.Vector2;
using SysVector3 = System.Numerics.Vector3;
using SysVector4 = System.Numerics.Vector4;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers.Bases;

internal abstract partial class NumericOrVectorTernaryResolverEditorBase<TResource>
	: TernaryNestedResolverEditorBase<TResource>
	where TResource : TernaryNestedResolverResourceBase, new()
{
	protected override Type[] FirstFactoryExpectedTypes =>
		[typeof(int), typeof(float), typeof(double), typeof(SysVector2), typeof(SysVector3), typeof(SysVector4)];

	protected override Type[] SecondFactoryExpectedTypes =>
		[typeof(int), typeof(float), typeof(double), typeof(SysVector2), typeof(SysVector3), typeof(SysVector4)];

	protected override Type[] ThirdFactoryExpectedTypes => [typeof(int), typeof(float), typeof(double)];

	protected override Type FirstNestedExpectedType => typeof(ForgeVariant128);

	protected override Type SecondNestedExpectedType => typeof(ForgeVariant128);

	protected override Type ThirdNestedExpectedType => typeof(float);

	public override bool IsCompatibleWith(Type expectedType)
	{
		return expectedType == typeof(ForgeVariant128)
			|| ResolverEditorCompatibility.IsNumericOrVectorType(expectedType)
			|| ResolverEditorCompatibility.IsQuaternionType(expectedType);
	}

	protected override Type[] GetFirstFactoryExpectedTypes(Type expectedType)
	{
		return expectedType == typeof(ForgeVariant128)
			? FirstFactoryExpectedTypes
			: [expectedType];
	}

	protected override Type[] GetSecondFactoryExpectedTypes(Type expectedType)
	{
		return expectedType == typeof(ForgeVariant128)
			? SecondFactoryExpectedTypes
			: [expectedType];
	}

	protected override Type GetFirstNestedExpectedType(Type expectedType)
	{
		return expectedType == typeof(ForgeVariant128)
			? typeof(ForgeVariant128)
			: expectedType;
	}

	protected override Type GetSecondNestedExpectedType(Type expectedType)
	{
		return expectedType == typeof(ForgeVariant128)
			? typeof(ForgeVariant128)
			: expectedType;
	}
}
#endif
