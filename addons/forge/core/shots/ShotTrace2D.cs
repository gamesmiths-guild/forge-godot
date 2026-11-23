// Copyright © Gamesmiths Guild.

using System.Collections.Generic;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Godot.Core.Statescript.Physics;
using Gamesmiths.Forge.Godot.Nodes;
using Gamesmiths.Forge.Godot.Resources;
using Gamesmiths.Forge.Shots;
using Godot;
using GodotDictionary = Godot.Collections.Dictionary;
using GodotRidArray = Godot.Collections.Array<Godot.Rid>;

namespace Gamesmiths.Forge.Godot.Core.Shots;

// Traces a 2D shot through what it may go through: a ray, or a circle for a projectile with a radius.
internal static class ShotTrace2D
{
	// The most colliders one trace meets, so a shot through a stack of thin ones still ends.
	private const int MaxSteps = 32;

	// How many other colliders a shot steps past, looking back for the far side of one it went into.
	private const int ProbeSteps = 8;

	private static readonly Dictionary<float, CircleShape2D> _circles = [];

	// A projectile in flight a shot can hit, where the shooter saw it.
	public readonly record struct Target(EntityShots Shots, ProjectileFlight Flight, System.Numerics.Vector3 Center);

	// Traces from a start along a motion, adding each hit in order, and gives where the shot stopped, if it did: on a
	// collider it may not go through, out of strength, or on a projectile. A collider it goes through is excluded from
	// the rest of the trace.
	public static bool Trace(
		World2D world,
		Vector2 start,
		Vector2 motion,
		float radius,
		uint mask,
		in ShotPenetration penetration,
		ref int pierces,
		ref float strength,
		GodotRidArray exclusions,
		IReadOnlyList<Target>? targets,
		List<ShotHit2D> hits,
		out ShotHit2D stop)
	{
		stop = default;
		float length = motion.Length();

		if (length <= Mathf.Epsilon)
		{
			return false;
		}

		Vector2 direction = motion / length;
		Target? struck = Nearest(targets, start, direction, length, out float struckAt);
		Vector2 from = start;
		float travelled = 0;

		for (int step = 0; step < MaxSteps; step++)
		{
			bool hit = Cast(
				world,
				from,
				direction,
				length - travelled,
				radius,
				mask,
				exclusions,
				out RaycastResult2D result);

			float at = travelled + result.Distance;

			if (struck is Target target && (!hit || struckAt <= at))
			{
				Vector2 point = start + (direction * struckAt);
				stop = new ShotHit2D(point, Vector2.Zero, struckAt, null, null, null, strength, target.Flight);
				hits.Add(stop);
				return true;
			}

			if (!hit)
			{
				return false;
			}

			var hitbox = result.Node as ForgeHitbox2D;
			var shotHit = new ShotHit2D(
				result.Position,
				result.Normal,
				at,
				result.Node,
				result.Entity,
				hitbox,
				strength * (hitbox?.DamageScale ?? 1));

			hits.Add(shotHit);

			if (!GoesThrough(world, result, direction, length - at, mask, penetration, pierces, ref strength))
			{
				stop = shotHit;
				return true;
			}

			pierces++;
			exclusions.Add(result.Rid);
			from = result.Position;
			travelled = at;
		}

		return false;
	}

	// The nearest projectile on the line, ahead of the start and within its length.
	private static Target? Nearest(
		IReadOnlyList<Target>? targets,
		Vector2 start,
		Vector2 direction,
		float length,
		out float distance)
	{
		distance = float.MaxValue;
		Target? nearest = null;

		if (targets is null)
		{
			return null;
		}

		var from = new System.Numerics.Vector3(start.X, start.Y, 0);
		var along = new System.Numerics.Vector3(direction.X, direction.Y, 0);

		foreach (Target target in targets)
		{
			if (target.Flight.TestRay(from, along, length, target.Center, out float at) && at < distance)
			{
				distance = at;
				nearest = target;
			}
		}

		return nearest;
	}

