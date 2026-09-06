// Copyright © Gamesmiths Guild.

using System;
using Gamesmiths.Forge.Godot.Core.Statescript.Physics;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;
using NumericsVector3 = System.Numerics.Vector3;

namespace Gamesmiths.Forge.Godot.Core.Statescript.Resolvers;

/// <summary>
/// Resolves the nearest point on the navigation mesh to a point.
/// </summary>
/// <remarks>
/// <para>What makes a ground-targeted ability land somewhere legal. A click on the world, a predicted intercept or a
/// point offset from a character is an arbitrary position that may be inside a wall, off a ledge or floating; this
/// clamps it onto walkable floor. A blink to the cursor, a summon placed where the player pointed, a ground-targeted
/// area that must not open inside geometry.</para>
/// <para>It answers about the mesh, not about a walk. The nearest walkable point may be across a chasm from whoever is
/// blinking to it - Nav Reachable 3D is the question that says whether they can get there, and the two compose.</para>
/// <para>The point is returned unchanged when there is no navigation map to clamp against, so a scene without
/// navigation behaves as though the ability were never clamped rather than dropping everything at the world
/// origin.</para>
/// </remarks>
/// <param name="pointResolver">Resolves the point to clamp.</param>
internal sealed class NavClosestPoint3DResolver(IPropertyResolver pointResolver) : IPropertyResolver
{
	private readonly IPropertyResolver _pointResolver = pointResolver;

	public Type ValueType => typeof(NumericsVector3);

	public Variant128 Resolve(GraphContext graphContext)
	{
		NumericsVector3 pointValue = _pointResolver.Resolve(graphContext).AsVector3();
		World3D? world = PhysicsQuery3D.ResolveWorld(graphContext);

		if (world is null)
		{
			return new Variant128(pointValue);
		}

		Vector3 clamped = NavigationServer3D.MapGetClosestPoint(
			world.NavigationMap,
			new Vector3(pointValue.X, pointValue.Y, pointValue.Z));

		return new Variant128(new NumericsVector3(clamped.X, clamped.Y, clamped.Z));
	}
}
