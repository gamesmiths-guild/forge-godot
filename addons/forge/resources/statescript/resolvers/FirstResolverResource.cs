// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;

namespace Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;

/// <summary>
/// Resolver resource that reads the first element of a nested value-typed array source. For entity arrays use
/// <see cref="EntityFirstResolverResource"/>.
/// </summary>
[Tool]
[GlobalClass]
public partial class FirstResolverResource : ArrayReductionResolverResourceBase
{
	/// <inheritdoc/>
	public override string ResolverTypeId => "First";

	/// <inheritdoc/>
	public override string PropertyNamePrefix => "first";

	/// <inheritdoc/>
	public override IPropertyResolver BuildResolver(Graph graph)
	{
		if (!TryResolveSource(
			graph,
			out IArrayPropertyResolver? sourceValueArray,
			out IObjectArrayResolver? sourceObjectArray))
		{
			return new VariantResolver(default, typeof(int));
		}

		if (sourceObjectArray is not null)
		{
			GD.PushError(
				"Statescript: First resolver requires a value-typed array source. Use First Entity for entity " +
				"arrays.");
			return new VariantResolver(default, typeof(int));
		}

		return new FirstResolver(sourceValueArray!);
	}
}
