// Copyright © Gamesmiths Guild.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Gamesmiths.Forge.Abilities;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Statescript;
using Godot;
using GodotDictionary = Godot.Collections.Dictionary;
using GodotRidArray = Godot.Collections.Array<Godot.Rid>;
using Node = Godot.Node;

namespace Gamesmiths.Forge.Godot.Core.Statescript.Physics;

/// <summary>
/// The physics queries the Statescript nodes and resolvers run, in one place so a ray cast from a Condition node and a
/// ray cast from a line-of-sight resolver answer the same question the same way.
/// </summary>
internal static class PhysicsQuery3D
{
	/// <summary>
	/// How many colliders one shape query reports. Godot's own default is 32; an area of effect landing in a crowd
	/// hits more than that often enough to be worth the larger buffer, and the cost of an unused slot is one pointer.
	/// </summary>
	public const int MaxResults = 64;

	// What a swept shape is allowed to be inside by before the contact test at the end of the sweep counts it. The
	// sweep stops the shape exactly touching, and an exact touch is the one distance floating point cannot be trusted
	// to report as a contact. Godot's own default collision margin, for the same reason.
	private const float ContactMargin = 0.04f;

	/// <summary>
	/// Turns an authored mask into the one the physics server is given.
	/// </summary>
	/// <remarks>
	/// Zero means every layer. A mask of zero can never find anything, so it is never a useful authored value, and
	/// reading it literally would make an unbound or unset mask silently disable the query.
	/// </remarks>
	/// <param name="mask">The authored mask.</param>
	/// <returns>The mask to query with.</returns>
	public static uint ResolveMask(int mask)
	{
		return mask == 0 ? uint.MaxValue : unchecked((uint)mask);
	}

	/// <summary>
	/// Gets the physics world a graph queries: the one its ability's owner is standing in.
	/// </summary>
	/// <remarks>
	/// Read from the owner rather than from the main scene tree so a game running its world inside a sub-viewport -
	/// split screen, a preview window - queries the space its entities actually live in.
	/// </remarks>
	/// <param name="graphContext">The graph execution context.</param>
	/// <returns>The world, or <see langword="null"/> when the graph has no owner in the scene.</returns>
	public static World3D? ResolveWorld(GraphContext graphContext)
	{
		return TryResolveContextNode(graphContext, out Node3D? ownerNode) ? ownerNode.GetWorld3D() : null;
	}

	/// <summary>
	/// Gets the node a graph's queries are anchored to: the spatial node its ability's owner lives on.
	/// </summary>
	/// <remarks>
	/// This is what answers "which world" and "which viewport" for everything a graph asks of physics, including where
	/// its debug geometry is drawn.
	/// </remarks>
	/// <param name="graphContext">The graph execution context.</param>
	/// <param name="contextNode">The owner's spatial node, when it has one.</param>
	/// <returns><see langword="true"/> if the graph has an owner in the scene; <see langword="false"/> otherwise.
	/// </returns>
	public static bool TryResolveContextNode(
		GraphContext graphContext,
		[NotNullWhen(true)] out Node3D? contextNode)
	{
		if (graphContext.TryGetActivationContext(out AbilityBehaviorContext? abilityContext))
		{
			return ForgeEntityBridge.TryGetSpatialNode3D(abilityContext.Owner, out contextNode);
		}

		contextNode = null;
		return false;
	}

	/// <summary>
	/// Gets whether a ray with these inputs describes a segment at all, which is what <see cref="TryRaycast"/> requires
	/// before it queries anything.
	/// </summary>
	/// <remarks>
	/// Exposed so a caller that draws the cast can ask the same question the query asks. A ray that fails this covers
	/// no distance, and a debug segment built from it would point somewhere the query never looked - backwards, for a
	/// negative distance.
	/// </remarks>
	/// <param name="direction">The direction the ray travels.</param>
	/// <param name="maxDistance">How far the ray reaches.</param>
	/// <returns><see langword="true"/> if the ray can be cast; <see langword="false"/> otherwise.</returns>
	public static bool IsCastable(Vector3 direction, float maxDistance)
	{
		return direction.LengthSquared() > 0.000001f && maxDistance > 0;
	}

