// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;

namespace Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;

[Tool]
[GlobalClass]
public partial class ConcatenateResolverResource : BinaryNestedResolverResourceBase
{
	public override string ResolverTypeId => "Concatenate";

	protected override string PropertyNamePrefix => "__concat";

	protected override IPropertyResolver CreateResolver(
		IPropertyResolver leftResolver,
		IPropertyResolver rightResolver,
		Graph graph)
	{
		return new ConcatenateResolver(leftResolver, rightResolver);
	}
}
