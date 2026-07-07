// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;

namespace Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;

/// <summary>
/// Resolver resource that reads the last element of a nested value-typed array source. For entity arrays use
/// <see cref="EntityLastResolverResource"/>.
/// </summary>
[Tool]
[GlobalClass]
public partial class LastResolverResource : ArrayReductionResolverResourceBase
{
	/// <inheritdoc/>
	public override string ResolverTypeId => "Last";

	/// <inheritdoc/>
	public override string PropertyNamePrefix => "last";

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
				"Statescript: Last resolver requires a value-typed array source. Use Last Entity for entity arrays.");
			return new VariantResolver(default, typeof(int));
		}

		return new LastResolver(sourceValueArray!);
	}
}
