// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;

namespace Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;

[Tool]
[GlobalClass]
public partial class ClampMagnitudeResolverResource : BinaryNestedResolverResourceBase
{
	public override string ResolverTypeId => "ClampMagnitude";

	protected override string PropertyNamePrefix => "__clampmag";

	protected override IPropertyResolver CreateResolver(
		IPropertyResolver leftResolver,
		IPropertyResolver rightResolver,
		Graph graph)
	{
		rightResolver = AdaptResolverForExpectedType(rightResolver, typeof(float));
		return new ClampMagnitudeResolver(leftResolver, rightResolver);
	}
}
