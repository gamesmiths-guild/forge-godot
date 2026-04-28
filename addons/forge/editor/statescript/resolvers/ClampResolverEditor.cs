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
internal sealed partial class ClampResolverEditor : NumericOrVectorTernaryResolverEditorBase<ClampResolverResource>
{
	public override string DisplayName => "Clamp";

	public override string ResolverTypeId => "Clamp";

	protected override string FirstTitle => "Value:";

	protected override string SecondTitle => "Min:";

	protected override string ThirdTitle => "Max:";

	protected override Type[] GetFirstFactoryExpectedTypes(Type expectedType)
	{
		return expectedType == typeof(ForgeVariant128)
			? [typeof(int), typeof(float), typeof(double), typeof(SysVector2), typeof(SysVector3), typeof(SysVector4)]
			: [expectedType];
	}

	protected override Type[] GetSecondFactoryExpectedTypes(Type expectedType)
	{
		return GetFirstFactoryExpectedTypes(expectedType);
	}

	protected override Type GetFirstNestedExpectedType(Type expectedType)
	{
		return expectedType;
	}

	protected override Type GetSecondNestedExpectedType(Type expectedType)
	{
		return expectedType;
	}

	protected override Type GetThirdNestedExpectedType(Type expectedType)
	{
		return expectedType;
	}
}
#endif
