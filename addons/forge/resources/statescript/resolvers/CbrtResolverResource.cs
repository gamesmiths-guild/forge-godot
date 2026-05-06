// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;

namespace Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;

[Tool]
[GlobalClass]
public partial class CbrtResolverResource : UnaryNestedResolverResourceBase
{
	public override string ResolverTypeId => "Cbrt";

	protected override string PropertyNamePrefix => "__cbrt";

	protected override IPropertyResolver CreateResolver(IPropertyResolver operandResolver, Graph graph)
	{
		return new CbrtResolver(operandResolver);
	}
}
