// Copyright © Gamesmiths Guild.

using System.Collections.Generic;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Cues;
using Gamesmiths.Forge.Godot.Core;
using Godot;
using NumericsVector2 = System.Numerics.Vector2;
using NumericsVector3 = System.Numerics.Vector3;

namespace Gamesmiths.Forge.Godot.Nodes;

/// <summary>
/// Cue handler that instantiates a scene for the target.
/// </summary>
/// <remarks>
/// <para>The general-purpose visual: a telegraph, an impact burst, a shield bubble, a scorch mark. Executing spawns and
/// forgets; applying spawns and holds, and removing frees what it spawned, which is what ties a bubble to the effect
/// that put it there.</para>
/// <para><see cref="Lifetime"/> covers the spawn-and-forget case where the scene does not clean up after itself. A
/// scene that ends on its own - a one-shot emitter with a self-freeing timer - needs none.</para>
/// </remarks>
[GlobalClass]
public partial class InstantiateSceneCueHandler : ForgeCueHandler
{
	/// <summary>
	/// The custom cue parameter naming where the instance goes. Read as either a Godot or a <c>System.Numerics</c>
	/// vector of either dimension, since a provider written in scene code and one authored in a graph reach for
	/// different ones.
	/// </summary>
	public const string PositionParameter = "position";

	private readonly Dictionary<IForgeEntity, Node> _appliedInstances = [];

	/// <summary>
	/// Gets or sets the scene to instantiate.
	/// </summary>
	[Export]
	public PackedScene? Scene { get; set; }

	/// <summary>
	/// Gets or sets where the instance is parented.
	/// </summary>
	[Export]
	public CueAttachMode Attach { get; set; }

	/// <summary>
	/// Gets or sets how long an executed instance lives, in seconds. Zero or less leaves it to free itself.
	/// </summary>
	[Export]
	public float Lifetime { get; set; }

	/// <summary>
	/// Gets or sets the curve scaling the instance by the cue's normalized magnitude, sampled at that magnitude and
	/// multiplied into the scene's authored scale. Unset means that scale unchanged.
	/// </summary>
	[Export]
	public Curve? MagnitudeCurve { get; set; }

	/// <inheritdoc/>
	public override void _CueOnExecute(IForgeEntity forgeEntity, CueParameters? parameters)
	{
		Node? instance = Instantiate(forgeEntity, parameters);

		if (instance is null || Lifetime <= 0)
		{
			return;
		}

		SceneTreeTimer timer = GetTree().CreateTimer(Lifetime);
		timer.Timeout += () =>
		{
			if (IsInstanceValid(instance))
			{
				instance.QueueFree();
			}
		};
	}

	/// <inheritdoc/>
	public override void _CueOnApply(IForgeEntity forgeEntity, CueParameters? parameters)
	{
		// A re-application replaces rather than stacks: the cue is one visual on one target, and two of them would be
		// one too many with only one removal coming.
		FreeApplied(forgeEntity);

		Node? instance = Instantiate(forgeEntity, parameters);

		if (instance is not null)
		{
			_appliedInstances[forgeEntity] = instance;
		}
	}

	/// <inheritdoc/>
	public override void _CueOnRemove(IForgeEntity forgeEntity, bool interrupted)
	{
		FreeApplied(forgeEntity);
	}

	private static bool TryReadPosition(CueParameters? parameters, out Vector3 position3D, out Vector2 position2D)
	{
		position3D = Vector3.Zero;
		position2D = Vector2.Zero;

		if (parameters?.CustomParameters is null
			|| !parameters.Value.CustomParameters.TryGetValue(PositionParameter, out object? value))
		{
			return false;
		}

		switch (value)
		{
			case Vector3 godot3D:
				position3D = godot3D;
				position2D = new Vector2(godot3D.X, godot3D.Y);
				return true;

			case NumericsVector3 numerics3D:
				position3D = new Vector3(numerics3D.X, numerics3D.Y, numerics3D.Z);
				position2D = new Vector2(numerics3D.X, numerics3D.Y);
				return true;

			case Vector2 godot2D:
				position3D = new Vector3(godot2D.X, godot2D.Y, 0);
				position2D = godot2D;
				return true;

			case NumericsVector2 numerics2D:
				position3D = new Vector3(numerics2D.X, numerics2D.Y, 0);
				position2D = new Vector2(numerics2D.X, numerics2D.Y);
				return true;

			default:
				return false;
		}
	}

