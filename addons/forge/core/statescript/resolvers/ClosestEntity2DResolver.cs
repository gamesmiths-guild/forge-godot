// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;
using NumericsVector2 = System.Numerics.Vector2;

namespace Gamesmiths.Forge.Godot.Core.Statescript.Resolvers;

/// <summary>
/// Resolves the nearest of a group of entities to a point.
/// </summary>
/// <remarks>
/// <para>It runs no query of its own: it narrows a group something else found, which is what makes it compose. An
/// overlap picks up everyone in range and this picks the one to hit; with core's Except in between, the same pair is
/// chain lightning - nearest, minus everyone already struck, repeated.</para>
/// <para>Composing it out of core would be an Order By over a Distance over a spatial getter and then a First, which
/// is four resolvers to say one thing that graphs say constantly. The parts still work, and anything this cannot
/// express - second nearest, nearest by health - is still reachable through them.</para>
/// <para>It measures to a <em>point</em> rather than to an entity, matching the sight resolvers. An entity's position
/// is an Entity Position 2D away, and a point also covers nearest-to-where-the-player-clicked and nearest-to-a-
/// predicted-intercept, neither of which has an entity to name.</para>
/// <para>Entities with no node in the scene are skipped rather than treated as being at the origin, which would make
/// a despawned candidate win every comparison.</para>
/// </remarks>
/// <param name="entitiesResolver">Resolves the group to choose from, or <see langword="null"/> when nothing was
/// authored to choose from.</param>
/// <param name="positionResolver">Resolves the point to measure to.</param>
internal sealed class ClosestEntity2DResolver(
	IObjectArrayResolver? entitiesResolver,
	IPropertyResolver positionResolver) : ObjectResolver<IForgeEntity>, IEntityResolver
{
	private readonly IObjectArrayResolver? _entitiesResolver = entitiesResolver;
	private readonly IPropertyResolver _positionResolver = positionResolver;

	public override IForgeEntity? Resolve(GraphContext graphContext)
	{
		object?[]? candidates = _entitiesResolver?.ResolveArray(graphContext);

		if (candidates is null || candidates.Length == 0)
		{
			return null;
		}

		NumericsVector2 origin = _positionResolver.Resolve(graphContext).AsVector2();
		var position = new Vector2(origin.X, origin.Y);

		IForgeEntity? closest = null;
		float closestDistance = float.MaxValue;

		for (int i = 0; i < candidates.Length; i++)
		{
			if (candidates[i] is not IForgeEntity entity
				|| !ForgeEntityBridge.TryGetSpatialNode2D(entity, out Node2D? spatialNode))
			{
				continue;
			}

			float distance = spatialNode.GlobalPosition.DistanceSquaredTo(position);

			if (distance < closestDistance)
			{
				closestDistance = distance;
				closest = entity;
			}
		}

		return closest;
	}
}