	/// <summary>
	/// Casts a ray and reports what it met.
	/// </summary>
	/// <param name="world">The world to query.</param>
	/// <param name="origin">Where the ray starts.</param>
	/// <param name="direction">Which way the ray points. Normalized here, so an unnormalized input is not a bug.
	/// </param>
	/// <param name="maxDistance">How far the ray reaches.</param>
	/// <param name="mask">The physics layers the ray can hit.</param>
	/// <param name="collideWithAreas">Whether areas count as hits, as well as bodies.</param>
	/// <param name="hitFromInside">Whether a ray starting inside a shape reports that shape.</param>
	/// <param name="exclude">Collision object RIDs the ray ignores, for casting out of a body without hitting it.
	/// </param>
	/// <param name="result">What the ray met, when it met anything.</param>
	/// <returns><see langword="true"/> if the ray hit something; <see langword="false"/> otherwise.</returns>
	public static bool TryRaycast(
		World3D world,
		Vector3 origin,
		Vector3 direction,
		float maxDistance,
		uint mask,
		bool collideWithAreas,
		bool hitFromInside,
		GodotRidArray? exclude,
		out RaycastResult3D result)
	{
		result = default;

		if (!IsCastable(direction, maxDistance))
		{
			return false;
		}

		Vector3 target = origin + (direction.Normalized() * maxDistance);

		var query = PhysicsRayQueryParameters3D.Create(origin, target, mask);
		query.CollideWithAreas = collideWithAreas;
		query.HitFromInside = hitFromInside;

		if (exclude is not null)
		{
			query.Exclude = exclude;
		}

		GodotDictionary hit = world.DirectSpaceState.IntersectRay(query);

		if (hit.Count == 0)
		{
			return false;
		}

		Vector3 position = hit["position"].AsVector3();
		Node3D collider = hit["collider"].As<Node3D>();

		result = new RaycastResult3D(
			position,
			hit["normal"].AsVector3(),
			collider,
			ForgeEntityBridge.TryGetEntityInHierarchy(collider, out IForgeEntity? entity) ? entity : null,
			origin.DistanceTo(position));

		return true;
	}

	/// <summary>
	/// Reports whether nothing on the mask stands between two points.
	/// </summary>
	/// <remarks>
	/// This is a ray asked the other way round. A raycast wants to know what it met; a line of sight wants to know that
	/// it met nothing, and reports the blocker only so a graph can react to what got in the way.
	/// </remarks>
	/// <param name="world">The world to query.</param>
	/// <param name="from">Where the line starts.</param>
	/// <param name="to">Where the line ends.</param>
	/// <param name="mask">The physics layers that block sight.</param>
	/// <param name="exclude">Collision object RIDs the line passes through.</param>
	/// <param name="blocker">What got in the way, when something did.</param>
	/// <returns><see langword="true"/> if the line is clear; <see langword="false"/> otherwise.</returns>
	public static bool TryLineOfSight(
		World3D world,
		Vector3 from,
		Vector3 to,
		uint mask,
		GodotRidArray? exclude,
		out RaycastResult3D blocker)
	{
		blocker = default;

		Vector3 offset = to - from;

		// Two points in the same place always see each other, and a zero-length ray is not a question physics can
		// answer.
		if (offset.LengthSquared() <= 0.000001f)
		{
			return true;
		}

		return !TryRaycast(
			world,
			from,
			offset,
			offset.Length(),
			mask,
			collideWithAreas: false,
			hitFromInside: false,
			exclude,
			out blocker);
	}

