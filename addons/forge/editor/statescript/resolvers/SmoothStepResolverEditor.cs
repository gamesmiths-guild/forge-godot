// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Godot;
using ForgeVariant128 = Gamesmiths.Forge.Statescript.Variant128;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers;

[Tool]
internal sealed partial class SmoothStepResolverEditor
	: NumericOrVectorTernaryResolverEditorBase<SmoothStepResolverResource>
{
	public override string DisplayName => "Smooth Step";

	public override string ResolverTypeId => "SmoothStep";

	protected override string FirstTitle => "Edge 0:";

	protected override string SecondTitle => "Edge 1:";

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
