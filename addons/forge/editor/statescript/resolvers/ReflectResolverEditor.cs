// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Godot;
using ForgeVariant128 = Gamesmiths.Forge.Statescript.Variant128;
using SysVector2 = System.Numerics.Vector2;
using SysVector3 = System.Numerics.Vector3;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers;

[Tool]
internal sealed partial class ReflectResolverEditor : BinaryNestedResolverEditorBase<ReflectResolverResource>
{
	public override string DisplayName => "Reflect";

	public override string ResolverTypeId => "Reflect";

	protected override Type[] FactoryExpectedTypes => [typeof(SysVector2), typeof(SysVector3)];

	protected override Type NestedExpectedType => typeof(ForgeVariant128);

	protected override string LeftTitle => "Incident:";

	protected override string RightTitle => "Normal:";

	public override bool IsCompatibleWith(Type expectedType)
	{
		return expectedType == typeof(ForgeVariant128)
			|| expectedType == typeof(SysVector2)
			|| expectedType == typeof(SysVector3);
	}

	protected override Type[] GetFactoryExpectedTypes(Type expectedType)
	{
		return expectedType == typeof(ForgeVariant128)
			? [typeof(SysVector2), typeof(SysVector3)]
			: [expectedType];
	}

	protected override Type GetNestedExpectedType(Type expectedType)
	{
		return expectedType;
	}
}
#endif
