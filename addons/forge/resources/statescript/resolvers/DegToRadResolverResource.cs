// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;

namespace Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;

[Tool]
[GlobalClass]
public partial class DegToRadResolverResource : UnaryNestedResolverResourceBase
{
	public override string ResolverTypeId => "DegToRad";

	protected override string PropertyNamePrefix => "__degtorad";

	protected override IPropertyResolver CreateResolver(IPropertyResolver operandResolver, Graph graph)
	{
		return new DegToRadResolver(operandResolver);
	}
}
