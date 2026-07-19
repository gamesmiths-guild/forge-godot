// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;

namespace Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;

/// <summary>
/// Resolver resource that computes the normalized position of a value within a range — the inverse of a Lerp.
/// </summary>
[Tool]
[GlobalClass]
public partial class InverseLerpResolverResource : TernaryNestedResolverResourceBase
{
	/// <inheritdoc/>
	public override string ResolverTypeId => "InverseLerp";

	/// <inheritdoc/>
	protected override string PropertyNamePrefix => "__invlerp";

	/// <inheritdoc/>
	protected override IPropertyResolver CreateResolver(
		IPropertyResolver firstResolver,
		IPropertyResolver secondResolver,
		IPropertyResolver thirdResolver,
		Graph graph)
	{
		return new InverseLerpResolver(firstResolver, secondResolver, thirdResolver);
	}
}
