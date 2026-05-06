// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;

namespace Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;

[Tool]
[GlobalClass]
public partial class MultiplyResolverResource : BinaryNestedResolverResourceBase
{
	public override string ResolverTypeId => "Multiply";

	protected override string PropertyNamePrefix => "__multiply";

	protected override IPropertyResolver CreateResolver(
		IPropertyResolver leftResolver,
		IPropertyResolver rightResolver,
		Graph graph)
	{
		return new MultiplyResolver(leftResolver, rightResolver);
	}
}
