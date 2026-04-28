// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;

namespace Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;

[Tool]
[GlobalClass]
public partial class PlaneFromNormalResolverResource : BinaryNestedResolverResourceBase
{
	public override string ResolverTypeId => "PlaneFromNormal";

	protected override string PropertyNamePrefix => "__planefromnormal";

	protected override IPropertyResolver CreateResolver(
		IPropertyResolver leftResolver,
		IPropertyResolver rightResolver,
		Graph graph)
	{
		return new PlaneFromNormalResolver(leftResolver, rightResolver);
	}
}
