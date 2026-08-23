// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Abilities;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Statescript;
using Godot;
using Node = Godot.Node;
using NumericsQuaternion = System.Numerics.Quaternion;
using NumericsVector3 = System.Numerics.Vector3;

namespace Gamesmiths.Forge.Godot.Core.Statescript.Nodes;

/// <summary>
/// The instantiation shared by the scene nodes, so the fire-and-forget one and the lifetime-owning one cannot drift
/// apart in how they parent, place, or hand ownership to what they create.
/// </summary>
internal static class SceneInstantiationUtilities
{
	/// <summary>
	/// Instantiates a scene, parents it, places it, and hands it its Forge ownership.
	/// </summary>
	/// <param name="graphContext">The graph execution context.</param>
	/// <param name="scene">The scene to instantiate.</param>
	/// <param name="parentMode">Where to parent the instance.</param>
	/// <param name="parentNode">The parent to use under <see cref="InstantiateParentMode.Node"/>.</param>
	/// <param name="parentEntity">The entity to parent under in <see cref="InstantiateParentMode.Entity"/>, also used
	/// as the placement anchor when no position was resolved.</param>
	/// <param name="position">The world position, when one was resolved.</param>
	/// <param name="rotation">The world rotation, when one was resolved.</param>
	/// <param name="passOwnership">Whether to call <see cref="IInstantiationReceiver.OnInstantiated"/> on the instance.
	/// </param>
	/// <returns>The instance node, or <see langword="null"/> when it could not be parented.</returns>
	public static Node? Instantiate(
		GraphContext graphContext,
		PackedScene scene,
		InstantiateParentMode parentMode,
		Node? parentNode,
		IForgeEntity? parentEntity,
		NumericsVector3? position,
		NumericsQuaternion? rotation,
		bool passOwnership)
	{
		Node? parent = ResolveParent(parentMode, parentNode, parentEntity);

		if (parent is null || !GodotObject.IsInstanceValid(parent))
		{
			GD.PushWarning(
				"Statescript: a scene node found no parent to add its instance to. Nothing was instantiated.");
			return null;
		}

		Node instance = scene.Instantiate();

		// Placed before it is added, because AddChild readies the instance: a scene that reads its own transform in
		// _Ready - as ForgeProjectile3D does to record where it launched from - would otherwise measure from the
		// scene's authored position and only afterwards be teleported to the one the graph asked for.
		Place(instance, parent, parentEntity, position, rotation);

		parent.AddChild(instance);

		if (passOwnership && instance is IInstantiationReceiver receiver)
		{
			ResolveOwnership(graphContext, parentEntity, out IForgeEntity? owner, out IForgeEntity? source);
			receiver.OnInstantiated(owner, source);
		}

		return instance;
	}

	private static Node? ResolveParent(InstantiateParentMode parentMode, Node? parentNode, IForgeEntity? entity)
	{
		return parentMode switch
		{
			InstantiateParentMode.Node => parentNode,
			InstantiateParentMode.Entity => ForgeEntityBridge.TryGetSpatialNode3D(entity, out Node3D? spatialNode)
								? spatialNode
								: null,

			// Reached through the entity rather than a scene-tree singleton, because an editor-time or headless graph
			// build has no current scene to speak of.
			_ => ForgeEntityBridge.TryGetEntityNode(entity, out Node? entityNode)
								? entityNode.GetTree()?.CurrentScene ?? entityNode.GetTree()?.Root
								: null,
		};
	}

	private static void Place(
		Node instance,
		Node parent,
		IForgeEntity? entity,
		NumericsVector3? position,
		NumericsQuaternion? rotation)
	{
		if (instance is not Node3D spatialInstance)
		{
			return;
		}

		// The instance is not in the tree yet, so it has no global transform to write; the world values are put
		// through the parent's transform and written as local ones instead. A non-spatial parent breaks the transform
		// chain, which makes the instance its own spatial root and its local transform the global one.
		Transform3D parentTransform = parent is Node3D spatialParent
			? spatialParent.GlobalTransform
			: Transform3D.Identity;

		// An unbound position means "where the caster is", which is the sane default for an instance that only cares
		// about rotation, and avoids dropping it at the world origin.
		if (position.HasValue)
		{
			spatialInstance.Position = parentTransform.AffineInverse()
				* new Vector3(position.Value.X, position.Value.Y, position.Value.Z);
		}
		else if (ForgeEntityBridge.TryGetSpatialNode3D(entity, out Node3D? entityNode))
		{
			spatialInstance.Position = parentTransform.AffineInverse() * entityNode.GlobalPosition;
		}

		if (rotation.HasValue)
		{
			var quaternion = new Quaternion(
				rotation.Value.X,
				rotation.Value.Y,
				rotation.Value.Z,
				rotation.Value.W);

			if (quaternion.LengthSquared() > 0.000001f)
			{
				spatialInstance.Basis = parentTransform.Basis.Inverse() * new Basis(quaternion.Normalized());
			}
		}
	}

	private static void ResolveOwnership(
		GraphContext graphContext,
		IForgeEntity? entity,
		out IForgeEntity? owner,
		out IForgeEntity? source)
	{
		if (graphContext.TryGetActivationContext(out AbilityBehaviorContext? abilityContext))
		{
			owner = abilityContext.Owner;
			source = abilityContext.Source;
			return;
		}

		owner = entity;
		source = entity;
	}
}
