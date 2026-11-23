// Copyright © Gamesmiths Guild.

using System;
using System.Collections.Generic;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Effects;
using Gamesmiths.Forge.Godot.Core.Statescript.Physics;
using Gamesmiths.Forge.Godot.Nodes;
using Gamesmiths.Forge.Godot.Resources;
using Gamesmiths.Forge.Networking;
using Gamesmiths.Forge.Shots;
using Godot;
using Godot.Collections;
using GodotDictionary = Godot.Collections.Dictionary;
using GodotRidArray = Godot.Collections.Array<Godot.Rid>;
using NumericsVector3 = System.Numerics.Vector3;

namespace Gamesmiths.Forge.Godot.Core.Shots;

/// <summary>
/// Shots from code: hitscans, lightweight projectiles and blasts, as the Hitscan, Launch Projectile and Splash nodes
/// fire them.
/// </summary>
/// <remarks>
/// <para>Fire them from an entity's tick, as an ability's behavior runs, so the server and the client that predicts the
/// entity fire them alike: a <see cref="RollbackBehavior{TContext}"/>'s fixed update, or a graph's node. On the server,
/// a client's shot meets the other entities where that client was shown them, and its blast too.</para>
/// <para>A shot hits what is on its line in order, going through what its penetration lets it, and applies its effects
/// once to each entity it hits, with the share of its damage it had left as float context data, scaled by a hitbox's
/// <see cref="ForgeHitbox3D.DamageScale"/>.</para>
/// </remarks>
public static partial class ForgeShots
{
	private const int MaxSplashResults = 64;

	private static readonly SphereShape3D _splashSphere = new();

