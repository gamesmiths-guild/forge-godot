// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;

namespace Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;

[Tool]
[GlobalClass]
public partial class TruncateResolverResource : UnaryNestedResolverResourceBase
{
	public override string ResolverTypeId => "Truncate";

	protected override string PropertyNamePrefix => "__truncate";

	protected override IPropertyResolver CreateResolver(IPropertyResolver operandResolver, Graph graph)
	{
		operandResolver = PromoteIntegralResolverToFloatingPoint(
			operandResolver,
			GetPreferredFloatingPointType(operandResolver));
		return new TruncateResolver(operandResolver);
	}
}
