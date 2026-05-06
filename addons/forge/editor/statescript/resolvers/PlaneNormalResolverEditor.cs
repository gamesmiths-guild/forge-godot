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
internal sealed partial class PlaneNormalResolverEditor : UnaryNestedResolverEditorBase<PlaneNormalResolverResource>
{
	public override string DisplayName => "Plane Normal";

	public override string ResolverTypeId => "PlaneNormal";

	protected override Type[] FactoryExpectedTypes => [typeof(SysPlane)];

	protected override Type NestedExpectedType => typeof(SysPlane);

	public override bool IsCompatibleWith(Type expectedType)
	{
		return expectedType == typeof(SysVector3) || expectedType == typeof(ForgeVariant128);
	}
}
#endif
