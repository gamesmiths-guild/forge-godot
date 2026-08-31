// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Godot.Core.Statescript.Resolvers;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;
using NumericsVector3 = System.Numerics.Vector3;

namespace Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;

/// <summary>
/// Resolver resource that reads the nearest of a group of entities to a point.
/// </summary>
[Tool]
[GlobalClass]
public partial class ClosestEntity3DResolverResource : EntityResolverResourceBase
{
	/// <inheritdoc/>
	public override string ResolverTypeId => "ClosestEntity3D";

	/// <summary>
	/// Gets or sets the nested resolver providing the group to choose from.
	/// </summary>
	[Export]
	public StatescriptResolverResource? Entities { get; set; }

	/// <summary>
	/// Gets or sets a value indicating whether the entities section is folded in the editor.
	/// </summary>
	[Export]
	public bool EntitiesFolded { get; set; }

	/// <summary>
	/// Gets or sets the nested resolver providing the point to measure to.
	/// </summary>
	[Export]
	public StatescriptResolverResource? Position { get; set; }

	/// <summary>
	/// Gets or sets a value indicating whether the position section is folded in the editor.
	/// </summary>
	[Export]
	public bool PositionFolded { get; set; } = true;

	/// <inheritdoc/>
	public override IEntityResolver BuildEntityResolver(Graph graph)
	{
		// An unset group resolves to nobody rather than to some fallback: a resolver that has not been told what to
		// choose from should find nothing and be obvious about it, matching the unset shape on the overlap queries.
		IObjectArrayResolver? entitiesResolver =
			Entities is not null
			&& Entities.TryBuildArrayResolver(graph, out _, out IObjectArrayResolver? built)
				? built
				: null;

		IPropertyResolver positionResolver = Position is null
			? new EntityPosition3DResolver(new AbilityOwnerResolver(), string.Empty, TransformSpace.Global)
			: AdaptResolverForExpectedType(Position.BuildResolver(graph), typeof(NumericsVector3));

		return new ClosestEntity3DResolver(entitiesResolver, positionResolver);
	}
}
