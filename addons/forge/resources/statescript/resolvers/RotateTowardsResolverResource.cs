// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;

namespace Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;

[Tool]
[GlobalClass]
public partial class RotateTowardsResolverResource : TernaryNestedResolverResourceBase
{
	public override string ResolverTypeId => "RotateTowards";

	protected override string PropertyNamePrefix => "__rotatetowards";

	protected override IPropertyResolver CreateResolver(
		IPropertyResolver firstResolver,
		IPropertyResolver secondResolver,
		IPropertyResolver thirdResolver,
		Graph graph)
	{
		thirdResolver = AdaptResolverForExpectedType(thirdResolver, typeof(float));
		return new RotateTowardsResolver(firstResolver, secondResolver, thirdResolver);
	}
}
