// Copyright © Gamesmiths Guild.

using System.Collections.Generic;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Godot.Core.Statescript.Physics;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;

namespace Gamesmiths.Forge.Godot.Core.Statescript.Resolvers;

/// <summary>
/// Resolves the entities an area already in the scene is overlapping.
/// </summary>
/// <remarks>
/// <para>This is the cheapest area of effect there is: the physics server already tracks what the area contains, so
/// reading it costs nothing beyond the walk from collider to entity. Point it at an aura's area, a weapon's hitbox, or
/// a zone the ability spawned, and feed the result to For Each.</para>
/// <para>Nothing here filters. A team check, a health threshold or a tag requirement is a Where over this array, which
/// is why no predicate input exists.</para>
/// </remarks>
/// <param name="entityResolver">Resolves which entity owns the area.</param>
/// <param name="nodePath">Path to the area, from the entity's spatial node. Empty means that node itself.</param>
/// <param name="includeAreas">Whether overlapping areas count, as well as bodies.</param>
/// <param name="ignoreResolver">Resolves the entities left out of the results, or <see langword="null"/> to leave out
/// nothing.</param>
internal sealed class AreaOverlaps2DResolver(
	IEntityResolver entityResolver,
	string nodePath,
	bool includeAreas,
	IObjectArrayResolver? ignoreResolver) : ObjectArrayResolver<IForgeEntity>
{
	private readonly IEntityResolver _entityResolver = entityResolver;
	private readonly string _nodePath = nodePath;
	private readonly bool _includeAreas = includeAreas;
	private readonly IObjectArrayResolver? _ignoreResolver = ignoreResolver;
	private readonly HashSet<IForgeEntity> _found = [];

	private bool _reportedMissingArea;

	public override IForgeEntity[] ResolveArray(GraphContext graphContext)
	{
		IForgeEntity? entity = _entityResolver.Resolve(graphContext);

		if (!ForgeEntityBridge.TryGetSpatialNode2D(entity, _nodePath, out Node2D? spatialNode)
			|| spatialNode is not Area2D area)
		{
			ReportMissingAreaOnce();
			return [];
		}

		// The set is kept between resolves so a per-tick read does not allocate one each time. Forge runs its graphs on
		// one thread, and the contents never outlive the copy returned below.
		_found.Clear();
		PhysicsQuery2D.CollectAreaOverlaps(area, _includeAreas, _ignoreResolver?.ResolveArray(graphContext), _found);

		// The area itself stays undrawn - it is in the scene and Godot renders it already - but which entities are
		// inside it is not something that wireframe says.
		PhysicsDebugDraw2D.FlashTargets(graphContext, _found, PhysicsDebugDraw2D.OverlapFoundColor);

		var resolved = new IForgeEntity[_found.Count];
		_found.CopyTo(resolved);
		return resolved;
	}

	private void ReportMissingAreaOnce()
	{
		if (_reportedMissingArea)
		{
			return;
		}

		_reportedMissingArea = true;

		GD.PushWarning(
			"Statescript: Area Overlaps 2D found no Area2D for its entity" +
			(_nodePath.Length == 0 ? "." : $" at [{_nodePath}].") +
			" Resolving to an empty array.");
	}
}
