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

// Traces a 3D shot through what it may go through: a ray, or a sphere for a projectile with a radius.
internal static class ShotTrace3D
{
	// The most colliders one trace meets, so a shot through a stack of thin ones still ends.
	private const int MaxSteps = 32;

	// How many other colliders a shot steps past, looking back for the far side of one it went into.
	private const int ProbeSteps = 8;

	private static readonly Dictionary<float, SphereShape3D> _spheres = [];

	// A projectile in flight a shot can hit, where the shooter saw it.
	public readonly record struct Target(EntityShots Shots, ProjectileFlight Flight, System.Numerics.Vector3 Center);

	// Traces from a start along a motion, adding each hit in order, and gives where the shot stopped, if it did: on a
	// collider it may not go through, out of strength, or on a projectile. A collider it goes through is excluded from
	// the rest of the trace.
	public static bool Trace(
		World3D world,
		Vector3 start,
		Vector3 motion,
		float radius,
		uint mask,
		in ShotPenetration penetration,
		ref int pierces,
		ref float strength,
		GodotRidArray exclusions,
		IReadOnlyList<Target>? targets,
		List<ShotHit3D> hits,
		out ShotHit3D stop)
	{
		stop = default;
		float length = motion.Length();

		if (length <= Mathf.Epsilon)
		{
			return false;
		}

		Vector3 direction = motion / length;
		Target? struck = Nearest(targets, start, direction, length, out float struckAt);
		Vector3 from = start;
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
				out RaycastResult3D result);

			float at = travelled + result.Distance;

			if (struck is Target target && (!hit || struckAt <= at))
			{
				Vector3 point = start + (direction * struckAt);
				stop = new ShotHit3D(point, Vector3.Zero, struckAt, null, null, null, strength, target.Flight);
				hits.Add(stop);
				return true;
			}

			if (!hit)
			{
				return false;
			}

			var hitbox = result.Node as ForgeHitbox3D;
			var shotHit = new ShotHit3D(
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
		Vector3 start,
		Vector3 direction,
		float length,
		out float distance)
	{
		distance = float.MaxValue;
		Target? nearest = null;

		if (targets is null)
		{
			return null;
		}

		var from = new System.Numerics.Vector3(start.X, start.Y, start.Z);
		var along = new System.Numerics.Vector3(direction.X, direction.Y, direction.Z);

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
		World3D world,
		in RaycastResult3D result,
		Vector3 direction,
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

		if (result.Node is CollisionObject3D collider && (collider.CollisionLayer & penetration.PierceMask) != 0)
		{
			strength *= penetration.PierceScale;
			return true;
		}

		return false;
	}

	// How far the shot crosses a collider: back from the end of its reach to where a ray first meets the same collider.
	// One thicker than what the shot has left is as good as solid.
	private static float Thickness(
		World3D world,
		in RaycastResult3D entry,
		Vector3 direction,
		float remaining,
		uint mask)
	{
		Vector3 beyond = entry.Position + (direction * remaining);
		var skipped = new GodotRidArray();

		for (int i = 0; i < ProbeSteps; i++)
		{
			if (!PhysicsQuery3D.TryRaycast(
				world,
				beyond,
				-direction,
				remaining,
				mask,
				collideWithAreas: false,
				hitFromInside: false,
				skipped,
				out RaycastResult3D exit))
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

	// A ray, or a sphere swept from where it stands: what it already touches is left alone, as a projectile flying on
	// from inside what it went through needs.
	private static bool Cast(
		World3D world,
		Vector3 from,
		Vector3 direction,
		float length,
		float radius,
		uint mask,
		GodotRidArray exclusions,
		out RaycastResult3D result)
	{
		if (radius <= 0)
		{
			return PhysicsQuery3D.TryRaycast(
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

		if (!_spheres.TryGetValue(radius, out SphereShape3D? sphere))
		{
			sphere = new SphereShape3D { Radius = radius };
			_spheres[radius] = sphere;
		}

		Vector3 motion = direction * length;
		var query = new PhysicsShapeQueryParameters3D
		{
			Shape = sphere,
			Transform = new Transform3D(Basis.Identity, from),
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

		query.Transform = new Transform3D(Basis.Identity, from + (motion * fractions[1]));
		query.Motion = Vector3.Zero;
		GodotDictionary rest = world.DirectSpaceState.GetRestInfo(query);

		if (rest.Count == 0)
		{
			return false;
		}

		var node = GodotObject.InstanceFromId(rest["collider_id"].AsUInt64()) as Node3D;
		result = new RaycastResult3D(
			rest["point"].AsVector3(),
			rest["normal"].AsVector3(),
			node,
			ForgeEntityBridge.TryGetEntityInHierarchy(node, out IForgeEntity? entity) ? entity : null,
			length * fractions[1],
			rest["rid"].AsRid());

		return true;
	}
}
