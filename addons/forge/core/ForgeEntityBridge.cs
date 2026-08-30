// Copyright © Gamesmiths Guild.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Gamesmiths.Forge.Core;
using Godot;

namespace Gamesmiths.Forge.Godot.Core;

/// <summary>
/// Canonical translation between Godot scene nodes and <see cref="IForgeEntity"/> instances.
/// </summary>
/// <remarks>
/// <para>Forge supports two authoring patterns, and this class is the only place that knows about both:</para>
/// <list type="bullet">
/// <item><description><b>Composition</b>: a plain <c>ForgeEntity</c> node parented under the visual or physics root.
/// The spatial node is an <em>ancestor</em> of the entity.</description></item>
/// <item><description><b>Direct</b>: a body script that implements <see cref="IForgeEntity"/> itself. The spatial node
/// <em>is</em> the entity.</description></item>
/// </list>
/// <para>Every spatial node, resolver, and cue handler resolves through here instead of guessing inline, so both
/// patterns keep working and the guess only ever has to be fixed in one place.</para>
/// </remarks>
public static class ForgeEntityBridge
{
	/// <summary>
	/// Finds the entity for a scene node, checking the node itself and then its direct children.
	/// </summary>
	/// <remarks>
	/// This is the narrow lookup used when applying effects to a collider that was just hit: the hit node either is
	/// the entity or owns it directly. Use <see cref="TryGetEntityInHierarchy"/> when the node may be a hitbox nested
	/// deeper than the entity's own level.
	/// </remarks>
	/// <param name="node">The scene node to resolve. May be <see langword="null"/>.</param>
	/// <param name="entity">When this method returns <see langword="true"/>, the resolved entity.</param>
	/// <returns><see langword="true"/> if an entity was found; <see langword="false"/> otherwise.</returns>
	public static bool TryGetEntity(Node? node, [NotNullWhen(true)] out IForgeEntity? entity)
	{
		entity = null;

		if (node is null || !GodotObject.IsInstanceValid(node))
		{
			return false;
		}

		if (node is IForgeEntity selfEntity)
		{
			entity = selfEntity;
			return true;
		}

		foreach (Node child in node.GetChildren())
		{
			if (child is IForgeEntity childEntity)
			{
				entity = childEntity;
				return true;
			}
		}

		return false;
	}

	/// <summary>
	/// Finds the entity for a scene node, widening the search up the ancestor chain when the node itself does not own
	/// one.
	/// </summary>
	/// <remarks>
	/// Physics queries report the collider that was hit, which is often a hurtbox nested under the body that owns the
	/// entity. This overload checks each level with the same self-then-children rule as <see cref="TryGetEntity"/>,
	/// walking up until it finds an entity or runs out of ancestors.
	/// </remarks>
	/// <param name="node">The scene node to resolve. May be <see langword="null"/>.</param>
	/// <param name="entity">When this method returns <see langword="true"/>, the resolved entity.</param>
	/// <returns><see langword="true"/> if an entity was found; <see langword="false"/> otherwise.</returns>
	public static bool TryGetEntityInHierarchy(Node? node, [NotNullWhen(true)] out IForgeEntity? entity)
	{
		Node? current = node;

		while (current is not null && GodotObject.IsInstanceValid(current))
		{
			if (TryGetEntity(current, out entity))
			{
				return true;
			}

			current = current.GetParent();
		}

		entity = null;
		return false;
	}

	/// <summary>
	/// Gets the Godot node backing an entity.
	/// </summary>
	/// <param name="entity">The entity to resolve. May be <see langword="null"/>.</param>
	/// <param name="node">When this method returns <see langword="true"/>, the entity's scene node.</param>
	/// <returns><see langword="true"/> if the entity is a live scene node; <see langword="false"/> otherwise.</returns>
	public static bool TryGetEntityNode(IForgeEntity? entity, [NotNullWhen(true)] out Node? node)
	{
		if (entity is Node entityNode && GodotObject.IsInstanceValid(entityNode))
		{
			node = entityNode;
			return true;
		}

		node = null;
		return false;
	}

