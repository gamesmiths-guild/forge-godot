// Copyright © Gamesmiths Guild.

using System.Collections.Generic;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Godot.Core.Statescript.Physics;
using Gamesmiths.Forge.Godot.Nodes;
using Gamesmiths.Forge.Godot.Resources;
using Gamesmiths.Forge.Networking.Replication;
using Gamesmiths.Forge.Shots;
using Godot;
using GodotRidArray = Godot.Collections.Array<Godot.Rid>;
using NumericsVector3 = System.Numerics.Vector3;

namespace Gamesmiths.Forge.Godot.Core.Shots;

// Flies a 2D entity's lightweight projectiles through its world and shows them. On the server, a client's projectile
// meets the other entities where that client was shown them, fading out as it flies, by the entity's Compensation Fade.
internal sealed class ShotHost2D(ForgeEntity entity) : IShotHost
{
	// How far off the surface it hit a projectile without a radius blasts.
	private const float SplashLift = 1f;

	private readonly List<ShotHit2D> _hits = [];
	private readonly HashSet<IForgeEntity> _applied = new(ReferenceEqualityComparer.Instance);
	private readonly GodotRidArray _exclusions = [];

	public ShotStep Fly(in ProjectileFlight flight, NumericsVector3 start, NumericsVector3 finish)
	{
		if (ForgeProjectileData.Of(flight.Projectile) is not ForgeProjectileData data
			|| ForgeShots.WorldOf2D(entity) is not World2D world)
		{
			return ShotStep.Flying;
		}

		double fade = entity.CompensationFadeMs < 0 ? double.PositiveInfinity : entity.CompensationFadeMs / 1000.0;
		double flown = (EntitySimulation.TickOf(entity) - flight.Tick) * ForgeManagers.Instance.Clock.FixedDeltaTime;
		ForgeNetworkSession? session =
			ForgeShots.Compensate(entity, LagCompensation.Remaining(flown, fade), out bool opened);

		try
		{
			_hits.Clear();
			_applied.Clear();
			PhysicsQuery2D.TryCollectExclusions([entity], _exclusions);
			int pierces = flight.Pierces;
			float strength = flight.Strength;
			var from = new Vector2(start.X, start.Y);
			Vector2 motion = new Vector2(finish.X, finish.Y) - from;
			float reach = Mathf.Max(motion.Length(), ShotHost3D.Left(flight, start));

			bool stopped = ShotTrace2D.Trace(
				world,
				from,
				motion,
				reach,
				data.Radius,
				data.CollisionMask,
				data.Penetration,
				ref pierces,
				ref strength,
				_exclusions,
				null,
				_hits,
				out ShotHit2D stop);

			foreach (ShotHit2D hit in _hits)
			{
				if (hit.Entity is IForgeEntity target && target != entity && _applied.Add(target))
				{
					var at = new NumericsVector3(hit.Position.X, hit.Position.Y, 0);
					float falloff = data.FalloffAt(NumericsVector3.Distance(flight.Origin, at));
					ForgeShots.Apply(data.Effects, target, entity, hit.Strength * falloff);
				}
			}

			if (!stopped)
			{
				return ShotStep.Passed(pierces, strength);
			}

			if (data.Splash is ForgeSplashData splash)
			{
				// Off the surface, so the surface it hit doesn't shield what's around it.
				ForgeShots.Splash2D(entity, stop.Position + (stop.Normal * Mathf.Max(data.Radius, SplashLift)), splash);
			}

			return ShotStep.Impact(
				new NumericsVector3(stop.Position.X, stop.Position.Y, 0),
				new NumericsVector3(stop.Normal.X, stop.Normal.Y, 0));
		}
		finally
		{
			if (opened)
			{
				session!.EndLagCompensation();
			}
		}
	}

	public void Show(in ProjectileFlight flight)
	{
		ShotVisuals.Of(entity)?.Show(entity, flight);
	}

	public void ShowEnd(in ProjectileFlight flight)
	{
		ShotVisuals.Of(entity)?.ShowEnd(flight);
	}

	public void Hide(in ProjectileFlight flight)
	{
		ShotVisuals.Of(entity)?.Hide(flight);
	}
}
