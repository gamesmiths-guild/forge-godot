// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Godot;
using ForgeVariant128 = Gamesmiths.Forge.Statescript.Variant128;
using SysPlane = System.Numerics.Plane;
using SysVector3 = System.Numerics.Vector3;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers;

[Tool]
internal sealed partial class DotNormalResolverEditor
	: AsymmetricBinaryNestedResolverEditorBase<DotNormalResolverResource>
{
	public override string DisplayName => "Dot Normal";

	public override string ResolverTypeId => "DotNormal";

	protected override Type[] LeftFactoryExpectedTypes => [typeof(SysPlane)];

	protected override Type[] RightFactoryExpectedTypes => [typeof(SysVector3)];

	protected override Type LeftNestedExpectedType => typeof(SysPlane);

	protected override Type RightNestedExpectedType => typeof(SysVector3);

	protected override string LeftTitle => "Plane:";

	protected override string RightTitle => "Normal:";

	public override bool IsCompatibleWith(Type expectedType)
	{
		return expectedType == typeof(float) || expectedType == typeof(ForgeVariant128);
	}
}
#endif
