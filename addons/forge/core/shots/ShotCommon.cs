// Copyright © Gamesmiths Guild.

using System.Collections.Generic;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Effects;
using Gamesmiths.Forge.Godot.Nodes;
using Gamesmiths.Forge.Godot.Resources;
using Gamesmiths.Forge.Networking;
using Gamesmiths.Forge.Shots;
using Godot;
using Godot.Collections;
using NumericsVector3 = System.Numerics.Vector3;

namespace Gamesmiths.Forge.Godot.Core.Shots;

// What the 2D and 3D shots share.
internal static class ShotCommon
{
	internal const int MaxSplashResults = 64;

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

	internal static Vector3 ToGodot(NumericsVector3 value)
	{
		return new Vector3(value.X, value.Y, value.Z);
	}

	// How far a projectile flies on from a point to the end of its lifetime, near enough: its path less what it flew.
	internal static float Left(in ProjectileFlight flight, NumericsVector3 from)
	{
		float path = flight.Velocity.Length() * (float)flight.Projectile.Lifetime;
		return path - NumericsVector3.Distance(flight.Origin, from);
	}

	// The projectiles a shot can hit, where its shooter saw them: its own where its last tick left them, the others'
	// where its player was shown them.
	internal static IEnumerable<(EntityShots Shots, ProjectileFlight Flight, NumericsVector3 Center)> Shootable(
		ForgeEntity instigator)
	{
		ForgeNetworkSession? session = instigator.Replicator ?? ForgeNetworkSession.Of(instigator);
		SimulationClock clock = ForgeManagers.Instance.Clock;
		long now = EntitySimulation.TickOf(instigator);
		double shown = session?.ShownTick() ?? (now - 1);
		World3D? world3D = ForgeShots3D.WorldOf(instigator);
		World2D? world2D = ForgeShots2D.WorldOf(instigator);

		foreach (Node node in instigator.GetTree().GetNodesInGroup(ForgeEntity.SimulationGroup))
		{
			if (node is not ForgeEntity entity
				|| ForgeShots3D.WorldOf(entity) != world3D
				|| ForgeShots2D.WorldOf(entity) != world2D)
			{
				continue;
			}

			double tick = entity == instigator ? now - 1 : shown;

			foreach (ProjectileFlight flight in entity.Shots.Flights)
			{
				if (flight.Projectile.Shootable && flight.FliesAt(tick))
				{
					yield return (entity.Shots, flight, flight.PositionAt(tick, clock.FixedDeltaTime));
				}
			}
		}
	}

	// A shot hit a projectile: it ends, struck, where it was, and blasts its struck splash. On a client, only its own
	// entity's projectiles end: another's are the server's.
	internal static void Strike(in ProjectileFlight flight, NumericsVector3 point)
	{
		if (flight.Instigator is not ForgeEntity owner || !owner.Shots.End(flight.Id, FlightEnd.Struck, point))
		{
			return;
		}

		if (ForgeProjectileData.Of(flight.Projectile)?.StruckSplash is not ForgeSplashData splash)
		{
			return;
		}

		if (ForgeShots2D.WorldOf(owner) is not null)
		{
			ForgeShots2D.Splash(owner, new Vector2(point.X, point.Y), splash);
			return;
		}

		ForgeShots3D.Splash(owner, ToGodot(point), splash);
	}
}
