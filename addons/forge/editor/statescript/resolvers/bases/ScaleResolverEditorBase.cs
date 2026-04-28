// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers.Bases;
using ForgeVariant128 = Gamesmiths.Forge.Statescript.Variant128;
using SysVector2 = System.Numerics.Vector2;
using SysVector3 = System.Numerics.Vector3;
using SysVector4 = System.Numerics.Vector4;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers.Bases;

internal abstract partial class ScaleResolverEditorBase<TResource> : AsymmetricBinaryNestedResolverEditorBase<TResource>
	where TResource : BinaryNestedResolverResourceBase, new()
{
	protected override Type[] LeftFactoryExpectedTypes => [typeof(SysVector2), typeof(SysVector3), typeof(SysVector4)];

	protected override Type[] RightFactoryExpectedTypes => [typeof(float), typeof(double)];

	protected override Type LeftNestedExpectedType => typeof(ForgeVariant128);

	protected override Type RightNestedExpectedType => typeof(float);

	protected override string LeftTitle => "Vector:";

	protected override string RightTitle => "Scalar:";

	public override bool IsCompatibleWith(Type expectedType)
	{
		return expectedType == typeof(ForgeVariant128)
			|| ResolverEditorCompatibility.IsVectorType(expectedType);
	}

	protected override Type[] GetLeftFactoryExpectedTypes(Type expectedType)
	{
		return expectedType == typeof(ForgeVariant128)
			? LeftFactoryExpectedTypes
			: [expectedType];
	}

	protected override Type GetLeftNestedExpectedType(Type expectedType)
	{
		return expectedType == typeof(ForgeVariant128)
			? typeof(ForgeVariant128)
			: expectedType;
	}
}
#endif
