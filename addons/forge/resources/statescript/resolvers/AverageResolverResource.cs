// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;

namespace Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;

/// <summary>
/// Resolver resource that computes the arithmetic mean of all elements of a nested numeric array source.
/// </summary>
[Tool]
[GlobalClass]
public partial class AverageResolverResource : ArrayReductionResolverResourceBase
{
	/// <inheritdoc/>
	public override string ResolverTypeId => "Average";

	/// <inheritdoc/>
	public override string PropertyNamePrefix => "average";

	/// <inheritdoc/>
	public override IPropertyResolver BuildResolver(Graph graph)
	{
		if (!ArrayResolverResourceUtilities.TryResolveNumericArraySource(
			Source,
			ResolverTypeId,
			graph,
			out IArrayPropertyResolver? sourceValueArray))
		{
			return new VariantResolver(default, typeof(double));
		}

		return new AverageResolver(sourceValueArray!);
	}
}