	// Whether the shot goes on through what it hit: a collider on the Pierce Mask, keeping a share of its damage, or
	// one with a surface its power crosses, losing what the thickness costs.
	private static bool GoesThrough(
		World2D world,
		in RaycastResult2D result,
		Vector2 direction,
		float remaining,
		uint mask,
		in ShotPenetration penetration,
		int pierces,
		ref float strength)
	{
		if (penetration.MaxPierces >= 0 && pierces >= penetration.MaxPierces)
		{
			return false;
		}

		ForgeShotSurface? surface = penetration.Power > 0 ? ForgeShotSurface.Of(result.Node) : null;

		if (surface is not null)
		{
			float cost = surface.Resistance * Thickness(world, result, direction, remaining, mask) / penetration.Power;

			if (cost >= strength)
			{
				strength = 0;
				return false;
			}

			strength -= cost;
			return true;
		}

		if (result.Node is CollisionObject2D collider && (collider.CollisionLayer & penetration.PierceMask) != 0)
		{
			strength *= penetration.PierceScale;
			return true;
		}

		return false;
	}

	// How far the shot crosses a collider: back from the end of its reach to where a ray first meets the same collider.
	// One thicker than what the shot has left is as good as solid.
	private static float Thickness(
		World2D world,
		in RaycastResult2D entry,
		Vector2 direction,
		float remaining,
		uint mask)
	{
		Vector2 beyond = entry.Position + (direction * remaining);
		var skipped = new GodotRidArray();

		for (int i = 0; i < ProbeSteps; i++)
		{
			if (!PhysicsQuery2D.TryRaycast(
				world,
				beyond,
				-direction,
				remaining,
				mask,
				collideWithAreas: false,
				hitFromInside: false,
				skipped,
				out RaycastResult2D exit))
			{
				return remaining;
			}

			if (exit.Node == entry.Node)
			{
				return remaining - exit.Distance;
			}

			skipped.Add(exit.Rid);
		}

		return remaining;
	}

	// A ray, or a circle swept from where it stands: what it already touches is left alone, as a projectile flying on
	// from inside what it went through needs.
	private static bool Cast(
		World2D world,
		Vector2 from,
		Vector2 direction,
		float length,
		float radius,
		uint mask,
		GodotRidArray exclusions,
		out RaycastResult2D result)
	{
		if (radius <= 0)
		{
			return PhysicsQuery2D.TryRaycast(
				world,
				from,
				direction,
				length,
				mask,
				collideWithAreas: false,
				hitFromInside: false,
				exclusions,
				out result);
		}

		result = default;

		if (!_circles.TryGetValue(radius, out CircleShape2D? circle))
		{
			circle = new CircleShape2D { Radius = radius };
			_circles[radius] = circle;
		}

		Vector2 motion = direction * length;
		var query = new PhysicsShapeQueryParameters2D
		{
			Shape = circle,
			Transform = new Transform2D(0, from),
			Motion = motion,
			CollisionMask = mask,
			Exclude = exclusions,
		};

		LagCompensationProxies.Refresh();
		float[] fractions = world.DirectSpaceState.CastMotion(query);

		if (fractions.Length < 2 || fractions[1] >= 1)
		{
			return false;
		}

		query.Transform = new Transform2D(0, from + (motion * fractions[1]));
		query.Motion = Vector2.Zero;
		GodotDictionary rest = world.DirectSpaceState.GetRestInfo(query);

		if (rest.Count == 0)
		{
			return false;
		}

		var node = GodotObject.InstanceFromId(rest["collider_id"].AsUInt64()) as Node2D;
		result = new RaycastResult2D(
			rest["point"].AsVector2(),
			rest["normal"].AsVector2(),
			node,
			ForgeEntityBridge.TryGetEntityInHierarchy(node, out IForgeEntity? entity) ? entity : null,
			length * fractions[1],
			rest["rid"].AsRid());

		return true;
	}
}
