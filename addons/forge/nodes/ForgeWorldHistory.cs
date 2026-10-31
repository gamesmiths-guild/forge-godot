// Copyright © Gamesmiths Guild.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.ExceptionServices;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Godot.Core;
using Godot;
using Node = Godot.Node;

namespace Gamesmiths.Forge.Godot.Nodes;

/// <summary>
/// Keeps the recent ticks of every <see cref="ForgeEntity"/> under its parent, so the scene can be paused and rolled
/// back to any of them, the whole world together.
/// </summary>
/// <remarks>
/// <para>It advances its entities itself, in its physics step, through a core <see cref="WorldHistory"/>: every entity
/// begins the tick, then each advances, then the tick is recorded. Entities spawned under its parent join as they
/// become ready.</para>
/// <para>An entity that dies goes to limbo instead of being freed: <see cref="Despawn"/>, or the Queue Free node on the
/// node the entity lives on, takes that node out of processing, physics and visibility, and keeps it until no tick held
/// has it.</para>
/// <para>Pause the tree to stop the world, and call <see cref="Seek"/> to show a tick held as it was. Unpausing
/// continues the world from that tick, leaving the ticks after it behind, or, with <see cref="Replaying"/> on, runs the
/// recorded ticks after it again and checks each against the recording.</para>
/// <para>An entity spawned into the world is seeded from the tick it spawns in and its place among that tick's spawns,
/// so one a replay spawns again rolls the way the one spawned first did.</para>
/// </remarks>
[GlobalClass]
[Icon("uid://baypv15ai34rl")]
public partial class ForgeWorldHistory : Node
{
	private readonly List<ForgeEntity> _entities = [];
	private readonly Dictionary<IForgeEntity, LimboState> _limbo = [];
	private Node? _root;
	private bool _settling;
	private ulong _sessionSeed;
	private long _spawnTick;
	private int _spawnIndex;

	// The particle systems on the world's entities as the first seek since the last presentation pass found them.
	private List<ParticleState>? _particles;

	/// <summary>
	/// Raised when a replayed tick came out differently from the recording.
	/// </summary>
	public event Action<WorldDesync>? Desynced;

	/// <summary>
	/// Gets or sets how far back the world can be rolled, in seconds.
	/// </summary>
	[Export]
	public double HistorySeconds { get; set; } = 5;

	/// <summary>
	/// Gets the core history of the world.
	/// </summary>
	public WorldHistory History { get; private set; } = null!;

	/// <summary>
	/// Gets how long recording the last tick took, which is how long its snapshots took to take.
	/// </summary>
	public TimeSpan LastRecordTime { get; private set; }

	/// <summary>
	/// Gets how many bytes recording the last tick allocated, which is about what its snapshots hold.
	/// </summary>
	public long LastRecordBytes { get; private set; }

	/// <summary>
	/// Gets or sets a value indicating whether the world replays its recorded ticks instead of running live ones.
	/// </summary>
	/// <remarks>
	/// While it's on, each physics step runs the tick after the one the world is at again, from the recording and with
	/// the input frames it was recorded with, through <see cref="WorldHistory.Replay"/>, and raises
	/// <see cref="Desynced"/> when the tick came out differently. The world stays as the recording left each tick, and
	/// waits at the newest tick held until replaying is turned off.
	/// </remarks>
	public bool Replaying { get; set; }

	/// <summary>
	/// Initializes a new instance of the <see cref="ForgeWorldHistory"/> class.
	/// </summary>
	/// <remarks>
	/// Its physics step runs right after <see cref="ForgeBootstrap"/>'s, so the input collected in the same step is
	/// what the tick reads, and before the bodies' own physics steps, as an entity's own would.
	/// </remarks>
	public ForgeWorldHistory()
	{
		ProcessPhysicsPriority = int.MinValue + 2;
	}

	/// <summary>
	/// Takes an entity out of the world, as when it dies. Its node goes to limbo when the tick is recorded, and is
	/// freed once no tick held has it.
	/// </summary>
	/// <param name="entity">An entity this world history drives.</param>
	public void Despawn(ForgeEntity entity)
	{
		History.Remove(entity);
	}

