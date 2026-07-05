// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;

namespace Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;

/// <summary>
/// Resolver resource that reads the smallest element of a nested numeric array source.
/// </summary>
[Tool]
[GlobalClass]
public partial class MinElementResolverResource : ArrayReductionResolverResourceBase
{
	/// <inheritdoc/>
	public override string ResolverTypeId => "MinElement";

	/// <inheritdoc/>
	public override string PropertyNamePrefix => "minelement";

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

		return new MinElementResolver(sourceValueArray!);
	}
}
