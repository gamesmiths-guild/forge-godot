// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers.Bases;
using ForgeVariant128 = Gamesmiths.Forge.Statescript.Variant128;
using SysQuaternion = System.Numerics.Quaternion;
using SysVector2 = System.Numerics.Vector2;
using SysVector3 = System.Numerics.Vector3;
using SysVector4 = System.Numerics.Vector4;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers.Bases;

internal abstract partial class VectorOrQuaternionUnaryFloatResolverEditorBase<TResource>
	: UnaryNestedResolverEditorBase<TResource>
	where TResource : UnaryNestedResolverResourceBase, new()
{
	protected override Type[] FactoryExpectedTypes =>
		[typeof(SysVector2), typeof(SysVector3), typeof(SysVector4), typeof(SysQuaternion)];

	protected override Type NestedExpectedType => typeof(ForgeVariant128);

	public override bool IsCompatibleWith(Type expectedType)
	{
		return expectedType == typeof(float) || expectedType == typeof(ForgeVariant128);
	}
}
#endif
