// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;

namespace Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;

[Tool]
[GlobalClass]
public partial class FloorResolverResource : UnaryNestedResolverResourceBase
{
	public override string ResolverTypeId => "Floor";

	protected override string PropertyNamePrefix => "__floor";

	protected override IPropertyResolver CreateResolver(IPropertyResolver operandResolver, Graph graph)
	{
		operandResolver = PromoteIntegralResolverToFloatingPoint(
			operandResolver,
			GetPreferredFloatingPointType(operandResolver));
		return new FloorResolver(operandResolver);
	}
}