	/// <summary>
	/// Collects the RIDs of several entities' collision objects, so a query can pass through all of them.
	/// </summary>
	/// <remarks>
	/// <para>Excluding by RID is the only reliable way to keep a query off the things it starts and ends on. A ray that
	/// begins at a character's own origin is a boundary case for <c>hitFromInside</c>: a <see cref="CharacterBody3D"/>
	/// sits at its feet, so a ray from there grazes the bottom of its own capsule and reports a hit at zero distance.
	/// The far end has the same problem in reverse — a line drawn to a marker inside someone is stopped by the body
	/// that marker belongs to — which is why this takes a list rather than one entity.</para>
	/// <para>Each entity's whole subtree is walked, not just its body, because a character's hurtboxes are usually
	/// areas nested under it and any of them would stop the query just as dead.</para>
	/// </remarks>
	/// <param name="entities">The entities to pass through. Untyped because both the value and object lanes hand back
	/// untyped arrays; anything that is not an entity is skipped.</param>
	/// <param name="into">The array to fill. Cleared first, and reused between queries by its owner.</param>
	/// <returns><see langword="true"/> if anything was collected; <see langword="false"/> otherwise.</returns>
	public static bool TryCollectExclusions(IReadOnlyList<object?>? entities, GodotRidArray into)
	{
		into.Clear();

		if (entities is null)
		{
			return false;
		}

		for (int i = 0; i < entities.Count; i++)
		{
			if (entities[i] is IForgeEntity entity
				&& ForgeEntityBridge.TryGetSpatialNode3D(entity, out Node3D? spatialNode))
			{
				CollectCollisionObjects(spatialNode, into);
			}
		}

		return into.Count > 0;
	}

	/// <summary>
	/// Collects the entities a shape placed in the world overlaps.
	/// </summary>
	/// <param name="world">The world to query.</param>
	/// <param name="shape">The shape to sweep.</param>
	/// <param name="transform">Where the shape sits and how it is turned.</param>
	/// <param name="mask">The physics layers the query can find.</param>
	/// <param name="includeAreas">Whether areas count as overlaps, as well as bodies.</param>
	/// <param name="excluded">Entities to leave out, usually the caster. Untyped because both lanes hand back untyped
	/// arrays; anything that is not an entity is ignored.</param>
	/// <param name="into">The set to add the found entities to.</param>
	public static void CollectShapeOverlaps(
		World3D world,
		Shape3D shape,
		Transform3D transform,
		uint mask,
		bool includeAreas,
		IReadOnlyList<object?>? excluded,
		ISet<IForgeEntity> into)
	{
		var query = new PhysicsShapeQueryParameters3D
		{
			Shape = shape,
			Transform = transform,
			CollisionMask = mask,
			CollideWithBodies = true,
			CollideWithAreas = includeAreas,
		};

		foreach (GodotDictionary hit in world.DirectSpaceState.IntersectShape(query, MaxResults))
		{
			AddEntity(hit["collider"].As<Node>(), excluded, into);
		}
	}

	/// <summary>
	/// Collects the entities an existing area currently overlaps.
	/// </summary>
	/// <param name="area">The area to read.</param>
	/// <param name="includeAreas">Whether overlapping areas count, as well as bodies.</param>
	/// <param name="excluded">Entities to leave out, usually the area's own owner.</param>
	/// <param name="into">The set to add the found entities to.</param>
	public static void CollectAreaOverlaps(
		Area3D area,
		bool includeAreas,
		IReadOnlyList<object?>? excluded,
		ISet<IForgeEntity> into)
	{
		if (!area.Monitoring)
		{
			return;
		}

		foreach (Node3D body in area.GetOverlappingBodies())
		{
			AddEntity(body, excluded, into);
		}

		if (!includeAreas)
		{
			return;
		}

		foreach (Area3D overlapping in area.GetOverlappingAreas())
		{
			AddEntity(overlapping, excluded, into);
		}
	}

