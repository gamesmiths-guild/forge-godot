// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;

namespace Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;

[Tool]
[GlobalClass]
public partial class LengthSquaredResolverResource : UnaryNestedResolverResourceBase
{
	public override string ResolverTypeId => "LengthSquared";

	protected override string PropertyNamePrefix => "__length2";

	protected override IPropertyResolver CreateResolver(IPropertyResolver operandResolver, Graph graph)
	{
		return new LengthSquaredResolver(operandResolver);
	}
}
