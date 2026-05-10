// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;

namespace Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;

[Tool]
[GlobalClass]
public partial class MoveTowardsResolverResource : TernaryNestedResolverResourceBase
{
	public override string ResolverTypeId => "MoveTowards";

	protected override string PropertyNamePrefix => "__movetowards";

	protected override IPropertyResolver CreateResolver(
		IPropertyResolver firstResolver,
		IPropertyResolver secondResolver,
		IPropertyResolver thirdResolver,
		Graph graph)
	{
		if (StatescriptNumericCompatibility.IsNumericType(firstResolver.ValueType)
			&& StatescriptNumericCompatibility.IsNumericType(secondResolver.ValueType))
		{
			firstResolver = AdaptResolverForExpectedType(firstResolver, typeof(float));
			secondResolver = AdaptResolverForExpectedType(secondResolver, typeof(float));
		}

		thirdResolver = AdaptResolverForExpectedType(thirdResolver, typeof(float));
		return new MoveTowardsResolver(firstResolver, secondResolver, thirdResolver);
	}
}
