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
internal sealed partial class MoveTowardsResolverEditor : TernaryNestedResolverEditorBase<MoveTowardsResolverResource>
{
	public override string DisplayName => "Move Towards";

	public override string ResolverTypeId => "MoveTowards";

	protected override Type[] FirstFactoryExpectedTypes =>
		[typeof(int), typeof(float), typeof(double), typeof(SysVector2), typeof(SysVector3), typeof(SysVector4)];

	protected override Type[] SecondFactoryExpectedTypes =>
		[typeof(int), typeof(float), typeof(double), typeof(SysVector2), typeof(SysVector3), typeof(SysVector4)];

	protected override Type[] ThirdFactoryExpectedTypes => ResolverEditorCompatibility.FloatOperandExpectedTypes;

	protected override Type FirstNestedExpectedType => typeof(ForgeVariant128);

	protected override Type SecondNestedExpectedType => typeof(ForgeVariant128);

	protected override Type ThirdNestedExpectedType => typeof(float);

	protected override string FirstTitle => "Current:";

	protected override string SecondTitle => "Target:";

	protected override string ThirdTitle => "Max Delta:";

	public override bool IsCompatibleWith(Type expectedType)
	{
		return expectedType == typeof(float)
			|| expectedType == typeof(SysVector2)
			|| expectedType == typeof(SysVector3)
			|| expectedType == typeof(SysVector4)
			|| expectedType == typeof(ForgeVariant128);
	}

	protected override Type[] GetFirstFactoryExpectedTypes(Type expectedType)
	{
		if (expectedType == typeof(float))
		{
			return ResolverEditorCompatibility.FloatOperandExpectedTypes;
		}
		else if (expectedType == typeof(ForgeVariant128))
		{
			return [typeof(int), typeof(float), typeof(double), typeof(SysVector2), typeof(SysVector3), typeof(SysVector4)];
		}
		else
		{
			return [expectedType];
		}
	}

	protected override Type[] GetSecondFactoryExpectedTypes(Type expectedType)
	{
		return GetFirstFactoryExpectedTypes(expectedType);
	}
}
#endif
