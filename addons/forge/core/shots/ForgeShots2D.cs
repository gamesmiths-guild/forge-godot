// Copyright © Gamesmiths Guild.

using System;
using System.Collections.Generic;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Godot.Core.Statescript.Physics;
using Gamesmiths.Forge.Godot.Nodes;
using Gamesmiths.Forge.Godot.Resources;
using Gamesmiths.Forge.Shots;
using Godot;
using GodotDictionary = Godot.Collections.Dictionary;
using GodotOverlaps = Godot.Collections.Array<Godot.Collections.Dictionary>;
using GodotRidArray = Godot.Collections.Array<Godot.Rid>;
using NumericsVector2 = System.Numerics.Vector2;
using NumericsVector3 = System.Numerics.Vector3;

namespace Gamesmiths.Forge.Godot.Core.Shots;

/// <summary>
/// 2D shots from code: hitscans, lightweight projectiles and blasts, as the Hitscan 2D, Launch Projectile 2D and Splash
/// 2D nodes fire them.
/// </summary>
/// <remarks>
/// <para>Fire them from an entity's tick, as an ability's behavior runs, so the server and the client that predicts the
/// entity fire them alike: a <see cref="RollbackBehavior{TContext}"/>'s fixed update, or a graph's node. On the server,
/// a client's shot meets the other entities where that client was shown them, and its blast too.</para>
/// <para>A shot hits what is on its line in order, going through what its penetration lets it, and applies its effects
/// once to each entity it hits, with the share of its strength it had left as float context data, scaled by a hitbox's
/// <see cref="ForgeHitbox2D.StrengthScale"/> and by the shot's falloff at the distance from where it started.</para>
/// </remarks>
public static class ForgeShots2D
{
	private static readonly CircleShape2D _splashCircle = new();

	/// <summary>
	/// Fires a 2D hitscan from an entity: its pellets spread from the entity's seeded random, each hitting what is on
	/// its line, and its effects applied to each entity a pellet hits.
	/// </summary>
	/// <param name="instigator">The entity firing.</param>
	/// <param name="origin">Where the shot starts.</param>
	/// <param name="aim">Where it aims.</param>
	/// <param name="data">The hitscan.</param>
	/// <param name="hits">Where to add what each pellet hit, in order, or <see langword="null"/>.</param>
	/// <returns>What the first pellet stopped on, or <see langword="null"/> if it reached the end of its range.
	/// </returns>
	public static ShotHit2D? Hitscan(
		ForgeEntity instigator,
		Vector2 origin,
		Vector2 aim,
		ForgeHitscanData data,
		List<ShotHit2D>? hits = null)
	{
		int pellets = Math.Max(1, data.Pellets);
		Span<NumericsVector2> directions = stackalloc NumericsVector2[pellets];
		Spread(instigator, aim, data.SpreadDegrees, directions);

		var pelletHits = new List<ShotHit2D>();
		var applied = new HashSet<IForgeEntity>(ReferenceEqualityComparer.Instance);
		ShotHit2D? first = null;
		ForgeNetworkSession? session = ShotCommon.Compensate(instigator, 1, out bool opened);

		try
		{
			for (int i = 0; i < pellets; i++)
			{
				pelletHits.Clear();
				applied.Clear();
				ShotHit2D? stop = Fire(
					instigator,
					origin,
					new Vector2(directions[i].X, directions[i].Y),
					data.Range,
					data.CollisionMask,
					data.Penetration,
					data.HitsProjectiles,
					pelletHits);

				for (int j = 0; j < pelletHits.Count; j++)
				{
					ShotHit2D hit = pelletHits[j];
					pelletHits[j] = hit with { Strength = hit.Strength * data.FalloffAt(hit.Distance) };
				}

				// What it stopped on is its last hit.
				if (i == 0 && stop is not null)
				{
					first = pelletHits[^1];
				}

				foreach (ShotHit2D hit in pelletHits)
				{
					if (hit.Entity is IForgeEntity entity && entity != instigator && applied.Add(entity))
					{
						ShotCommon.Apply(data.Effects, entity, instigator, hit.Strength);
					}
				}

				hits?.AddRange(pelletHits);
			}
		}
		finally
		{
			if (opened)
			{
				session!.EndLagCompensation();
			}
		}

		return first;
	}

