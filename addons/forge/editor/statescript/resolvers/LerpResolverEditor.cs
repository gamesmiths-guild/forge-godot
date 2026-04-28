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
internal sealed partial class LerpResolverEditor : NumericOrVectorTernaryResolverEditorBase<LerpResolverResource>
{
	public override string DisplayName => "Lerp";

	public override string ResolverTypeId => "Lerp";

	protected override string FirstTitle => "A:";

	protected override string SecondTitle => "B:";

	protected override string ThirdTitle => "T:";

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
}
#endif
