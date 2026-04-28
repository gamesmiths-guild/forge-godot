// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Godot;
using ForgeVariant128 = Gamesmiths.Forge.Statescript.Variant128;
using SysQuaternion = System.Numerics.Quaternion;
using SysVector3 = System.Numerics.Vector3;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers;

[Tool]
internal sealed partial class LookAtResolverEditor : TernaryNestedResolverEditorBase<LookAtResolverResource>
{
	public override string DisplayName => "Look At";

	public override string ResolverTypeId => "LookAt";

	protected override Type[] FirstFactoryExpectedTypes => [typeof(SysVector3)];

	protected override Type[] SecondFactoryExpectedTypes => [typeof(SysVector3)];

	protected override Type[] ThirdFactoryExpectedTypes => [typeof(SysVector3)];

	protected override Type FirstNestedExpectedType => typeof(SysVector3);

	protected override Type SecondNestedExpectedType => typeof(SysVector3);

	protected override Type ThirdNestedExpectedType => typeof(SysVector3);

	protected override string FirstTitle => "From:";

	protected override string SecondTitle => "To:";

	protected override string ThirdTitle => "Up:";

	public override bool IsCompatibleWith(Type expectedType)
	{
		return expectedType == typeof(SysQuaternion) || expectedType == typeof(ForgeVariant128);
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
