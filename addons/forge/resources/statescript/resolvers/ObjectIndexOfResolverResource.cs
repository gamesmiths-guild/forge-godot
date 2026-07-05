// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;

namespace Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;

/// <summary>
/// Resolver resource that finds the zero-based index of a given entity in a nested entity array source, matched by
/// reference identity, or -1 when absent.
/// </summary>
[Tool]
[GlobalClass]
public partial class ObjectIndexOfResolverResource : ArrayReductionResolverResourceBase
{
	/// <inheritdoc/>
	public override string ResolverTypeId => "ObjectIndexOf";

	/// <inheritdoc/>
	public override string PropertyNamePrefix => "objectindexof";

	/// <summary>
	/// Gets or sets the nested entity resolver providing the entity to search for.
	/// </summary>
	[Export]
	public EntityResolverResourceBase? Value { get; set; }

	/// <inheritdoc/>
	public override IPropertyResolver BuildResolver(Graph graph)
	{
		if (!ArrayResolverResourceUtilities.TryResolveEntityArraySource(
			Source,
			ResolverTypeId,
			graph,
			out IObjectArrayResolver<IForgeEntity>? entityArray))
		{
			return new VariantResolver(new Variant128(-1), typeof(int));
		}

		IEntityResolver value = Value?.BuildEntityResolver(graph) ?? new AbilityOwnerResolver();
		return new ObjectIndexOfResolver(entityArray!, value);
	}
}
