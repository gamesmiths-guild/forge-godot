// Copyright © Gamesmiths Guild.

using System;
using Gamesmiths.Forge.Godot.Core.Statescript.Physics;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;
using NumericsVector2 = System.Numerics.Vector2;

namespace Gamesmiths.Forge.Godot.Core.Statescript.Resolvers;

/// <summary>
/// Resolves the nearest point on the navigation mesh to a point.
/// </summary>
/// <remarks>
/// <para>What makes a ground-targeted ability land somewhere legal. A click on the world, a predicted intercept or a
/// point offset from a character is an arbitrary position that may be inside a wall or off a ledge; this clamps it
/// onto walkable floor. A blink to the cursor, a summon placed where the player pointed, a ground-targeted area that
/// must not open inside geometry.</para>
/// <para>It answers about the mesh, not about a walk. The nearest walkable point may be across a gap from whoever is
/// blinking to it - Nav Reachable 2D is the question that says whether they can get there, and the two compose.</para>
/// <para>The point is returned unchanged when there is no navigation map to clamp against, so a scene without
/// navigation behaves as though the ability were never clamped rather than dropping everything at the world
/// origin.</para>
/// </remarks>
/// <param name="pointResolver">Resolves the point to clamp.</param>
internal sealed class NavClosestPoint2DResolver(IPropertyResolver pointResolver) : IPropertyResolver
{
	private readonly IPropertyResolver _pointResolver = pointResolver;

	public Type ValueType => typeof(NumericsVector2);

	public Variant128 Resolve(GraphContext graphContext)
	{
		NumericsVector2 pointValue = _pointResolver.Resolve(graphContext).AsVector2();
		World2D? world = PhysicsQuery2D.ResolveWorld(graphContext);

		if (world is null)
		{
			return new Variant128(pointValue);
		}

		var point = new Vector2(pointValue.X, pointValue.Y);

		Rid map = world.NavigationMap;

		// A world always hands back a navigation map, whether or not anything ever built one, and a map answers every
		// closest-point query with the origin rather than refusing it. Two things have to be true before the answer
		// means anything: the map has synchronized at least once, and some region owns the point it came back with.
		// The iteration check goes first because querying an unsynchronized map is what the engine prints an error
		// about, and the engine's own message names this as the way to ask instead.
		if (NavigationServer2D.MapGetIterationId(map) == 0
			|| !NavigationServer2D.MapGetClosestPointOwner(map, point).IsValid)
		{
			return new Variant128(pointValue);
		}

		Vector2 clamped = NavigationServer2D.MapGetClosestPoint(map, point);

		return new Variant128(new NumericsVector2(clamped.X, clamped.Y));
	}
}
