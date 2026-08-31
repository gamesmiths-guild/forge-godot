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
/// Resolves the entities standing inside a cone opening from a point.
/// </summary>
/// <remarks>
/// <para>The cleave, the breath weapon, the shotgun spread and the frontal taunt are all this one query, and it earns
/// a slot rather than being composed because the composition is five resolvers deep: an overlap, a Where, a Normalize
/// over a Subtract, a Dot, and a Cos of half the angle. Written out once it is one row.</para>
/// <para>The cone is the sphere the physics server actually sweeps, narrowed by an angle test on each result. There is
/// no cone collision shape in Godot, and there is no way to ask a physics server for one - so the sphere is the query
/// and the angle is the filter, which is also why the range is the cone's slant reach rather than its depth.</para>
/// <para>The aperture is authored in <b>degrees</b>, unlike every other angle in the layer. A cone's aperture is not a
/// rotation: nothing lerps it, wraps it, or reads it off a transform, and every graph that authors one authors a
/// design figure. Radians would put a Deg To Rad resolver in front of every cone in the game to say what "90" already
/// says.</para>
/// <para>Nothing here filters by team or by tag. A Where over this array is how that is spelled, the same as every
/// other query.</para>
/// </remarks>
/// <param name="originResolver">Resolves the cone's apex.</param>
/// <param name="directionResolver">Resolves which way it opens.</param>
/// <param name="rangeResolver">Resolves how far it reaches.</param>
/// <param name="angleResolver">Resolves the full aperture, in degrees.</param>
/// <param name="maskResolver">Resolves the physics layers the query can find. Zero means every layer.</param>
/// <param name="includeAreas">Whether areas count as overlaps, as well as bodies.</param>
/// <param name="ignoreResolver">Resolves the entities left out of the results, or <see langword="null"/> to leave out
/// nothing.</param>
// The sphere is built once and lives as long as the graph does. A property resolver has no teardown to dispose it
// from, and building one per query would allocate a native shape every tick, which is what caching it exists to avoid.
#pragma warning disable CA1001 // Types that own disposable fields should be disposable
internal sealed class EntitiesInCone3DResolver(
	IPropertyResolver originResolver,
	IPropertyResolver directionResolver,
	IPropertyResolver rangeResolver,
	IPropertyResolver angleResolver,
	IPropertyResolver? maskResolver,
	bool includeAreas,
	IObjectArrayResolver? ignoreResolver) : ObjectArrayResolver<IForgeEntity>
#pragma warning restore CA1001 // Types that own disposable fields should be disposable
{
	private readonly IPropertyResolver _originResolver = originResolver;
	private readonly IPropertyResolver _directionResolver = directionResolver;
	private readonly IPropertyResolver _rangeResolver = rangeResolver;
	private readonly IPropertyResolver _angleResolver = angleResolver;
	private readonly IPropertyResolver? _maskResolver = maskResolver;
	private readonly bool _includeAreas = includeAreas;
	private readonly IObjectArrayResolver? _ignoreResolver = ignoreResolver;
	private readonly HashSet<IForgeEntity> _found = [];
	private readonly List<IForgeEntity> _inCone = [];

	// One sphere for the resolver's whole life, resized per query. The alternative is a new Shape3D - a reference
	// counted engine object - every time a graph asks, which for a query bound to a per-tick condition is a lot.
	private readonly SphereShape3D _sphere = new();

	public override IForgeEntity[] ResolveArray(GraphContext graphContext)
	{
		World3D? world = PhysicsQuery3D.ResolveWorld(graphContext);

		NumericsVector3 originValue = _originResolver.Resolve(graphContext).AsVector3();
		NumericsVector3 directionValue = _directionResolver.Resolve(graphContext).AsVector3();
		float range = (float)_rangeResolver.Resolve(graphContext).AsDouble();
		float angle = (float)_angleResolver.Resolve(graphContext).AsDouble();

		var origin = new Vector3(originValue.X, originValue.Y, originValue.Z);
		var direction = new Vector3(directionValue.X, directionValue.Y, directionValue.Z);

		if (world is null || range <= 0 || direction.LengthSquared() <= 0.000001f)
		{
			return [];
		}

		Vector3 axis = direction.Normalized();

		// Halved because the authored figure is the whole aperture - a 90 degree cleave reaches 45 degrees either side
		// of where the caster is facing, which is what "90 degree cleave" means everywhere it is said.
		float halfAngle = Mathf.DegToRad(angle) * 0.5f;

		// The sets are kept between resolves so a per-tick read does not allocate. Forge runs its graphs on one
		// thread, and the contents never outlive the copy returned below.
		_found.Clear();
		_inCone.Clear();

		_sphere.Radius = range;

		PhysicsQuery3D.CollectShapeOverlaps(
			world,
			_sphere,
			new Transform3D(Basis.Identity, origin),
			PhysicsQuery3D.ResolveMask(ResolveMaskValue(graphContext)),
			_includeAreas,
			_ignoreResolver?.ResolveArray(graphContext),
			_found);

		float cosHalfAngle = Mathf.Cos(halfAngle);

		// Filtered by hand rather than through Where, so a query bound to a per-tick condition allocates neither an
		// iterator nor a closure over the three operands the test reads.
#pragma warning disable S3267 // Loops should be simplified using the "Where" LINQ method
		foreach (IForgeEntity entity in _found)
		{
			if (IsInCone(entity, origin, axis, cosHalfAngle))
			{
				_inCone.Add(entity);
			}
		}
#pragma warning restore S3267 // Loops should be simplified using the "Where" LINQ method

		PhysicsDebugDraw3D.FlashCone(
			graphContext,
			origin,
			axis,
			range,
			halfAngle,
			_inCone.Count > 0 ? PhysicsDebugDraw3D.OverlapFoundColor : PhysicsDebugDraw3D.OverlapEmptyColor);

		return [.. _inCone];
	}

	// The apex counts as inside: an entity standing exactly on the caster has no direction to test, and dropping it
	// would make a point-blank cleave the one place a cleave misses.
	private static bool IsInCone(IForgeEntity entity, Vector3 origin, Vector3 axis, float cosHalfAngle)
	{
		if (!ForgeEntityBridge.TryGetSpatialNode3D(entity, out Node3D? spatialNode))
		{
			return false;
		}

		Vector3 offset = spatialNode.GlobalPosition - origin;

		return offset.LengthSquared() <= 0.000001f || offset.Normalized().Dot(axis) >= cosHalfAngle;
	}

	private int ResolveMaskValue(GraphContext graphContext)
	{
		return _maskResolver is null ? 0 : (int)_maskResolver.Resolve(graphContext).AsDouble();
	}
}
