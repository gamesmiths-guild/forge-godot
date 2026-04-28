// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;

namespace Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;

[Tool]
[GlobalClass]
public partial class PlaneFromVerticesResolverResource : TernaryNestedResolverResourceBase
{
	public override string ResolverTypeId => "PlaneFromVertices";

	protected override string PropertyNamePrefix => "__planefromvertices";

	protected override IPropertyResolver CreateResolver(
		IPropertyResolver firstResolver,
		IPropertyResolver secondResolver,
		IPropertyResolver thirdResolver,
		Graph graph)
	{
		return new PlaneFromVerticesResolver(firstResolver, secondResolver, thirdResolver);
	}
}
