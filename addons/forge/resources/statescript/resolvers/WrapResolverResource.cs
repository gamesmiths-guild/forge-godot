// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;

namespace Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;

/// <summary>
/// Resolver resource that wraps a value into a range.
/// </summary>
[Tool]
[GlobalClass]
public partial class WrapResolverResource : TernaryNestedResolverResourceBase
{
	/// <inheritdoc/>
	public override string ResolverTypeId => "Wrap";

	/// <inheritdoc/>
	protected override string PropertyNamePrefix => "__wrap";

	/// <inheritdoc/>
	protected override IPropertyResolver CreateResolver(
		IPropertyResolver firstResolver,
		IPropertyResolver secondResolver,
		IPropertyResolver thirdResolver,
		Graph graph)
	{
		return new WrapResolver(firstResolver, secondResolver, thirdResolver);
	}
}