	/// <summary>
	/// Shows the world as a tick held left it: every entity in the world then is restored, and every other one is in
	/// limbo.
	/// </summary>
	/// <remarks>
	/// <para>The bodies are put in place without interpolating from where they were. Unpausing after a seek continues
	/// the world from the tick, and leaves the ticks after it behind.</para>
	/// <para>The first physics step after a seek doesn't tick: the physics server can take until its next step to move
	/// a character body put back in place, and a tick that queried it before then would find the body where it was.
	/// </para>
	/// <para>A particle system on an entity that the seek showed, hid or moved, as of the presentation pass after it,
	/// starts over if it's emitting: particles can't go back with the world, and Godot keeps a hidden system's where
	/// they froze and a moved one's where it was.</para>
	/// </remarks>
	/// <param name="tick">A tick between <see cref="WorldHistory.OldestTick"/> and
	/// <see cref="WorldHistory.NewestTick"/>.
	/// </param>
	public void Seek(long tick)
	{
		_particles ??= Particles();
		History.Rollback(tick);
		_settling = true;

		foreach (IForgeEntity entity in History.Entities)
		{
			LimboNode(entity).ResetPhysicsInterpolation();
		}
	}

	/// <inheritdoc/>
	public override void _EnterTree()
	{
		base._EnterTree();

		// Godot readies a node once, so a history put back in the tree starts over here.
		if (IsNodeReady())
		{
			Start();
		}
	}

	/// <inheritdoc/>
	public override void _Ready()
	{
		base._Ready();
		Start();
	}

	/// <inheritdoc/>
	public override void _ExitTree()
	{
		base._ExitTree();

		GetTree().NodeAdded -= OnNodeAdded;

		// Without the history the entities tick on their own again, and nothing can bring the ones in limbo back.
		foreach (ForgeEntity entity in _entities)
		{
			entity.WorldHistory = null;
		}

		foreach (IForgeEntity entity in _limbo.Keys)
		{
			LimboNode(entity).QueueFree();
		}

		_entities.Clear();
		_limbo.Clear();
	}

	/// <inheritdoc/>
	public override void _PhysicsProcess(double delta)
	{
		base._PhysicsProcess(delta);

		if (_settling)
		{
			_settling = false;
			return;
		}

		for (int i = _entities.Count - 1; i >= 0; i--)
		{
			LeaveIfMovedOut(_entities[i]);
		}

		long tick = (History.CurrentTick ?? (long)Engine.GetPhysicsFrames()) + 1;

		if (Replaying)
		{
			if (tick <= History.NewestTick)
			{
				foreach (WorldDesync desync in History.Replay(tick, x => Advance(x, delta)?.Throw()))
				{
					Desynced?.Invoke(desync);
				}
			}

			return;
		}

		ExceptionDispatchInfo? failure = Advance(tick, delta);
		long startBytes = GC.GetAllocatedBytesForCurrentThread();
		long start = Stopwatch.GetTimestamp();
		History.Record(tick);
		LastRecordTime = Stopwatch.GetElapsedTime(start);
		LastRecordBytes = GC.GetAllocatedBytesForCurrentThread() - startBytes;

		failure?.Throw();
	}

	// Sends the entity living on the node to its world history's limbo, when one drives it, instead of freeing the
	// node, so a rollback can bring it back.
	internal static bool TryDespawn(Node node)
	{
		if (FindEntityLivingOn(node) is ForgeEntity { WorldHistory: ForgeWorldHistory worldHistory } entity
			&& ForgeEntityBridge.TryGetOwningNode(entity, null, out Node? owningNode)
			&& owningNode == node)
		{
			worldHistory.Despawn(entity);
			return true;
		}

		return false;
	}

	// Called after the presentation pass: the particle systems a seek since the last pass showed, hid or moved start
	// over.
	internal void AfterPresentation()
	{
		if (_particles is not List<ParticleState> particles)
		{
			return;
		}

		_particles = null;

		foreach (ParticleState particle in particles)
		{
			// A cue handler can take its particles out of the tree on a seek, freeing them at the frame's end.
			if (IsInstanceValid(particle.Node) && particle.Node.IsInsideTree() && Particle(particle.Node) != particle)
			{
				Restart(particle.Node);
			}
		}
	}

