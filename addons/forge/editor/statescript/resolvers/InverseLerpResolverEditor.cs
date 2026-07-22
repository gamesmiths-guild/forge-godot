// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Godot;
using ForgeVariant128 = Gamesmiths.Forge.Statescript.Variant128;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers;

[Tool]
internal sealed partial class InverseLerpResolverEditor
	: NumericOrVectorTernaryResolverEditorBase<InverseLerpResolverResource>
{
	public override string DisplayName => "Inverse Lerp";

	public override string ResolverTypeId => "InverseLerp";

	protected override string FirstTitle => "A:";

	protected override string SecondTitle => "B:";

	protected override string ThirdTitle => "Value:";

	public override bool IsCompatibleWith(Type expectedType)
	{
		return expectedType == typeof(float)
			|| expectedType == typeof(double)
			|| expectedType == typeof(ForgeVariant128);
	}

	protected override Type[] GetFirstFactoryExpectedTypes(Type expectedType)
	{
		return [typeof(int), typeof(float), typeof(double)];
	}

	protected override Type[] GetSecondFactoryExpectedTypes(Type expectedType)
	{
		return GetFirstFactoryExpectedTypes(expectedType);
	}

	protected override Type[] GetThirdFactoryExpectedTypes(Type expectedType)
	{
		return GetFirstFactoryExpectedTypes(expectedType);
	}
}
#endif