	/// <summary>
	/// Traces one ray of a 2D shot from an entity, adding what it hits in order, and applies nothing. A projectile it
	/// hits ends, struck, and blasts its struck splash.
	/// </summary>
	/// <param name="instigator">The entity firing.</param>
	/// <param name="origin">Where the ray starts.</param>
	/// <param name="direction">Where it points.</param>
	/// <param name="range">How far it reaches.</param>
	/// <param name="mask">The layers it hits.</param>
	/// <param name="penetration">What it goes through.</param>
	/// <param name="hitsProjectiles">Whether it hits projectiles that can be shot.</param>
	/// <param name="hits">Where to add what it hit.</param>
	public static void Trace(
		ForgeEntity instigator,
		Vector2 origin,
		Vector2 direction,
		float range,
		uint mask,
		ShotPenetration penetration,
		bool hitsProjectiles,
		List<ShotHit2D> hits)
	{
		ForgeNetworkSession? session = ShotCommon.Compensate(instigator, 1, out bool opened);

		try
		{
			Fire(instigator, origin, direction, range, mask, penetration, hitsProjectiles, hits);
		}
		finally
		{
			if (opened)
			{
				session!.EndLagCompensation();
			}
		}
	}

	/// <summary>
	/// Spreads directions over an arc around an aim from an entity's seeded random.
	/// </summary>
	/// <param name="instigator">The entity whose random to draw from.</param>
	/// <param name="aim">The aim.</param>
	/// <param name="angleDegrees">The arc's half angle, in degrees.</param>
	/// <param name="directions">Where to write the directions.</param>
	public static void Spread(
		ForgeEntity instigator,
		Vector2 aim,
		float angleDegrees,
		Span<NumericsVector2> directions)
	{
		Vector2 normalized = aim.Normalized();
		ShotSpread.Spread2D(
			instigator.Random,
			new NumericsVector2(normalized.X, normalized.Y),
			Mathf.DegToRad(angleDegrees),
			directions);
	}

	/// <summary>
	/// Launches a 2D lightweight projectile from an entity, in its current tick.
	/// </summary>
	/// <param name="instigator">The entity launching it.</param>
	/// <param name="data">The projectile.</param>
	/// <param name="origin">Where it starts.</param>
	/// <param name="direction">Where it flies.</param>
	/// <param name="speed">How fast, or <see langword="null"/> for the projectile's
	/// <see cref="ForgeProjectileData.Speed"/>.</param>
	/// <returns>The flight.</returns>
	public static ProjectileFlight Launch(
		ForgeEntity instigator,
		ForgeProjectileData data,
		Vector2 origin,
		Vector2 direction,
		float? speed = null)
	{
		Vector2 velocity = direction.Normalized() * (speed ?? data.Speed);
		return instigator.Shots.Launch(
			data.GetProjectileData(),
			new NumericsVector3(origin.X, origin.Y, 0),
			new NumericsVector3(velocity.X, velocity.Y, 0));
	}

