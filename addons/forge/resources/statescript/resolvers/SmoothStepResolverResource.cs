// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;

namespace Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;

/// <summary>
/// Resolver resource that computes the smooth Hermite interpolation of a value between two edges.
/// </summary>
[Tool]
[GlobalClass]
public partial class SmoothStepResolverResource : TernaryNestedResolverResourceBase
{
	/// <inheritdoc/>
	public override string ResolverTypeId => "SmoothStep";

	/// <inheritdoc/>
	protected override string PropertyNamePrefix => "__smoothstep";

	/// <inheritdoc/>
	protected override IPropertyResolver CreateResolver(
		IPropertyResolver firstResolver,
		IPropertyResolver secondResolver,
		IPropertyResolver thirdResolver,
		Graph graph)
	{
		return new SmoothStepResolver(firstResolver, secondResolver, thirdResolver);
	}
}
