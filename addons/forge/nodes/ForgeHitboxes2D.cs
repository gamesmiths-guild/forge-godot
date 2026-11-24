// Copyright © Gamesmiths Guild.

using System.Collections.Generic;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Godot.Core;
using Godot;
using Godot.Collections;

namespace Gamesmiths.Forge.Godot.Nodes;

/// <summary>
/// The hitboxes of a 2D entity: every <see cref="ForgeHitbox2D"/> under the entity's body, and the animators that pose
/// them. Put it next to the entity's <see cref="ForgeEntity"/>.
/// </summary>
/// <remarks>
/// <para>On the server, it records where each hitbox stands against the entity's body every tick, so a lag-compensated
/// shot meets the hitboxes as the shooter's client showed them, with the body. A shot that meets one reports it.</para>
/// <para>The animators in <see cref="Animators"/> move hitboxes, so they're simulation rather than presentation: they
/// advance in physics ticks, and a Play Animation node plays them on a server with nothing to show, a dedicated one,
/// where other animation doesn't play.</para>
/// </remarks>
[GlobalClass]
public partial class ForgeHitboxes2D : Node
{
	private static readonly HashSet<AnimationMixer> _poses = [];

	private readonly List<ForgeHitbox2D> _hitboxes = [];
	private readonly List<AnimationMixer> _animators = [];
	private long[] _ticks = [];
	private Transform2D[][] _history = [];

	/// <summary>
	/// Gets or sets the AnimationPlayer and AnimationTree nodes that pose the hitboxes.
	/// </summary>
	[Export]
	public Array<NodePath> Animators { get; set; } = [];

	/// <summary>
	/// Gets the hitboxes, found under the entity's body as the node gets ready.
	/// </summary>
	public IReadOnlyList<ForgeHitbox2D> Hitboxes => _hitboxes;

	internal Node2D? Body { get; private set; }

	/// <summary>
	/// Gives whether an animator poses hitboxes.
	/// </summary>
	/// <param name="animator">The animator.</param>
	/// <returns><see langword="true"/> if a hitboxes node names it.</returns>
	public static bool PosesHitboxes(AnimationMixer animator)
	{
		return _poses.Contains(animator);
	}

	/// <summary>
	/// Finds the hitboxes node of an entity.
	/// </summary>
	/// <param name="entity">The entity.</param>
	/// <returns>The node, or <see langword="null"/> for an entity without one.</returns>
	public static ForgeHitboxes2D? Of(IForgeEntity entity)
	{
		if (entity is not Node node || node.GetParent() is not Node parent)
		{
			return null;
		}

		foreach (Node child in parent.GetChildren())
		{
			if (child is ForgeHitboxes2D hitboxes)
			{
				return hitboxes;
			}
		}

		return null;
	}

	public override void _Ready()
	{
		base._Ready();

		Body = GetParent() as Node2D;
		Collect(GetParent());

		// A second, the longest rewind, and the tick after it to ease towards.
		_ticks = new long[Engine.PhysicsTicksPerSecond + 2];
		_history = new Transform2D[_ticks.Length][];
		System.Array.Fill(_ticks, -1);

		for (int i = 0; i < _ticks.Length; i++)
		{
			_history[i] = new Transform2D[_hitboxes.Count];
		}

		foreach (NodePath path in Animators)
		{
			if (GetNodeOrNull(path) is AnimationMixer animator)
			{
				animator.CallbackModeProcess = AnimationMixer.AnimationCallbackModeProcess.Physics;
				_animators.Add(animator);
				_poses.Add(animator);
			}
		}

		// Recorded after the animators and bodies moved in the tick.
		ProcessPhysicsPriority = int.MaxValue - 1;
	}

	public override void _ExitTree()
	{
		base._ExitTree();

		foreach (AnimationMixer animator in _animators)
		{
			_poses.Remove(animator);
		}

		_animators.Clear();
	}

	public override void _PhysicsProcess(double delta)
	{
		base._PhysicsProcess(delta);

		if (Body is null || ForgeNetworkSession.Of(this) is not { IsServer: true })
		{
			return;
		}

		Record(ForgeManagers.Instance.Clock.Tick);
	}

	// Records where each hitbox stands against the body at a tick.
	internal void Record(long tick)
	{
		if (Body is null)
		{
			return;
		}

		int slot = (int)(tick % _ticks.Length);
		Transform2D toBody = Body.GlobalTransform.AffineInverse();

		for (int i = 0; i < _hitboxes.Count; i++)
		{
			if (!IsInstanceValid(_hitboxes[i]))
			{
				continue;
			}

			_history[slot][i] = toBody * _hitboxes[i].GlobalTransform;
		}

		_ticks[slot] = tick;
	}

	// Where a hitbox stood against the body at a tick, which may lie between two recorded ones.
	internal bool TryLocalAt(int index, double tick, out Transform2D local)
	{
		local = Transform2D.Identity;

		if (index >= _hitboxes.Count)
		{
			return false;
		}

		long before = (long)Mathf.Floor(tick);

		if (!TryRecorded(before, out Transform2D[]? from))
		{
			return false;
		}

		if (!TryRecorded(before + 1, out Transform2D[]? to))
		{
			local = from[index];
			return true;
		}

		local = from[index].InterpolateWith(to[index], (float)(tick - before));
		return true;
	}

	private bool TryRecorded(long tick, out Transform2D[] transforms)
	{
		int slot = (int)(((tick % _ticks.Length) + _ticks.Length) % _ticks.Length);
		transforms = _history[slot];
		return _ticks[slot] == tick;
	}

	private void Collect(Node node)
	{
		foreach (Node child in node.GetChildren())
		{
			if (child is ForgeHitbox2D hitbox)
			{
				_hitboxes.Add(hitbox);
			}

			Collect(child);
		}
	}
}
