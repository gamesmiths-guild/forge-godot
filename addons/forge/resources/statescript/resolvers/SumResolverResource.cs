// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;

namespace Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;

/// <summary>
/// Resolver resource that adds up all elements of a nested numeric array source.
/// </summary>
[Tool]
[GlobalClass]
public partial class SumResolverResource : ArrayReductionResolverResourceBase
{
	/// <inheritdoc/>
	public override string ResolverTypeId => "Sum";

	/// <inheritdoc/>
	public override string PropertyNamePrefix => "sum";

	/// <inheritdoc/>
	public override IPropertyResolver BuildResolver(Graph graph)
	{
		if (!ArrayResolverResourceUtilities.TryResolveNumericArraySource(
			Source,
			ResolverTypeId,
			graph,
			out IArrayPropertyResolver? sourceValueArray))
		{
			return new VariantResolver(default, typeof(int));
		}

		return new SumResolver(sourceValueArray!);
	}
}
