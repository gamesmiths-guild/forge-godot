// Copyright © Gamesmiths Guild.

using System.Linq;
using FluentAssertions;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Godot.Core;
using Gamesmiths.Forge.Godot.Nodes;
using Gamesmiths.Forge.Networking;
using GdUnit4;
using Godot;

namespace Gamesmiths.Forge.Godot.Tests.Nodes;

/// <summary>
/// The session clock: <see cref="ForgeBootstrap"/> begins every entity's tick at the clock's tick, the tick-end node
/// moves the clock once the step is over, something can ask for the next tick to be another, and a world history that
/// rolled back brings the clock along.
/// </summary>
[TestSuite]
public class ForgeClockTests
{
	private const double Step = 1.0 / 60.0;

	private static Window Root => ((SceneTree)Engine.GetMainLoop()).Root;

	private static ForgeBootstrap Bootstrap => Root.GetChildren().OfType<ForgeBootstrap>().Single();

	private static SimulationClock Clock => ForgeManagers.Instance.Clock;

	[TestCase]
	[RequireGodotRuntime]
	public void The_clock_runs_at_the_physics_tick_rate()
	{
		Clock.TickRate.Should().Be(Engine.PhysicsTicksPerSecond);
		Clock.FixedDeltaTime.Should().BeApproximately(1.0 / Engine.PhysicsTicksPerSecond, 1e-12);
	}

	[TestCase]
	[RequireGodotRuntime]
	public void The_bootstrap_begins_the_clock_s_tick_and_the_tick_end_moves_the_clock()
	{
		var entity = new ForgeEntity();
		Root.AddChild(entity);

		try
		{
			long tick = Clock.Tick;
			entity.Random.NextInt64();

			Bootstrap._PhysicsProcess(Step);
			entity.Random.NextInt64().Should().Be(Draw(entity.Random.Seed, tick), "the tick begun is the clock's");

			Bootstrap.GetNode<ForgeTickEnd>("Tick End")._PhysicsProcess(Step);
			Clock.Tick.Should().Be(tick + 1);

			Bootstrap._PhysicsProcess(Step);
			entity.Random.NextInt64().Should().Be(Draw(entity.Random.Seed, tick + 1));
		}
		finally
		{
			entity.Free();
		}
	}

	[TestCase]
	[RequireGodotRuntime]
	public void An_entity_is_seeded_at_the_clock_s_tick_when_ready()
	{
		var entity = new ForgeEntity();
		long tick = Clock.Tick;
		Root.AddChild(entity);

		try
		{
			entity.Random.NextInt64().Should().Be(Draw(entity.Random.Seed, tick));
		}
		finally
		{
			entity.Free();
		}
	}

	[TestCase]
	[RequireGodotRuntime]
	public void The_next_tick_can_be_set_and_counts_on_from_there()
	{
		long tick = Clock.Tick;

		try
		{
			ForgeManagers.Instance.SetNextTick(tick + 500);
			ForgeManagers.Instance.EndTick();
			Clock.Tick.Should().Be(tick + 500);

			ForgeManagers.Instance.EndTick();
			Clock.Tick.Should().Be(tick + 501, "the request is taken once");
		}
		finally
		{
			ForgeManagers.Instance.SetNextTick(tick + 1);
			ForgeManagers.Instance.EndTick();
		}
	}

	[TestCase]
	[RequireGodotRuntime]
	public void A_world_history_that_rolled_back_brings_the_clock_along()
	{
		var level = new Node3D();
		var body = new Node3D();
		var entity = new ForgeEntity();
		body.AddChild(entity);
		level.AddChild(body);
		var world = new ForgeWorldHistory { HistorySeconds = 1 };
		level.AddChild(world);
		Root.AddChild(level);
		long start = Clock.Tick;

		try
		{
			// Three steps of the world, each followed by the tick end, keep the world and the clock together.
			for (int i = 0; i < 3; i++)
			{
				world._PhysicsProcess(Step);
				ForgeManagers.Instance.EndTick();
			}

			world.History.CurrentTick.Should().Be(start + 2);
			Clock.Tick.Should().Be(start + 3);

			// Back to the first tick: the step after the seek settles, the next runs tick start + 1, and the clock
			// follows it.
			world.Seek(start);
			world._PhysicsProcess(Step);
			ForgeManagers.Instance.EndTick();
			world._PhysicsProcess(Step);
			ForgeManagers.Instance.EndTick();

			world.History.CurrentTick.Should().Be(start + 1);
			Clock.Tick.Should().Be(start + 2);
		}
		finally
		{
			level.Free();
		}
	}

	private static long Draw(ulong seed, long tick)
	{
		var random = new DeterministicRandom(seed);
		random.BeginTick(tick);
		return random.NextInt64();
	}
}