	/// <summary>
	/// Fires a hitscan from an entity: its pellets spread from the entity's seeded random, each hitting what is on its
	/// line, and its effects applied to each entity a pellet hits.
	/// </summary>
	/// <param name="instigator">The entity firing.</param>
	/// <param name="origin">Where the shot starts.</param>
	/// <param name="aim">Where it aims.</param>
	/// <param name="data">The hitscan.</param>
	/// <param name="hits">Where to add what each pellet hit, in order, or <see langword="null"/>.</param>
	public static void Hitscan3D(
		ForgeEntity instigator,
		Vector3 origin,
		Vector3 aim,
		ForgeHitscanData data,
		List<ShotHit3D>? hits = null)
	{
		int pellets = Math.Max(1, data.Pellets);
		Span<NumericsVector3> directions = stackalloc NumericsVector3[pellets];
		Spread3D(instigator, aim, data.SpreadDegrees, directions);

		var pelletHits = new List<ShotHit3D>();
		var applied = new HashSet<IForgeEntity>(ReferenceEqualityComparer.Instance);
		ForgeNetworkSession? session = Compensate(instigator, 1, out bool opened);

		try
		{
			foreach (NumericsVector3 direction in directions)
			{
				pelletHits.Clear();
				applied.Clear();
				Trace(
					instigator,
					origin,
					ToGodot(direction),
					data.Range,
					data.CollisionMask,
					data.Penetration,
					data.HitsProjectiles,
					pelletHits);

				foreach (ShotHit3D hit in pelletHits)
				{
					if (hit.Entity is IForgeEntity entity && entity != instigator && applied.Add(entity))
					{
						Apply(data.Effects, entity, instigator, hit.Scale);
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
	}

	/// <summary>
	/// Traces one ray of a shot from an entity, adding what it hits in order, and applies nothing: the caller decides
	/// what the hits do. A projectile it hits ends, struck, and blasts its struck splash.
	/// </summary>
	/// <param name="instigator">The entity firing.</param>
	/// <param name="origin">Where the ray starts.</param>
	/// <param name="direction">Where it points.</param>
	/// <param name="range">How far it reaches.</param>
	/// <param name="mask">The layers it hits.</param>
	/// <param name="penetration">What it goes through.</param>
	/// <param name="hitsProjectiles">Whether it hits projectiles that can be shot.</param>
	/// <param name="hits">Where to add what it hit.</param>
	public static void Trace3D(
		ForgeEntity instigator,
		Vector3 origin,
		Vector3 direction,
		float range,
		uint mask,
		ShotPenetration penetration,
		bool hitsProjectiles,
		List<ShotHit3D> hits)
	{
		ForgeNetworkSession? session = Compensate(instigator, 1, out bool opened);

		try
		{
			Trace(instigator, origin, direction, range, mask, penetration, hitsProjectiles, hits);
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
	/// Spreads directions over a cone around an aim from an entity's seeded random, so the server and the client that
	/// predicts the entity spread them alike.
	/// </summary>
	/// <param name="instigator">The entity whose random to draw from.</param>
	/// <param name="aim">The aim.</param>
	/// <param name="angleDegrees">The cone's half angle, in degrees.</param>
	/// <param name="directions">Where to write the directions.</param>
	public static void Spread3D(
		ForgeEntity instigator,
		Vector3 aim,
		float angleDegrees,
		Span<NumericsVector3> directions)
	{
		Vector3 normalized = aim.Normalized();
		ShotSpread.Spread(
			instigator.Random,
			new NumericsVector3(normalized.X, normalized.Y, normalized.Z),
			Mathf.DegToRad(angleDegrees),
			directions);
	}

	/// <summary>
	/// Launches a lightweight projectile from an entity, in its current tick.
	/// </summary>
	/// <param name="instigator">The entity launching it.</param>
	/// <param name="data">The projectile.</param>
	/// <param name="origin">Where it starts.</param>
	/// <param name="direction">Where it flies.</param>
	/// <param name="speed">How fast, or <see langword="null"/> for the projectile's
	/// <see cref="ForgeProjectileData.Speed"/>.</param>
	/// <returns>The flight.</returns>
	public static ProjectileFlight Launch3D(
		ForgeEntity instigator,
		ForgeProjectileData data,
		Vector3 origin,
		Vector3 direction,
		float? speed = null)
	{
		Vector3 velocity = direction.Normalized() * (speed ?? data.Speed);
		return instigator.Shots.Launch(
			data.GetProjectileData(),
			new NumericsVector3(origin.X, origin.Y, origin.Z),
			new NumericsVector3(velocity.X, velocity.Y, velocity.Z));
	}

	/// <summary>
	/// Blasts from a point: each entity in reach gets the blast's effects and push, scaled by how close it is.
	/// </summary>
	/// <param name="instigator">The entity the blast is from.</param>
	/// <param name="center">Where it blasts.</param>
	/// <param name="data">The blast.</param>
	/// <param name="hits">Where to add the entities it reached, or <see langword="null"/>.</param>
	public static void Splash3D(
		ForgeEntity instigator,
		Vector3 center,
		ForgeSplashData data,
		List<SplashHit3D>? hits = null)
	{
		if (WorldOf(instigator) is not World3D world)
		{
			return;
		}

		ForgeNetworkSession? session = Compensate(instigator, 1, out bool opened);

		try
		{
			_splashSphere.Radius = data.Radius;
			var query = new PhysicsShapeQueryParameters3D
			{
				Shape = _splashSphere,
				Transform = new Transform3D(Basis.Identity, center),
				CollisionMask = data.CollisionMask,
			};

			LagCompensationProxies.Refresh();
			var reached = new HashSet<IForgeEntity>(ReferenceEqualityComparer.Instance);
			var exclusions = new GodotRidArray();

			foreach (GodotDictionary overlap in world.DirectSpaceState.IntersectShape(query, MaxSplashResults))
			{
				if (!ForgeEntityBridge.TryGetEntityInHierarchy(overlap["collider"].As<Node>(), out IForgeEntity? entity)
					|| !reached.Add(entity)
					|| (entity == instigator && !data.AffectsInstigator)
					|| !ForgeEntityBridge.TryGetSpatialNode3D(entity, out Node3D? body))
				{
					continue;
				}

				Vector3 position = CenterOf(session, entity, body);

				if (data.BlockedByWalls
					&& PhysicsQuery3D.TryCollectExclusions([entity], exclusions)
					&& !PhysicsQuery3D.TryLineOfSight(world, center, position, data.CollisionMask, exclusions, out _))
				{
					continue;
				}

				float distance = center.DistanceTo(position);
				float scale = data.ScaleAt(distance);
				float effectScale = entity == instigator ? scale * data.InstigatorScale : scale;
				Vector3 away = position - center;
				away = away.IsZeroApprox() ? Vector3.Up : away.Normalized();
				Vector3 push = ((away * (1 - data.KnockbackLift)) + (Vector3.Up * data.KnockbackLift)).Normalized()
					* data.Knockback * scale;

				Apply(data.Effects, entity, instigator, effectScale);

				if (data.Knockback > 0 && entity is ForgeEntity { Mover: ForgeCharacterMovement3D movement })
				{
					movement.Knockback(push);
				}

				hits?.Add(new SplashHit3D(entity, position, distance, effectScale, push));
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

	internal static void Apply(
		Array<ForgeEffectData> effects,
		IForgeEntity target,
		IForgeEntity instigator,
		float scale)
	{
		foreach (ForgeEffectData data in effects)
		{
			target.EffectsManager.ApplyEffect(
				new Effect(data.GetEffectData(), new EffectOwnership(instigator, instigator)),
				scale);
		}
	}

	// The compensation for what an entity does now, opened unless one is open already.
	internal static ForgeNetworkSession? Compensate(ForgeEntity instigator, double remaining, out bool opened)
	{
		ForgeNetworkSession? session = instigator.Replicator ?? ForgeNetworkSession.Of(instigator);
		opened = session?.BeginActionOf(instigator, remaining) == true;
		return session;
	}

	internal static World3D? WorldOf(ForgeEntity entity)
	{
		return ForgeEntityBridge.TryGetSpatialNode3D(entity, out Node3D? node) ? node.GetWorld3D() : null;
	}

	internal static Vector3 ToGodot(NumericsVector3 value)
	{
		return new Vector3(value.X, value.Y, value.Z);
	}

	private static void Trace(
		ForgeEntity instigator,
		Vector3 origin,
		Vector3 direction,
		float range,
		uint mask,
		in ShotPenetration penetration,
		bool hitsProjectiles,
		List<ShotHit3D> hits)
	{
		if (WorldOf(instigator) is not World3D world || direction.IsZeroApprox())
		{
			return;
		}

		var exclusions = new GodotRidArray();
		PhysicsQuery3D.TryCollectExclusions([instigator], exclusions);
		int pierces = 0;
		float strength = 1;

		if (ShotTrace3D.Trace(
			world,
			origin,
			direction.Normalized() * range,
			0,
			mask,
			penetration,
			ref pierces,
			ref strength,
			exclusions,
			hitsProjectiles ? Targets(instigator) : null,
			hits,
			out ShotHit3D stop)
			&& stop.Flight is ProjectileFlight struck)
		{
			Strike(struck, new NumericsVector3(stop.Position.X, stop.Position.Y, stop.Position.Z));
		}
	}

	// The projectiles a shot can hit, where its shooter saw them: its own where its last tick left them, the others'
	// where its player was shown them.
	private static IEnumerable<(EntityShots Shots, ProjectileFlight Flight, NumericsVector3 Center)> Shootable(
		ForgeEntity instigator)
	{
		ForgeNetworkSession? session = instigator.Replicator ?? ForgeNetworkSession.Of(instigator);
		SimulationClock clock = ForgeManagers.Instance.Clock;
		double shown = session?.ShownTick() ?? (clock.Tick - 1);

		foreach (Node node in instigator.GetTree().GetNodesInGroup(ForgeEntity.SimulationGroup))
		{
			if (node is not ForgeEntity entity)
			{
				continue;
			}

			double tick = entity == instigator ? clock.Tick - 1 : shown;

			foreach (ProjectileFlight flight in entity.Shots.Flights)
			{
				if (flight.Projectile.Shootable && flight.FliesAt(tick))
				{
					yield return (entity.Shots, flight, flight.PositionAt(tick, clock.FixedDeltaTime));
				}
			}
		}
	}

	private static List<ShotTrace3D.Target> Targets(ForgeEntity instigator)
	{
		var targets = new List<ShotTrace3D.Target>();

		foreach ((EntityShots shots, ProjectileFlight flight, NumericsVector3 center) in Shootable(instigator))
		{
			targets.Add(new ShotTrace3D.Target(shots, flight, center));
		}

		return targets;
	}

	// A shot hit a projectile: it ends, struck, where it was, and blasts its struck splash. On a client, only its own
	// entity's projectiles end: another's are the server's.
	private static void Strike(in ProjectileFlight flight, NumericsVector3 point)
	{
		if (flight.Instigator is not ForgeEntity owner || !owner.Shots.End(flight.Id, FlightEnd.Struck, point))
		{
			return;
		}

		if (ForgeProjectileData.Of(flight.Projectile)?.StruckSplash is not ForgeSplashData splash)
		{
			return;
		}

		if (WorldOf2D(owner) is not null)
		{
			Splash2D(owner, new Vector2(point.X, point.Y), splash);
			return;
		}

		Splash3D(owner, ToGodot(point), splash);
	}

	// The center of an entity's body where the open compensation put it: its first collision shape's place on the body.
	private static Vector3 CenterOf(ForgeNetworkSession? session, IForgeEntity entity, Node3D body)
	{
		Vector3 origin = body.GlobalPosition;

		if (session?.TryGetShownBody(entity, out BodyState shown) == true)
		{
			origin = ToGodot(shown.Position);
		}

		foreach (Node child in body.GetChildren())
		{
			if (child is CollisionShape3D shape)
			{
				return origin + (body.GlobalBasis * shape.Position);
			}
		}

		return origin;
	}
}
