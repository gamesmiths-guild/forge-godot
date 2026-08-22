// Copyright © Gamesmiths Guild.

using System.Diagnostics.CodeAnalysis;
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
}
