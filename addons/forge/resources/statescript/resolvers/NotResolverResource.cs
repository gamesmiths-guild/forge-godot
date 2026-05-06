// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;

namespace Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;

[Tool]
[GlobalClass]
public partial class NotResolverResource : UnaryNestedResolverResourceBase
{
	public override string ResolverTypeId => "Not";

	protected override string PropertyNamePrefix => "__not";

	protected override IPropertyResolver CreateResolver(IPropertyResolver operandResolver, Graph graph)
	{
		return new NotResolver(operandResolver);
	}

	protected override IPropertyResolver CreateDefaultOperandResolver()
	{
		return new VariantResolver(new Variant128(false), typeof(bool));
	}
}
