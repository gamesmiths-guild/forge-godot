// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;

namespace Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;

[Tool]
[GlobalClass]
public partial class DistanceSquaredResolverResource : BinaryNestedResolverResourceBase
{
	public override string ResolverTypeId => "DistanceSquared";

	protected override string PropertyNamePrefix => "__dist2";

	protected override IPropertyResolver CreateResolver(
		IPropertyResolver leftResolver,
		IPropertyResolver rightResolver,
		Graph graph)
	{
		return new DistanceSquaredResolver(leftResolver, rightResolver);
	}
}