	// An entity freed some other way than by going to limbo can't come back.
	internal void OnEntityFreed(ForgeEntity entity)
	{
		GD.PushWarning(
			$"Forge: {entity.Name} was freed while {Name} kept its history, so no rollback brings it back. Despawn " +
			"it with the Queue Free node or ForgeWorldHistory.Despawn instead.");

		History.Forget(entity);
		_entities.Remove(entity);
		_limbo.Remove(entity);
	}

	private static Node LimboNode(IForgeEntity entity)
	{
		return ForgeEntityBridge.TryGetOwningNode(entity, null, out Node? node) ? node : (Node)entity;
	}

	// An entity lives on its nearest spatial ancestor, so the one living on a node is the node itself or is found below
	// it through children that aren't spatial.
	private static ForgeEntity? FindEntityLivingOn(Node node)
	{
		if (node is ForgeEntity entity)
		{
			return entity;
		}

		foreach (Node child in node.GetChildren())
		{
			if (child is not (Node2D or Node3D) && FindEntityLivingOn(child) is ForgeEntity found)
			{
				return found;
			}
		}

		return null;
	}

	private static void AddParticles(Node node, List<ParticleState> particles)
	{
		if (node is GpuParticles3D or GpuParticles2D or CpuParticles3D or CpuParticles2D)
		{
			particles.Add(Particle(node));
		}

		foreach (Node child in node.GetChildren())
		{
			AddParticles(child, particles);
		}
	}

	private static ParticleState Particle(Node node)
	{
		return node switch
		{
			Node3D node3D => new ParticleState(node, node3D.IsVisibleInTree(), node3D.GlobalPosition),
			Node2D node2D => new ParticleState(
				node,
				node2D.IsVisibleInTree(),
				new Vector3(node2D.GlobalPosition.X, node2D.GlobalPosition.Y, 0)),
			_ => new ParticleState(node, true, Vector3.Zero),
		};
	}

	// One that has stopped emitting, such as a one-shot that played out, is left alone rather than played again.
	private static void Restart(Node particles)
	{
		switch (particles)
		{
			case GpuParticles3D { Emitting: true } gpu3D:
				gpu3D.Restart();
				break;

			case GpuParticles2D { Emitting: true } gpu2D:
				gpu2D.Restart();
				break;

			case CpuParticles3D { Emitting: true } cpu3D:
				cpu3D.Restart();
				break;

			case CpuParticles2D { Emitting: true } cpu2D:
				cpu2D.Restart();
				break;
		}
	}

	private static bool IsVisible(Node node)
	{
		return node switch
		{
			Node3D node3D => node3D.Visible,
			CanvasItem canvasItem => canvasItem.Visible,
			_ => true,
		};
	}

	private static void SetVisible(Node node, bool visible)
	{
		switch (node)
		{
			case Node3D node3D:
				node3D.Visible = visible;
				break;

			case CanvasItem canvasItem:
				canvasItem.Visible = visible;
				break;
		}
	}

	// Begins every entity's tick, then advances each. An entity that throws keeps no other from advancing, as when each
	// advanced in its own physics step, and the first exception is handed back. One whose processing is off stays as
	// it is, as it would without the history, and is still recorded.
	private ExceptionDispatchInfo? Advance(long tick, double delta)
	{
		IReadOnlyList<IForgeEntity> entities = History.Entities;
		ExceptionDispatchInfo? failure = null;

		// Each run of the tick counts its spawns from the first, however often it runs again.
		_spawnTick = tick;
		_spawnIndex = 0;

		foreach (IForgeEntity entity in entities)
		{
			EntitySimulation.BeginTick(entity, tick);
		}

		foreach (IForgeEntity entity in entities)
		{
			try
			{
				if (entity is not Node node || (node.CanProcess() && node.IsPhysicsProcessing()))
				{
					EntitySimulation.Advance(entity, delta);
				}
			}
			catch (Exception exception)
			{
				failure ??= ExceptionDispatchInfo.Capture(exception);
			}
		}

		return failure;
	}

	private List<ParticleState> Particles()
	{
		List<ParticleState> particles = [];

		foreach (ForgeEntity entity in _entities)
		{
			AddParticles(LimboNode(entity), particles);
		}

		return particles;
	}

