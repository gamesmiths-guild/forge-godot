// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;

namespace Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;

[Tool]
[GlobalClass]
public partial class SinHResolverResource : UnaryNestedResolverResourceBase
{
	public override string ResolverTypeId => "SinH";

	protected override string PropertyNamePrefix => "__sinh";

	protected override IPropertyResolver CreateResolver(IPropertyResolver operandResolver, Graph graph)
	{
		return new SinHResolver(operandResolver);
	}
}
