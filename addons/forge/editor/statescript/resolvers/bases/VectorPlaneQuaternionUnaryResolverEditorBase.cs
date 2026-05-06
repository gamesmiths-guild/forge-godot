// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers.Bases;
using ForgeVariant128 = Gamesmiths.Forge.Statescript.Variant128;
using SysPlane = System.Numerics.Plane;
using SysQuaternion = System.Numerics.Quaternion;
using SysVector2 = System.Numerics.Vector2;
using SysVector3 = System.Numerics.Vector3;
using SysVector4 = System.Numerics.Vector4;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers.Bases;

internal abstract partial class VectorPlaneQuaternionUnaryResolverEditorBase<TResource>
	: UnaryNestedResolverEditorBase<TResource>
	where TResource : UnaryNestedResolverResourceBase, new()
{
	protected override Type[] FactoryExpectedTypes =>
		[typeof(SysVector2), typeof(SysVector3), typeof(SysVector4), typeof(SysPlane), typeof(SysQuaternion)];

	protected override Type NestedExpectedType => typeof(ForgeVariant128);

	public override bool IsCompatibleWith(Type expectedType)
	{
		return expectedType == typeof(ForgeVariant128)
			|| ResolverEditorCompatibility.IsVectorPlaneOrQuaternionType(expectedType);
	}

	protected override Type[] GetFactoryExpectedTypes(Type expectedType)
	{
		return expectedType == typeof(ForgeVariant128)
			? FactoryExpectedTypes
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
