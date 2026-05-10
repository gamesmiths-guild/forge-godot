// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;

namespace Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;

[Tool]
[GlobalClass]
public partial class ScaleResolverResource : BinaryNestedResolverResourceBase
{
	public override string ResolverTypeId => "Scale";

	protected override string PropertyNamePrefix => "__scale";

	protected override IPropertyResolver CreateResolver(
		IPropertyResolver leftResolver,
		IPropertyResolver rightResolver,
		Graph graph)
	{
		rightResolver = AdaptResolverForExpectedType(rightResolver, typeof(float));
		return new ScaleResolver(leftResolver, rightResolver);
	}
}
