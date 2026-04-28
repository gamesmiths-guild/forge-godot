// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Godot;
using ForgeVariant128 = Gamesmiths.Forge.Statescript.Variant128;
using SysQuaternion = System.Numerics.Quaternion;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers;

[Tool]
internal sealed partial class SlerpResolverEditor : TernaryNestedResolverEditorBase<SlerpResolverResource>
{
	public override string DisplayName => "Slerp";

	public override string ResolverTypeId => "Slerp";

	protected override Type[] FirstFactoryExpectedTypes => [typeof(SysQuaternion)];

	protected override Type[] SecondFactoryExpectedTypes => [typeof(SysQuaternion)];

	protected override Type[] ThirdFactoryExpectedTypes => [typeof(float), typeof(double)];

	protected override Type FirstNestedExpectedType => typeof(SysQuaternion);

	protected override Type SecondNestedExpectedType => typeof(SysQuaternion);

	protected override Type ThirdNestedExpectedType => typeof(float);

	protected override string FirstTitle => "A:";

	protected override string SecondTitle => "B:";

	protected override string ThirdTitle => "T:";

	public override bool IsCompatibleWith(Type expectedType)
	{
		return expectedType == typeof(SysQuaternion) || expectedType == typeof(ForgeVariant128);
	}

	protected override Type[] GetFirstFactoryExpectedTypes(Type expectedType)
	{
		return [typeof(SysQuaternion)];
	}

	protected override Type[] GetSecondFactoryExpectedTypes(Type expectedType)
	{
		return [typeof(SysQuaternion)];
	}
}
#endif