	// An instance parented to the target is already where it belongs, and its authored local offset is worth keeping -
	// a scene that sits above a character's head says so in its own transform. Only a world-attached instance, or an
	// authored position, has anything to write.
	//
	// Run before the instance is added, because adding it readies it: a scene that reads its own transform in _Ready -
	// as ForgeProjectile3D does to record where it launched from - would otherwise measure the scene's authored
	// position and only afterwards be moved to the one the cue asked for. Out of the tree there is no global transform
	// to write, so the world position is put through the parent's and written as a local one.
	private static void Place(Node instance, Node parent, Node anchor, bool placeAtAnchor, CueParameters? parameters)
	{
		bool authored = TryReadPosition(parameters, out Vector3 position3D, out Vector2 position2D);

		if (!authored && !placeAtAnchor)
		{
			return;
		}

		if (instance is Node3D instance3D)
		{
			if (!authored && anchor is Node3D anchor3D)
			{
				position3D = anchor3D.GlobalPosition;
			}
			else if (!authored)
			{
				return;
			}

			// A non-spatial parent breaks the transform chain, which makes the instance its own spatial root and its
			// local transform the global one.
			Transform3D parentTransform = parent is Node3D spatialParent
				? spatialParent.GlobalTransform
				: Transform3D.Identity;

			instance3D.Position = parentTransform.AffineInverse() * position3D;
			return;
		}

		if (instance is not Node2D instance2D)
		{
			return;
		}

		if (!authored && anchor is Node2D anchor2D)
		{
			position2D = anchor2D.GlobalPosition;
		}
		else if (!authored)
		{
			return;
		}

		Transform2D parentTransform2D = parent is Node2D spatialParent2D
			? spatialParent2D.GlobalTransform
			: Transform2D.Identity;

		instance2D.Position = parentTransform2D.AffineInverse() * position2D;
	}

	private Node? Instantiate(IForgeEntity forgeEntity, CueParameters? parameters)
	{
		if (Scene is null)
		{
			WarnOnce("has no scene to instantiate.");
			return null;
		}

		if (!ForgeEntityBridge.TryGetEntityNode(forgeEntity, out Node? entityNode))
		{
			WarnOnce("has a target that is not a scene node, so there is nowhere to instantiate.");
			return null;
		}

		Node anchor = entityNode;

		if (ForgeEntityBridge.TryGetSpatialNode3D(forgeEntity, out Node3D? spatial3D))
		{
			anchor = spatial3D;
		}
		else if (ForgeEntityBridge.TryGetSpatialNode2D(forgeEntity, out Node2D? spatial2D))
		{
			anchor = spatial2D;
		}

		Node? parent = Attach == CueAttachMode.TargetEntity
			? anchor
			: entityNode.GetTree()?.CurrentScene ?? entityNode.GetTree()?.Root;

		if (parent is null || !IsInstanceValid(parent))
		{
			WarnOnce("found no parent to add its instance to. Nothing was instantiated.");
			return null;
		}

		Node instance = Scene.Instantiate();

		Place(instance, parent, anchor, Attach == CueAttachMode.World, parameters);
		Scale(instance, parameters);

		parent.AddChild(instance);

		if (instance is IInstantiationReceiver receiver)
		{
			receiver.OnInstantiated(parameters?.Source ?? forgeEntity, parameters?.Source ?? forgeEntity);
		}

		return instance;
	}

	private void Scale(Node instance, CueParameters? parameters)
	{
		if (MagnitudeCurve is null)
		{
			return;
		}

		float factor = MagnitudeCurve.Sample(parameters?.NormalizedMagnitude ?? 0);

		switch (instance)
		{
			case Node3D instance3D:
				instance3D.Scale *= factor;
				break;

			case Node2D instance2D:
				instance2D.Scale *= factor;
				break;
		}
	}

	private void FreeApplied(IForgeEntity forgeEntity)
	{
		if (_appliedInstances.Remove(forgeEntity, out Node? instance) && IsInstanceValid(instance))
		{
			instance.QueueFree();
		}
	}
}
