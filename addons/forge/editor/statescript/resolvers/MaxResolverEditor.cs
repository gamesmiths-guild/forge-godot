// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Godot;
using ForgeVariant128 = Gamesmiths.Forge.Statescript.Variant128;
using SysVector2 = System.Numerics.Vector2;
using SysVector3 = System.Numerics.Vector3;
using SysVector4 = System.Numerics.Vector4;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers;

[Tool]
internal sealed partial class MaxResolverEditor : NumericOrVectorBinaryResolverEditorBase<MaxResolverResource>
{
	public override string DisplayName => "Max";

	public override string ResolverTypeId => "Max";

	protected override Type[] GetFactoryExpectedTypes(Type expectedType)
	{
		return expectedType == typeof(ForgeVariant128)
			? [typeof(int), typeof(float), typeof(double), typeof(SysVector2), typeof(SysVector3), typeof(SysVector4)]
			: [expectedType];
	}

	protected override Type GetNestedExpectedType(Type expectedType)
	{
		return expectedType;
	}
}
#endif
