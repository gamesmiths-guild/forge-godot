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
	// The most colliders one trace meets, so a shot through a stack of thin ones still ends: on the last.
	private const int MaxSteps = 32;

	// How many other colliders a shot steps past, looking back for the far side of one it went into.
	private const int ProbeSteps = 8;

	private static readonly Dictionary<float, SphereShape3D> _spheres = [];

	// A projectile in flight a shot can hit, where the shooter saw it.
	public readonly record struct Target(EntityShots Shots, ProjectileFlight Flight, System.Numerics.Vector3 Center);

	// Traces from a start along a motion, adding each hit in order, and gives where the shot stopped, if it did: on a
	// collider it may not go through, out of strength, or on a projectile. A collider it goes through is excluded from
	// the rest of the trace. The reach is how far the shot can go on from the start at most, past the motion for a
	// projectile, as far as a collider it goes into can be crossed.
	public static bool Trace(
		World3D world,
		Vector3 start,
		Vector3 motion,
		float reach,
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
				stop = default;
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
				strength * (hitbox?.StrengthScale ?? 1));

			hits.Add(shotHit);

			stop = shotHit;

			if (!GoesThrough(world, result, direction, reach - at, mask, penetration, pierces, ref strength))
			{
				return true;
			}

			pierces++;
			exclusions.Add(result.Rid);
			from = start + (direction * at);
			travelled = at;
		}

		return true;
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

	// Whether the shot goes on through what it hit: a collider on the Pierce Mask, keeping a share of its strength, or
	// one with a surface its power crosses, losing what the thickness costs.
	private static bool GoesThrough(
		World3D world,
		in RaycastResult3D result,
		Vector3 direction,
		float left,
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
			if (surface.Resistance <= 0)
			{
				return true;
			}

			// One thicker than its power crosses is solid; one the shot ends in before its far side, it pays for to
			// there.
			float crossed = strength * penetration.Power / surface.Resistance;
			float probe = Mathf.Min(crossed, left);

			if (!Thickness(world, result, direction, probe, mask, out float thickness))
			{
				if (probe >= crossed)
				{
					strength = 0;
					return false;
				}

				thickness = probe;
			}

			float cost = surface.Resistance * thickness / penetration.Power;

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
			return strength > 0;
		}

		return false;
	}

	// Whether the shot comes out of a collider within a reach, and how thick it is there: back from the reach to where
	// a ray first meets the same collider.
	private static bool Thickness(
		World3D world,
		in RaycastResult3D entry,
		Vector3 direction,
		float reach,
		uint mask,
		out float thickness)
	{
		thickness = reach;
		Vector3 beyond = entry.Position + (direction * reach);
		var skipped = new GodotRidArray();

		for (int i = 0; i < ProbeSteps; i++)
		{
			if (!PhysicsQuery3D.TryRaycast(
				world,
				beyond,
				-direction,
				reach,
				mask,
				collideWithAreas: false,
				hitFromInside: false,
				skipped,
				out RaycastResult3D exit))
			{
				return false;
			}

			if (exit.Node == entry.Node)
			{
				thickness = reach - exit.Distance;
				return true;
			}

			skipped.Add(exit.Rid);
		}

		return false;
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

		// What it stood in, which the sweep went past, is left out of the contact too.
		var restExclusions = new GodotRidArray(exclusions);
		query.Motion = Vector3.Zero;

		foreach (GodotDictionary overlap in world.DirectSpaceState.IntersectShape(query, MaxSteps))
		{
			restExclusions.Add(overlap["rid"].AsRid());
		}

		query.Transform = new Transform3D(Basis.Identity, from + (motion * fractions[1]));
		query.Exclude = restExclusions;
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
