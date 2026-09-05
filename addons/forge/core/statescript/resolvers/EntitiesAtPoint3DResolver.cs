// Copyright © Gamesmiths Guild.

using System.Collections.Generic;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Godot.Core.Statescript.Physics;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;
using NumericsVector3 = System.Numerics.Vector3;

namespace Gamesmiths.Forge.Godot.Core.Statescript.Resolvers;

/// <summary>
/// Resolves the entities whose colliders contain a point.
/// </summary>
/// <remarks>
/// <para>The one physics query with no shape behind it. An overlap with a very small sphere is close but not the same
/// thing: it needs a radius nobody meant to author, and that radius is the difference between "standing exactly here"
/// and "standing near here".</para>
/// <para>What it is for is asking about a place rather than about a volume — what occupies the tile the player
/// clicked, whether a spawn point is free, what is inside a doorway. In 2D it is the picking query, because a cursor
/// there already names a world point exactly.</para>
/// <para>Nothing here filters. A team check, a health threshold or a tag requirement is a Where over this array, the
/// same as every other query.</para>
/// </remarks>
/// <param name="positionResolver">Resolves the point to test.</param>
/// <param name="maskResolver">Resolves the physics layers the query can find. Zero means every layer.</param>
/// <param name="includeAreas">Whether areas count, as well as bodies.</param>
/// <param name="ignoreResolver">Resolves the entities left out of the results, or <see langword="null"/> to leave out
/// nothing.</param>
internal sealed class EntitiesAtPoint3DResolver(
	IPropertyResolver positionResolver,
	IPropertyResolver? maskResolver,
	bool includeAreas,
	IObjectArrayResolver? ignoreResolver) : ObjectArrayResolver<IForgeEntity>
{
	private readonly IPropertyResolver _positionResolver = positionResolver;
	private readonly IPropertyResolver? _maskResolver = maskResolver;
	private readonly bool _includeAreas = includeAreas;
	private readonly IObjectArrayResolver? _ignoreResolver = ignoreResolver;
	private readonly HashSet<IForgeEntity> _found = [];

	public override IForgeEntity[] ResolveArray(GraphContext graphContext)
	{
		World3D? world = PhysicsQuery3D.ResolveWorld(graphContext);

		if (world is null)
		{
			return [];
		}

		NumericsVector3 origin = _positionResolver.Resolve(graphContext).AsVector3();
		var position = new Vector3(origin.X, origin.Y, origin.Z);

		// The set is kept between resolves so a per-tick read does not allocate one each time. Forge runs its graphs on
		// one thread, and the contents never outlive the copy returned below.
		_found.Clear();

		PhysicsQuery3D.CollectPointOverlaps(
			world,
			position,
			PhysicsQuery3D.ResolveMask(_maskResolver is null ? 0 : (int)_maskResolver.Resolve(graphContext).AsDouble()),
			_includeAreas,
			_ignoreResolver?.ResolveArray(graphContext),
			_found);

		FlashAnswer(graphContext, position);

		var resolved = new IForgeEntity[_found.Count];
		_found.CopyTo(resolved);
		return resolved;
	}

	// Skipped whole rather than left to the individual flashes, so a read bound to a per-tick condition costs one flag
	// when the debug switch is off instead of a walk up the tree for every entity found.
	private void FlashAnswer(GraphContext graphContext, Vector3 position)
	{
		if (!PhysicsDebugDraw3D.IsEnabled)
		{
			return;
		}

		Color color = _found.Count > 0
			? PhysicsDebugDraw3D.OverlapFoundColor
			: PhysicsDebugDraw3D.OverlapEmptyColor;

		PhysicsDebugDraw3D.FlashPoint(graphContext, position, color);

		// The mark says where the question was asked; the outlines say what answered it. A point has no volume of its
		// own to draw, so without them a hit and a miss differ only by the colour of a speck.
		foreach (IForgeEntity entity in _found)
		{
			if (ForgeEntityBridge.TryGetSpatialNode3D(entity, out Node3D? spatialNode)
				&& spatialNode is CollisionObject3D collider)
			{
				PhysicsDebugDraw3D.FlashBody(graphContext, collider, collider.GlobalTransform, color);
			}
		}
	}
}
