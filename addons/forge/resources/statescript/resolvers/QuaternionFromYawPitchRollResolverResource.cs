// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;

namespace Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;

[Tool]
[GlobalClass]
public partial class QuaternionFromYawPitchRollResolverResource : TernaryNestedResolverResourceBase
{
	public override string ResolverTypeId => "QuaternionFromYawPitchRoll";

	protected override string PropertyNamePrefix => "__quatypr";

	protected override IPropertyResolver CreateResolver(
		IPropertyResolver firstResolver,
		IPropertyResolver secondResolver,
		IPropertyResolver thirdResolver,
		Graph graph)
	{
		firstResolver = AdaptResolverForExpectedType(firstResolver, typeof(float));
		secondResolver = AdaptResolverForExpectedType(secondResolver, typeof(float));
		thirdResolver = AdaptResolverForExpectedType(thirdResolver, typeof(float));
		return new QuaternionFromYawPitchRollResolver(firstResolver, secondResolver, thirdResolver);
	}
}