	/// <summary>
	/// Blasts from a point in 2D: each entity in reach gets the blast's effects and push, scaled by how close it is.
	/// </summary>
	/// <param name="instigator">The entity the blast is from.</param>
	/// <param name="center">Where it blasts.</param>
	/// <param name="data">The blast.</param>
	/// <param name="hits">Where to add the entities it reached, or <see langword="null"/>.</param>
	public static void Splash(
		ForgeEntity instigator,
		Vector2 center,
		ForgeSplashData data,
		List<SplashHit2D>? hits = null)
	{
		if (WorldOf(instigator) is not World2D world)
		{
			return;
		}

		ForgeNetworkSession? session = ShotCommon.Compensate(instigator, 1, out bool opened);

		try
		{
			_splashCircle.Radius = data.Radius;
			var query = new PhysicsShapeQueryParameters2D
			{
				Shape = _splashCircle,
				Transform = new Transform2D(0, center),
				CollisionMask = data.CollisionMask,
			};

			LagCompensationProxies.Refresh();
			var reached = new HashSet<IForgeEntity>(ReferenceEqualityComparer.Instance);
			var exclusions = new GodotRidArray();

			foreach (GodotDictionary overlap in Overlaps(world, query))
			{
				if (!ForgeEntityBridge.TryGetEntityInHierarchy(overlap["collider"].As<Node>(), out IForgeEntity? entity)
					|| !reached.Add(entity)
					|| (entity == instigator && !data.AffectsInstigator)
					|| !ForgeEntityBridge.TryGetSpatialNode2D(entity, out Node2D? body))
				{
					continue;
				}

				Vector2 position = CenterOf(session, entity, body);

				if (data.BlockedByWalls
					&& PhysicsQuery2D.TryCollectExclusions([entity], exclusions)
					&& !PhysicsQuery2D.TryLineOfSight(world, center, position, data.CollisionMask, exclusions, out _))
				{
					continue;
				}

				float distance = center.DistanceTo(position);
				float scale = data.ScaleAt(distance);
				float effectScale = entity == instigator ? scale * data.InstigatorScale : scale;
				Vector2 away = position - center;
				away = away.IsZeroApprox() ? Vector2.Up : away.Normalized();
				Vector2 push = ((away * (1 - data.KnockbackLift)) + (Vector2.Up * data.KnockbackLift)).Normalized()
					* data.Knockback * scale;

				ShotCommon.Apply(data.Effects, entity, instigator, effectScale);

				if (data.Knockback > 0 && entity is ForgeEntity { Mover: ForgeCharacterMovement2D movement })
				{
					movement.Knockback(push);
				}

				hits?.Add(new SplashHit2D(entity, position, distance, effectScale, push));
			}
		}
		finally
		{
			if (opened)
			{
				session!.EndLagCompensation();
			}
		}
	}

	internal static World2D? WorldOf(ForgeEntity entity)
	{
		return ForgeEntityBridge.TryGetSpatialNode2D(entity, out Node2D? node) ? node.GetWorld2D() : null;
	}

	private static GodotOverlaps Overlaps(World2D world, PhysicsShapeQueryParameters2D query)
	{
		int max = ShotCommon.MaxSplashResults;
		GodotOverlaps overlaps = world.DirectSpaceState.IntersectShape(query, max);

		while (overlaps.Count == max)
		{
			max *= 2;
			overlaps = world.DirectSpaceState.IntersectShape(query, max);
		}

		return overlaps;
	}

	private static ShotHit2D? Fire(
		ForgeEntity instigator,
		Vector2 origin,
		Vector2 direction,
		float range,
		uint mask,
		in ShotPenetration penetration,
		bool hitsProjectiles,
		List<ShotHit2D> hits)
	{
		if (WorldOf(instigator) is not World2D world || direction.IsZeroApprox())
		{
			return null;
		}

		var exclusions = new GodotRidArray();
		PhysicsQuery2D.TryCollectExclusions([instigator], exclusions);
		int pierces = 0;
		float strength = 1;

		if (!ShotTrace2D.Trace(
			world,
			origin,
			direction.Normalized() * range,
			range,
			0,
			mask,
			penetration,
			ref pierces,
			ref strength,
			exclusions,
			hitsProjectiles ? Targets(instigator) : null,
			hits,
			out ShotHit2D stop))
		{
			return null;
		}

		if (stop.Flight is ProjectileFlight struck)
		{
			ShotCommon.Strike(struck, new NumericsVector3(stop.Position.X, stop.Position.Y, 0));
		}

		return stop;
	}

	private static List<ShotTrace2D.Target> Targets(ForgeEntity instigator)
	{
		var targets = new List<ShotTrace2D.Target>();

		foreach ((EntityShots shots, ProjectileFlight flight, NumericsVector3 center)
			in ShotCommon.Shootable(instigator))
		{
			targets.Add(new ShotTrace2D.Target(shots, flight, center));
		}

		return targets;
	}

	private static Vector2 CenterOf(ForgeNetworkSession? session, IForgeEntity entity, Node2D body)
	{
		Transform2D transform = body.GlobalTransform;

		if (session?.TryGetShownBody(entity, out BodyState shown) == true)
		{
			transform = new Transform2D(
				2 * Mathf.Atan2(shown.Rotation.Z, shown.Rotation.W),
				body.GlobalScale,
				body.GlobalSkew,
				new Vector2(shown.Position.X, shown.Position.Y));
		}

		foreach (Node child in body.GetChildren())
		{
			if (child is CollisionShape2D shape)
			{
				return transform * shape.Position;
			}
		}

		return transform.Origin;
	}
}
