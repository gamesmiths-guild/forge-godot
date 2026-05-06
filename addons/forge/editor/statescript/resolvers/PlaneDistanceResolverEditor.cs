// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Godot;
using ForgeVariant128 = Gamesmiths.Forge.Statescript.Variant128;
using SysPlane = System.Numerics.Plane;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers;

[Tool]
internal sealed partial class PlaneDistanceResolverEditor : UnaryNestedResolverEditorBase<PlaneDistanceResolverResource>
{
	public override string DisplayName => "Plane Distance";

	public override string ResolverTypeId => "PlaneDistance";

	protected override Type[] FactoryExpectedTypes => [typeof(SysPlane)];

	protected override Type NestedExpectedType => typeof(SysPlane);

	public override bool IsCompatibleWith(Type expectedType)
	{
		return expectedType == typeof(float) || expectedType == typeof(ForgeVariant128);
	}
}
#endif