	/// <summary>
	/// Gets the node an entity lives on, whichever dimension: its nearest spatial ancestor including itself, or its own
	/// node when it belongs to no spatial hierarchy at all.
	/// </summary>
	/// <remarks>
	/// The dimension-neutral counterpart of the spatial getters, for callers that treat a node as a node - reading a
	/// property off it, calling a method on it, watching one of its signals - and so have no reason to care whether the
	/// game is 2D or 3D. An authored path is resolved from that node first and from the entity's own node second, the
	/// same order the spatial getters use, so the path works whichever authoring pattern the scene uses.
	/// </remarks>
	/// <param name="entity">The entity to resolve. May be <see langword="null"/>.</param>
	/// <param name="nodePath">A path relative to the node the entity lives on, or empty for that node itself.</param>
	/// <param name="node">When this method returns <see langword="true"/>, the resolved node.</param>
	/// <returns><see langword="true"/> if a node was found; <see langword="false"/> otherwise.</returns>
	public static bool TryGetOwningNode(
		IForgeEntity? entity,
		string? nodePath,
		[NotNullWhen(true)] out Node? node)
	{
		node = null;

		if (!TryGetEntityNode(entity, out Node? entityNode))
		{
			return false;
		}

		Node owningNode = GetOwningNode(entityNode);

		if (string.IsNullOrEmpty(nodePath))
		{
			node = owningNode;
			return true;
		}

		node = owningNode.GetNodeOrNull(nodePath) ?? entityNode.GetNodeOrNull(nodePath);
		return node is not null;
	}

	/// <summary>
	/// Gets a node belonging to an entity that is neither spatial nor the entity itself - an animation player, an audio
	/// player, a particle emitter.
	/// </summary>
	/// <remarks>
	/// <para>The dimension-neutral counterpart of the spatial getters, for the nodes that have no 2D and 3D split worth
	/// caring about. Paths resolve from the node the entity lives on, so <c>AnimationPlayer</c> means the same thing
	/// under both authoring patterns rather than depending on where the <c>ForgeEntity</c> node happens to sit.</para>
	/// <para>An empty path means "the one the entity has", found among that node's children. Unlike a spatial getter,
	/// where an empty path has the entity's own node as its obvious answer, an entity's node is never itself a player,
	/// so the alternative would be a path that is always required for the single-player scene that is the common case.
	/// The search is one level deep and takes the first match, which keeps it predictable: a player nested inside a
	/// model scene is named explicitly.</para>
	/// </remarks>
	/// <param name="entity">The entity to resolve. May be <see langword="null"/>.</param>
	/// <param name="nodePath">A path from the node the entity lives on, or empty to take the first matching child.
	/// </param>
	/// <param name="matches">What makes a node the one being looked for.</param>
	/// <param name="child">When this method returns <see langword="true"/>, the resolved node.</param>
	/// <returns><see langword="true"/> if a matching node was found; <see langword="false"/> otherwise.</returns>
	public static bool TryGetEntityChild(
		IForgeEntity? entity,
		string? nodePath,
		Func<Node, bool> matches,
		[NotNullWhen(true)] out Node? child)
	{
		child = null;

		if (!TryGetEntityNode(entity, out Node? entityNode))
		{
			return false;
		}

		Node root = GetOwningNode(entityNode);

		if (string.IsNullOrEmpty(nodePath))
		{
			child = root.GetChildren().FirstOrDefault(matches);
			return child is not null;
		}

		Node? resolved = root.GetNodeOrNull(nodePath) ?? entityNode.GetNodeOrNull(nodePath);
		child = resolved is not null && matches(resolved) ? resolved : null;

		return child is not null;
	}

	/// <summary>
	/// Gets a node of a given type belonging to an entity.
	/// </summary>
	/// <typeparam name="T">The node type to look for.</typeparam>
	/// <param name="entity">The entity to resolve. May be <see langword="null"/>.</param>
	/// <param name="nodePath">A path from the node the entity lives on, or empty to take the first child of that type.
	/// </param>
	/// <param name="child">When this method returns <see langword="true"/>, the resolved node.</param>
	/// <returns><see langword="true"/> if a node of that type was found; <see langword="false"/> otherwise.</returns>
	public static bool TryGetEntityChild<T>(IForgeEntity? entity, string? nodePath, [NotNullWhen(true)] out T? child)
		where T : Node
	{
		bool found = TryGetEntityChild(entity, nodePath, static node => node is T, out Node? node);
		child = (T?)node;

		return found;
	}

	/// <summary>
	/// Gets the 3D spatial node an entity lives at: the entity's own node when it is spatial, otherwise its nearest
	/// spatial ancestor.
	/// </summary>
	/// <param name="entity">The entity to resolve. May be <see langword="null"/>.</param>
	/// <param name="spatialNode">When this method returns <see langword="true"/>, the entity's spatial node.</param>
	/// <returns><see langword="true"/> if a spatial node was found; <see langword="false"/> otherwise.</returns>
	public static bool TryGetSpatialNode3D(IForgeEntity? entity, [NotNullWhen(true)] out Node3D? spatialNode)
	{
		spatialNode = null;

		if (!TryGetEntityNode(entity, out Node? node))
		{
			return false;
		}

		Node? current = node;

		while (current is not null && GodotObject.IsInstanceValid(current))
		{
			if (current is Node3D spatial)
			{
				spatialNode = spatial;
				return true;
			}

			current = current.GetParent();
		}

		return false;
	}

