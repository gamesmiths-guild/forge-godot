// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Godot;
using ForgeVariant128 = Gamesmiths.Forge.Statescript.Variant128;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers;

[Tool]
internal sealed partial class WrapResolverEditor : NumericOrVectorTernaryResolverEditorBase<WrapResolverResource>
{
	public override string DisplayName => "Wrap";

	public override string ResolverTypeId => "Wrap";

	protected override string FirstTitle => "Value:";

	protected override string SecondTitle => "Min:";

	protected override string ThirdTitle => "Max:";

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