	/// <summary>
	/// Sweeps a shape through the world and reports the first thing it meets.
	/// </summary>
	/// <remarks>
	/// <para>Two queries, because Godot's sweep and Godot's contact report are separate calls. The sweep says <em>how
	/// far</em> the shape got as a fraction of the motion; placing the shape there and asking for rest info says
	/// <em>what</em> stopped it. A margin is applied to that second query because the sweep stops the shape exactly
	/// touching, and an exact touch is the one case a contact test can miss to floating point.</para>
	/// <para>Exclusions are by RID rather than dropped from the results, the same as the casts: a shape swept from a
	/// caster's own position starts inside the caster's own collider, which would stop the sweep at zero distance.
	/// </para>
	/// </remarks>
	/// <param name="world">The world to query.</param>
	/// <param name="shape">The shape to sweep.</param>
	/// <param name="transform">Where the shape starts and how it is turned.</param>
	/// <param name="motion">How far and which way the shape is swept.</param>
	/// <param name="mask">The physics layers the sweep can hit.</param>
	/// <param name="collideWithAreas">Whether areas stop the sweep, as well as bodies.</param>
	/// <param name="exclude">Collision object RIDs the sweep passes through.</param>
	/// <param name="hitTransform">Where the shape came to rest, whether or not it met anything.</param>
	/// <param name="collider">What stopped the sweep, when something did.</param>
	/// <returns><see langword="true"/> if the sweep met something; <see langword="false"/> otherwise.</returns>
	public static bool TryShapecast(
		World3D world,
		Shape3D shape,
		Transform3D transform,
		Vector3 motion,
		uint mask,
		bool collideWithAreas,
		GodotRidArray? exclude,
		out Transform3D hitTransform,
		out Node? collider)
	{
		hitTransform = transform;
		collider = null;

		var query = new PhysicsShapeQueryParameters3D
		{
			Shape = shape,
			Transform = transform,
			Motion = motion,
			CollisionMask = mask,
			CollideWithBodies = true,
			CollideWithAreas = collideWithAreas,
		};

		if (exclude is not null)
		{
			query.Exclude = exclude;
		}

		// Two fractions come back: the last one that is definitely clear, and the first one that is not. A sweep that
		// meets nothing reports both as the whole motion.
		// Asked before the sweep, because the sweep cannot answer it. Godot's cast motion skips every collider the
		// shape already overlaps at its start - "test initial overlap, ignore objects it's inside of", in the engine's
		// own words - so a sweep that begins inside something reports no hit at all rather than reporting it at zero
		// distance. Without this the first thing met is exactly the thing this misses. No margin here, unlike the test
		// at the end of the sweep: that one has to forgive an exact touch, this one must not invent one.
		query.Motion = Vector3.Zero;

		GodotDictionary resting = world.DirectSpaceState.GetRestInfo(query);

		if (resting.Count > 0)
		{
			return TryReadCollider(resting, out collider);
		}

		query.Motion = motion;

		float[] fractions = world.DirectSpaceState.CastMotion(query);

		if (fractions.Length < 2 || fractions[1] >= 1.0f)
		{
			hitTransform = transform.Translated(motion);
			return false;
		}

		hitTransform = transform.Translated(motion * fractions[1]);

		query.Transform = hitTransform;
		query.Motion = Vector3.Zero;
		query.Margin = ContactMargin;

		GodotDictionary rest = world.DirectSpaceState.GetRestInfo(query);

		return rest.Count > 0 && TryReadCollider(rest, out collider);
	}

	private static bool TryReadCollider(GodotDictionary rest, out Node? collider)
	{
		collider = GodotObject.InstanceFromId((ulong)rest["collider_id"]) as Node;
		return collider is not null;
	}

	private static void CollectCollisionObjects(Node node, GodotRidArray into)
	{
		if (node is CollisionObject3D collisionObject)
		{
			into.Add(collisionObject.GetRid());
		}

		foreach (Node child in node.GetChildren())
		{
			CollectCollisionObjects(child, into);
		}
	}

	private static void AddEntity(Node? collider, IReadOnlyList<object?>? excluded, ISet<IForgeEntity> into)
	{
		// The hierarchy walk, rather than the narrow lookup: a physics query reports the collider, which on a
		// well-built character is a hurtbox nested well below the node that owns the entity.
		if (ForgeEntityBridge.TryGetEntityInHierarchy(collider, out IForgeEntity? entity)
			&& !IsExcluded(entity, excluded))
		{
			into.Add(entity);
		}
	}

	// Overlaps drop their exclusions from the results rather than from the query, unlike the casts, which exclude by
	// RID. Either spelling of the same authored list: a cast has to keep off a collider it would otherwise start
	// inside, while an overlap only has to not report one.
	private static bool IsExcluded(IForgeEntity entity, IReadOnlyList<object?>? excluded)
	{
		if (excluded is null)
		{
			return false;
		}

		for (int i = 0; i < excluded.Count; i++)
		{
			if (ReferenceEquals(excluded[i], entity))
			{
				return true;
			}
		}

		return false;
	}
}
