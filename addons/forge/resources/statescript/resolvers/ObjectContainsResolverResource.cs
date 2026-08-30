// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;

namespace Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;

/// <summary>
/// Resolver resource that checks whether a nested entity array source contains a given entity, matched by reference
/// identity (e.g. "has this entity already been hit?").
/// </summary>
[Tool]
[GlobalClass]
public partial class ObjectContainsResolverResource : ArrayReductionResolverResourceBase
{
	/// <inheritdoc/>
	public override string ResolverTypeId => "ObjectContains";

	/// <inheritdoc/>
	public override string PropertyNamePrefix => "objectcontains";

	/// <summary>
	/// Gets or sets the nested entity resolver providing the entity to search for.
	/// </summary>
	[Export]
	public StatescriptResolverResource? Value { get; set; }

	/// <inheritdoc/>
	public override IPropertyResolver BuildResolver(Graph graph)
	{
		if (!ArrayResolverResourceUtilities.TryResolveEntityArraySource(
			Source,
			ResolverTypeId,
			graph,
			out IObjectArrayResolver<IForgeEntity>? entityArray))
		{
			return new VariantResolver(new Variant128(false), typeof(bool));
		}

		IEntityResolver value = EntityOperand.BuildOrOwner(Value, graph);
		return new ObjectContainsResolver(entityArray!, value);
	}
}