	/// <summary>
	/// Gets the 3D spatial node an entity lives at, optionally redirected to a descendant marker.
	/// </summary>
	/// <remarks>
	/// This is how authored offsets such as a muzzle or cast point are expressed without Forge inventing a cast-point
	/// concept: point <paramref name="nodePath"/> at a marker node. Scene-unique names (<c>%CastPoint</c>) are
	/// supported, and are looked up from the spatial node first and the entity node second, so the path works whichever
	/// authoring pattern the scene uses.
	/// <para>A path that resolves to nothing returns <see langword="false"/> silently rather than logging: resolvers
	/// call this every tick, so a warning here would repeat every frame. Callers that want to report a bad path should
	/// do so once.</para>
	/// </remarks>
	/// <param name="entity">The entity to resolve. May be <see langword="null"/>.</param>
	/// <param name="nodePath">A path relative to the entity's spatial node, or empty for the spatial node itself.
	/// </param>
	/// <param name="spatialNode">When this method returns <see langword="true"/>, the resolved spatial node.</param>
	/// <returns><see langword="true"/> if a spatial node was found; <see langword="false"/> otherwise.</returns>
	public static bool TryGetSpatialNode3D(
		IForgeEntity? entity,
		string? nodePath,
		[NotNullWhen(true)] out Node3D? spatialNode)
	{
		if (!TryGetSpatialNode3D(entity, out spatialNode))
		{
			return false;
		}

		if (string.IsNullOrEmpty(nodePath))
		{
			return true;
		}

		Node3D? resolved = spatialNode.GetNodeOrNull<Node3D>(nodePath);

		if (resolved is null && TryGetEntityNode(entity, out Node? entityNode))
		{
			resolved = entityNode.GetNodeOrNull<Node3D>(nodePath);
		}

		spatialNode = resolved;
		return resolved is not null;
	}

	/// <summary>
	/// Gets the 2D spatial node an entity lives at: the entity's own node when it is spatial, otherwise its nearest
	/// spatial ancestor.
	/// </summary>
	/// <param name="entity">The entity to resolve. May be <see langword="null"/>.</param>
	/// <param name="spatialNode">When this method returns <see langword="true"/>, the entity's spatial node.</param>
	/// <returns><see langword="true"/> if a spatial node was found; <see langword="false"/> otherwise.</returns>
	public static bool TryGetSpatialNode2D(IForgeEntity? entity, [NotNullWhen(true)] out Node2D? spatialNode)
	{
		spatialNode = null;

		if (!TryGetEntityNode(entity, out Node? node))
		{
			return false;
		}

		Node? current = node;

		while (current is not null && GodotObject.IsInstanceValid(current))
		{
			if (current is Node2D spatial)
			{
				spatialNode = spatial;
				return true;
			}

			current = current.GetParent();
		}

		return false;
	}

	/// <summary>
	/// Gets the 2D spatial node an entity lives at, optionally redirected to a descendant marker.
	/// </summary>
	/// <remarks>
	/// The 2D counterpart of <see cref="TryGetSpatialNode3D(IForgeEntity, string, out Node3D)"/>; see that overload for
	/// how <paramref name="nodePath"/> is resolved.
	/// </remarks>
	/// <param name="entity">The entity to resolve. May be <see langword="null"/>.</param>
	/// <param name="nodePath">A path relative to the entity's spatial node, or empty for the spatial node itself.
	/// </param>
	/// <param name="spatialNode">When this method returns <see langword="true"/>, the resolved spatial node.</param>
	/// <returns><see langword="true"/> if a spatial node was found; <see langword="false"/> otherwise.</returns>
	public static bool TryGetSpatialNode2D(
		IForgeEntity? entity,
		string? nodePath,
		[NotNullWhen(true)] out Node2D? spatialNode)
	{
		if (!TryGetSpatialNode2D(entity, out spatialNode))
		{
			return false;
		}

		if (string.IsNullOrEmpty(nodePath))
		{
			return true;
		}

		Node2D? resolved = spatialNode.GetNodeOrNull<Node2D>(nodePath);

		if (resolved is null && TryGetEntityNode(entity, out Node? entityNode))
		{
			resolved = entityNode.GetNodeOrNull<Node2D>(nodePath);
		}

		spatialNode = resolved;
		return resolved is not null;
	}

	// The node an entity lives on, whichever dimension: the nearest spatial ancestor including itself, and the entity's
	// own node when it belongs to no spatial hierarchy at all.
	private static Node GetOwningNode(Node entityNode)
	{
		Node? current = entityNode;

		while (current is not null && GodotObject.IsInstanceValid(current))
		{
			if (current is Node2D or Node3D)
			{
				return current;
			}

			current = current.GetParent();
		}

		return entityNode;
	}
}
