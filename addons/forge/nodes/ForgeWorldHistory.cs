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
/// continues the world from that tick, leaving the ticks after it behind.</para>
/// </remarks>
[GlobalClass]
[Icon("uid://baypv15ai34rl")]
public partial class ForgeWorldHistory : Node
{
	private readonly List<ForgeEntity> _entities = [];
	private readonly Dictionary<IForgeEntity, LimboState> _limbo = [];
	private Node? _root;
	private bool _settling;

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
	/// </remarks>
	/// <param name="tick">A tick between <see cref="WorldHistory.OldestTick"/> and
	/// <see cref="WorldHistory.NewestTick"/>.
	/// </param>
	public void Seek(long tick)
	{
		History.Rollback(tick);
		_settling = true;

		foreach (IForgeEntity entity in History.Entities)
		{
			LimboNode(entity).ResetPhysicsInterpolation();
		}
	}

	/// <inheritdoc/>
	public override void _Ready()
	{
		base._Ready();

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

		long tick = (History.CurrentTick ?? (long)Engine.GetPhysicsFrames()) + 1;
		IReadOnlyList<IForgeEntity> entities = History.Entities;
		ExceptionDispatchInfo? failure = null;

		foreach (IForgeEntity entity in entities)
		{
			EntitySimulation.BeginTick(entity, tick);
		}

		// An entity that throws keeps no other from advancing, as when each advanced in its own physics step.
		foreach (IForgeEntity entity in entities)
		{
			try
			{
				EntitySimulation.Advance(entity, delta);
			}
			catch (Exception exception)
			{
				failure ??= ExceptionDispatchInfo.Capture(exception);
			}
		}

		long startBytes = GC.GetAllocatedBytesForCurrentThread();
		long start = Stopwatch.GetTimestamp();
		History.Record(tick);
		LastRecordTime = Stopwatch.GetElapsedTime(start);
		LastRecordBytes = GC.GetAllocatedBytesForCurrentThread() - startBytes;

		failure?.Throw();
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
		if (node is ForgeEntity { WorldHistory: null } entity && _root?.IsAncestorOf(entity) == true)
		{
			JoinWhenReady(entity);
		}
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
		if (entity.WorldHistory is not null)
		{
			return;
		}

		entity.WorldHistory = this;
		_entities.Add(entity);
		History.Add(entity);
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
}
