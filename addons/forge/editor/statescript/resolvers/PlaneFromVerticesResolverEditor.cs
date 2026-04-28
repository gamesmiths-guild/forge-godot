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
internal sealed partial class PlaneFromVerticesResolverEditor
	: TernaryNestedResolverEditorBase<PlaneFromVerticesResolverResource>
{
	public override string DisplayName => "Plane From Vertices";

	public override string ResolverTypeId => "PlaneFromVertices";

	protected override Type[] FirstFactoryExpectedTypes => [typeof(SysVector3)];

	protected override Type[] SecondFactoryExpectedTypes => [typeof(SysVector3)];

	protected override Type[] ThirdFactoryExpectedTypes => [typeof(SysVector3)];

	protected override Type FirstNestedExpectedType => typeof(SysVector3);

	protected override Type SecondNestedExpectedType => typeof(SysVector3);

	protected override Type ThirdNestedExpectedType => typeof(SysVector3);

	protected override string FirstTitle => "Point 1:";

	protected override string SecondTitle => "Point 2:";

	protected override string ThirdTitle => "Point 3:";

	public override bool IsCompatibleWith(Type expectedType)
	{
		return expectedType == typeof(SysPlane) || expectedType == typeof(ForgeVariant128);
	}

	protected override Type[] GetFirstFactoryExpectedTypes(Type expectedType)
	{
		return [typeof(SysVector3)];
	}

	protected override Type[] GetSecondFactoryExpectedTypes(Type expectedType)
	{
		return [typeof(SysVector3)];
	}

	protected override Type[] GetThirdFactoryExpectedTypes(Type expectedType)
	{
		return [typeof(SysVector3)];
	}
}
#endif
