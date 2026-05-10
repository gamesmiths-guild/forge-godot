// Copyright © Gamesmiths Guild.

using System;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;

namespace Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;

[Tool]
[GlobalClass]
public partial class LerpResolverResource : TernaryNestedResolverResourceBase
{
	public override string ResolverTypeId => "Lerp";

	protected override string PropertyNamePrefix => "__lerp";

	protected override IPropertyResolver CreateResolver(
		IPropertyResolver firstResolver,
		IPropertyResolver secondResolver,
		IPropertyResolver thirdResolver,
		Graph graph)
	{
		if (IsVectorOrQuaternionType(firstResolver.ValueType) || IsVectorOrQuaternionType(secondResolver.ValueType))
		{
			thirdResolver = AdaptResolverForExpectedType(thirdResolver, typeof(float));
		}
		else
		{
			Type preferredFloatingType = GetPreferredFloatingPointType(firstResolver, secondResolver, thirdResolver);
			firstResolver = PromoteIntegralResolverToFloatingPoint(firstResolver, preferredFloatingType);
			secondResolver = PromoteIntegralResolverToFloatingPoint(secondResolver, preferredFloatingType);
			thirdResolver = PromoteIntegralResolverToFloatingPoint(thirdResolver, preferredFloatingType);
		}

		return new LerpResolver(firstResolver, secondResolver, thirdResolver);
	}
}
