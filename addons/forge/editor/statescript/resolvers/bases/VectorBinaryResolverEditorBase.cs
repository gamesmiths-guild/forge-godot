// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers.Bases;
using ForgeVariant128 = Gamesmiths.Forge.Statescript.Variant128;
using SysVector2 = System.Numerics.Vector2;
using SysVector3 = System.Numerics.Vector3;
using SysVector4 = System.Numerics.Vector4;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers.Bases;

internal abstract partial class VectorBinaryResolverEditorBase<TResource> : BinaryNestedResolverEditorBase<TResource>
	where TResource : BinaryNestedResolverResourceBase, new()
{
	protected override Type[] FactoryExpectedTypes => [typeof(SysVector2), typeof(SysVector3), typeof(SysVector4)];

	protected override Type NestedExpectedType => typeof(ForgeVariant128);

	public override bool IsCompatibleWith(Type expectedType)
	{
		return expectedType == typeof(ForgeVariant128)
			|| ResolverEditorCompatibility.IsVectorType(expectedType);
	}

	protected override Type[] GetFactoryExpectedTypes(Type expectedType)
	{
		return expectedType == typeof(ForgeVariant128)
			? [typeof(SysVector2), typeof(SysVector3), typeof(SysVector4)]
			: [expectedType];
	}

	protected override Type GetNestedExpectedType(Type expectedType)
	{
		return expectedType == typeof(ForgeVariant128)
			? typeof(ForgeVariant128)
			: expectedType;
	}
}
#endif
