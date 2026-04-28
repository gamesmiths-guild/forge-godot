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
internal sealed partial class QuaternionFromYawPitchRollResolverEditor
	: TernaryNestedResolverEditorBase<QuaternionFromYawPitchRollResolverResource>
{
	public override string DisplayName => "Quaternion From Yaw Pitch Roll";

	public override string ResolverTypeId => "QuaternionFromYawPitchRoll";

	protected override Type[] FirstFactoryExpectedTypes => [typeof(float), typeof(double)];

	protected override Type[] SecondFactoryExpectedTypes => [typeof(float), typeof(double)];

	protected override Type[] ThirdFactoryExpectedTypes => [typeof(float), typeof(double)];

	protected override Type FirstNestedExpectedType => typeof(float);

	protected override Type SecondNestedExpectedType => typeof(float);

	protected override Type ThirdNestedExpectedType => typeof(float);

	protected override string FirstTitle => "Yaw:";

	protected override string SecondTitle => "Pitch:";

	protected override string ThirdTitle => "Roll:";

	public override bool IsCompatibleWith(Type expectedType)
	{
		return expectedType == typeof(SysQuaternion) || expectedType == typeof(ForgeVariant128);
	}
}
#endif

