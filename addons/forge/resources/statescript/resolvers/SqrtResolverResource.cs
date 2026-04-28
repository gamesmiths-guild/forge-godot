// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;

namespace Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;

[Tool]
[GlobalClass]
public partial class SqrtResolverResource : UnaryNestedResolverResourceBase
{
	public override string ResolverTypeId => "Sqrt";

	protected override string PropertyNamePrefix => "__sqrt";

	protected override IPropertyResolver CreateResolver(IPropertyResolver operandResolver, Graph graph)
	{
		return new SqrtResolver(operandResolver);
	}
}
