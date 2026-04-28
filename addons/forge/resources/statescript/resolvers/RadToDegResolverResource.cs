// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;

namespace Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;

[Tool]
[GlobalClass]
public partial class RadToDegResolverResource : UnaryNestedResolverResourceBase
{
	public override string ResolverTypeId => "RadToDeg";

	protected override string PropertyNamePrefix => "__radtodeg";

	protected override IPropertyResolver CreateResolver(IPropertyResolver operandResolver, Graph graph)
	{
		return new RadToDegResolver(operandResolver);
	}
}