	private void Start()
	{
		_settling = false;
		_particles = null;
		_sessionSeed = ((ulong)GD.Randi() << 32) | GD.Randi();
		int ticksPerSecond = Engine.PhysicsTicksPerSecond;
		History = new WorldHistory(
			(int)Math.Ceiling(Math.Max(HistorySeconds, 0) * ticksPerSecond) + 1,
			1.0 / ticksPerSecond);
		History.EnteredLimbo += OnEnteredLimbo;
		History.LeftLimbo += OnLeftLimbo;
		History.Released += OnReleased;

		_root = GetParent();
		JoinUnder(_root);
		GetTree().NodeAdded += OnNodeAdded;
	}

	private void JoinUnder(Node node)
	{
		foreach (Node child in node.GetChildren())
		{
			if (child is ForgeEntity entity)
			{
				JoinWhenReady(entity);
			}

			JoinUnder(child);
		}
	}

	private void OnNodeAdded(Node node)
	{
		if (node is not ForgeEntity entity || _root?.IsAncestorOf(entity) != true)
		{
			return;
		}

		// One moved here from another history's world leaves it first.
		if (entity.WorldHistory is ForgeWorldHistory other && other != this)
		{
			other.LeaveIfMovedOut(entity);
		}

		if (entity.WorldHistory is null)
		{
			if (!entity.IsNodeReady())
			{
				SeedSpawn(entity);
			}

			JoinWhenReady(entity);
		}
	}

	// Seeds an entity spawning into the world from the tick it spawns in and its place among that tick's spawns, which
	// a replay of the tick gives the entity it spawns again too.
	private void SeedSpawn(ForgeEntity entity)
	{
		long tick = (History.CurrentTick ?? (long)Engine.GetPhysicsFrames()) + 1;

		if (tick != _spawnTick)
		{
			_spawnTick = tick;
			_spawnIndex = 0;
		}

		entity.SpawnTick = tick;
		entity.SpawnSeed = unchecked(
			_sessionSeed ^ ((ulong)tick * 0x9E3779B97F4A7C15UL) ^ ((ulong)_spawnIndex++ * 0xC2B2AE3D27D4EB4FUL));
	}

	private void JoinWhenReady(ForgeEntity entity)
	{
		if (entity.IsNodeReady())
		{
			Join(entity);
		}
		else
		{
			entity.Connect(Node.SignalName.Ready, Callable.From(() => Join(entity)), (uint)ConnectFlags.OneShot);
		}
	}

	private void Join(ForgeEntity entity)
	{
		// One that waited to be ready may have moved away meanwhile, and a limbo node the history freed as it left the
		// tree is still there if it comes back within the frame.
		if (entity.WorldHistory is not null
			|| _root?.IsAncestorOf(entity) != true
			|| LimboNode(entity).IsQueuedForDeletion())
		{
			return;
		}

		entity.WorldHistory = this;
		_entities.Add(entity);
		History.Add(entity);
	}

	// An entity moved out from under the history's parent leaves its world, free to join another.
	private void LeaveIfMovedOut(ForgeEntity entity)
	{
		if (entity.IsInsideTree() && _root?.IsAncestorOf(entity) == true)
		{
			return;
		}

		History.Forget(entity);
		_entities.Remove(entity);
		OnLeftLimbo(entity);
		entity.WorldHistory = null;
	}

	private void OnEnteredLimbo(IForgeEntity entity)
	{
		Node node = LimboNode(entity);
		_limbo[entity] = new LimboState(node.ProcessMode, IsVisible(node));
		node.ProcessMode = ProcessModeEnum.Disabled;
		SetVisible(node, false);
	}

	private void OnLeftLimbo(IForgeEntity entity)
	{
		if (_limbo.Remove(entity, out LimboState state))
		{
			Node node = LimboNode(entity);
			node.ProcessMode = state.ProcessMode;
			SetVisible(node, state.Visible);
		}
	}

	private void OnReleased(IForgeEntity entity)
	{
		_limbo.Remove(entity);

		if (entity is ForgeEntity forgeEntity)
		{
			forgeEntity.WorldHistory = null;
			_entities.Remove(forgeEntity);
		}

		LimboNode(entity).QueueFree();
	}

	private readonly record struct LimboState(ProcessModeEnum ProcessMode, bool Visible);

	private readonly record struct ParticleState(Node Node, bool Visible, Vector3 Position);
}
