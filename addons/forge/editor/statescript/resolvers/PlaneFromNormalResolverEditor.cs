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
internal sealed partial class PlaneFromNormalResolverEditor
	: AsymmetricBinaryNestedResolverEditorBase<PlaneFromNormalResolverResource>
{
	public override string DisplayName => "Plane From Normal";

	public override string ResolverTypeId => "PlaneFromNormal";

	protected override Type[] LeftFactoryExpectedTypes => [typeof(SysVector3)];

	protected override Type[] RightFactoryExpectedTypes => [typeof(float), typeof(double)];

	protected override Type LeftNestedExpectedType => typeof(SysVector3);

	protected override Type RightNestedExpectedType => typeof(float);

	protected override string LeftTitle => "Normal:";

	protected override string RightTitle => "Distance:";

	public override bool IsCompatibleWith(Type expectedType)
	{
		return expectedType == typeof(SysPlane) || expectedType == typeof(ForgeVariant128);
	}
}
#endif
