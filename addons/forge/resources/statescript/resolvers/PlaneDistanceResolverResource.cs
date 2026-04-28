// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;

namespace Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;

[Tool]
[GlobalClass]
public partial class PlaneDistanceResolverResource : UnaryNestedResolverResourceBase
{
	public override string ResolverTypeId => "PlaneDistance";

	protected override string PropertyNamePrefix => "__planedistance";

	protected override IPropertyResolver CreateResolver(IPropertyResolver operandResolver, Graph graph)
	{
		return new PlaneDistanceResolver(operandResolver);
	}
}
