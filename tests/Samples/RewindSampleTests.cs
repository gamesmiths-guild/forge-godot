// Copyright © Gamesmiths Guild.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Gamesmiths.Forge.Attributes;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Example;
using Gamesmiths.Forge.Godot.Core;
using Gamesmiths.Forge.Godot.Nodes;
using Gamesmiths.Forge.Tags;
using GdUnit4;
using Godot;
using Node = Godot.Node;

namespace Gamesmiths.Forge.Godot.Tests.Samples;

/// <summary>
/// Sample 04 played by script: the player catches fire at the bonfire, the fire spreads to the enemies, they burn to
/// death and are replaced, and the world history goes back before both and forward again.
/// </summary>
[TestSuite]
public class RewindSampleTests
{
	private const string LevelPath = "res://forge_samples/demos/04_rewind/scenes/rewind_level.tscn";

	private const string DeathBurstPath = "res://forge_samples/demos/03_statescript/scenes/vfx/death_burst.tscn";

	private const string ProjectilePath = "res://forge_samples/demos/04_rewind/scenes/projectile.tscn";

	private const string FireEffectPath =
		"res://forge_samples/demos/01_realtime_3d/scenes/particles/3d_fire_effect.tscn";

	private static Window Root => ((SceneTree)Engine.GetMainLoop()).Root;

	private static double Step => 1.0 / Engine.PhysicsTicksPerSecond;

	[TestCase(Timeout = 120000)]
	[RequireGodotRuntime]
	public async Task A_death_and_the_spawn_that_replaced_it_rewind_and_come_back()
	{
		Node level = GD.Load<PackedScene>(LevelPath).Instantiate();
		Root.AddChild(level);

		try
		{
			await SettlePhysics(level);

			ForgeWorldHistory world = level.GetNode<ForgeWorldHistory>("World History");
			CharacterBody3D player = level.GetNode<CharacterBody3D>("Player");
			ForgeEntity playerEntity = player.GetNode<ForgeEntity>("%Forge Entity");
			Node spawned = level.GetNode("%Spawned");
			CharacterBody3D[] enemies = [.. spawned.GetChildren().OfType<CharacterBody3D>()];

			// Standing by the bonfire sets the player alight.
			player.GlobalPosition = new Vector3(0, 0, 1.3f);
			await SettlePhysics(level);
			RunTicks(level, world, 30);

			playerEntity.Tags.AllTags.HasTag(Tag.RequestTag(ForgeManagers.Instance.TagsManager, "effect.fire"))
				.Should().BeTrue("the bonfire set the player alight");

			// The first enemy is brought to the burning player, and burns until it dies.
			CharacterBody3D dying = enemies[0];
			dying.GlobalPosition = new Vector3(1.2f, 0, 1.3f);
			await SettlePhysics(level);
			long? death = null;

			for (int i = 0; i < 600 && death is null; i++)
			{
				RunTicks(level, world, 1);

				if (!dying.Visible)
				{
					death = world.History.CurrentTick;
				}
			}

			death.Should().NotBeNull("the fire killed the enemy");
			dying.ProcessMode.Should().Be(Node.ProcessModeEnum.Disabled, "it waits in limbo");
			spawned.GetChildren().OfType<Node3D>().Single(x => x.SceneFilePath == DeathBurstPath).GlobalPosition
				.DistanceTo(dying.GlobalPosition).Should().BeLessThan(0.01f, "the burst stays where the enemy died");

			long? spawn = null;

			for (int i = 0; i < 60 && spawn is null; i++)
			{
				RunTicks(level, world, 1);

				if (Replacements(spawned, enemies).Any())
				{
					spawn = world.History.CurrentTick;
				}
			}

			(spawn - death).Should().BeInRange(17, 19, "the spawner waits 0.3 seconds");

			CharacterBody3D replacement = Replacements(spawned, enemies).Single();
			replacement.Visible.Should().BeTrue();

			RunTicks(level, world, 30);
			long newest = world.History.NewestTick!.Value;

			world.Seek(spawn!.Value - 1);

			dying.Visible.Should().BeFalse("the enemy is still dead");
			replacement.Visible.Should().BeFalse("the replacement waits in limbo before it spawned");

			world.Seek(death!.Value - 1);

			dying.Visible.Should().BeTrue("the enemy is back before it died");

			world.Seek(newest);

			dying.Visible.Should().BeFalse();
			replacement.Visible.Should().BeTrue();

			// Far enough back that the next tick doesn't burn the enemy to death again.
			world.Seek(death.Value - 40);
			await SettlePhysics(level);
			RunTicks(level, world, 1);

			replacement.IsQueuedForDeletion().Should().BeTrue("resuming left behind the ticks it spawned in");
			dying.ProcessMode.Should().Be(Node.ProcessModeEnum.Inherit);
			world.History.Entities.Should().Contain(dying.GetNode<ForgeEntity>("%Forge Entity"));
		}
		finally
		{
			level.Free();
		}
	}

	[TestCase(Timeout = 120000)]
	[RequireGodotRuntime]
	public async Task Enemies_that_die_on_one_tick_all_come_back_even_at_one_spawn_point()
	{
		Node level = GD.Load<PackedScene>(LevelPath).Instantiate();
		Root.AddChild(level);

		try
		{
			await SettlePhysics(level);

			ForgeWorldHistory world = level.GetNode<ForgeWorldHistory>("World History");
			Node spawned = level.GetNode("%Spawned");
			CharacterBody3D[] enemies = [.. spawned.GetChildren().OfType<CharacterBody3D>()];

			// One marker left, so both replacements are drawn to the same spot.
			foreach (Node point in level.GetNode("SpawnPoints").GetChildren().Skip(1))
			{
				point.RemoveFromGroup("spawn_points");
			}

			level.GetNode<CharacterBody3D>("Player").GlobalPosition = new Vector3(0, 0, 1.3f);
			await SettlePhysics(level, 40);

			// Either side of the burning player, so the same pulse of fire reaches both.
			enemies[0].GlobalPosition = new Vector3(1, 0, 1.3f);
			enemies[1].GlobalPosition = new Vector3(-1, 0, 1.3f);
			enemies[2].GlobalPosition = new Vector3(8, 0, -8);
			long?[] deaths = new long?[2];

			// Real physics steps throughout: the server only moves a body it has been told about on its next step, and
			// two bodies left in the same place were what it flung.
			for (int i = 0; i < 600 && Array.Exists(deaths, x => x is null); i++)
			{
				await SettlePhysics(level);

				for (int e = 0; e < deaths.Length; e++)
				{
					if (deaths[e] is null && !enemies[e].Visible)
					{
						deaths[e] = world.History.CurrentTick;
					}
				}
			}

			deaths[0].Should().NotBeNull().And.Be(deaths[1], "both burned to death on the same tick");

			await SettlePhysics(level, 80);

			CharacterBody3D[] replacements = [.. Replacements(spawned, enemies)];
			replacements.Should().HaveCount(2, "each death brings an enemy back");

			foreach (CharacterBody3D replacement in replacements)
			{
				replacement.GlobalPosition.Y.Should().BeLessThan(0.5f, "nothing launched it");
				replacement.GlobalPosition.Length().Should().BeLessThan(15f, "it stayed in the arena");
			}

			replacements[0].GlobalPosition.DistanceTo(replacements[1].GlobalPosition).Should().BeGreaterThan(0.5f);

			// Back to while both respawns were waiting, and on from there: both are kept, and both spawn again.
			world.Seek(deaths[0]!.Value + 5);
			await SettlePhysics(level, 40);

			replacements.Should().AllSatisfy(
				x => GodotObject.IsInstanceValid(x).Should().BeFalse("its spawn was left behind"));
			Replacements(spawned, enemies).Should().HaveCount(2);
		}
		finally
		{
			level.Free();
		}
	}

	[TestCase(Timeout = 120000)]
	[RequireGodotRuntime]
	public async Task A_projectile_and_its_hit_rewind_and_come_back()
	{
		Node level = GD.Load<PackedScene>(LevelPath).Instantiate();
		Root.AddChild(level);

		try
		{
			await SettlePhysics(level);

			ForgeWorldHistory world = level.GetNode<ForgeWorldHistory>("World History");
			CharacterBody3D player = level.GetNode<CharacterBody3D>("Player");
			ForgeEntity playerEntity = player.GetNode<ForgeEntity>("%Forge Entity");
			Node spawned = level.GetNode("%Spawned");
			CharacterBody3D[] enemies = [.. spawned.GetChildren().OfType<CharacterBody3D>()];
			CharacterBody3D target = enemies[2];
			EntityAttribute health =
				target.GetNode<ForgeEntity>("%Forge Entity").Attributes["CharacterAttributes.Health"];

			// The target stands four units in front of the player, clear of the bonfire.
			player.GlobalPosition = new Vector3(5, 0, 6);
			target.GlobalPosition = new Vector3(5, 0, 2);
			await SettlePhysics(level);
			player.GlobalRotation = Vector3.Zero;

			ulong fire = 1UL << playerEntity.Input!.Actions.IndexOf("skill_1");
			playerEntity.Input.Submit(new InputFrame { Held = fire, Pressed = fire });
			RunTicks(level, world, 1);
			long shot = world.History.CurrentTick!.Value;

			CharacterBody3D projectile = spawned.GetChildren().OfType<CharacterBody3D>()
				.Single(x => x.SceneFilePath == ProjectilePath);
			long? hit = null;

			for (int i = 0; i < 60 && hit is null; i++)
			{
				RunTicks(level, world, 1);

				if (!projectile.Visible)
				{
					hit = world.History.CurrentTick;
				}
			}

			hit.Should().NotBeNull("the projectile reached the target");
			health.CurrentValue.Should().Be(90);

			world.Seek(shot - 1);

			projectile.Visible.Should().BeFalse("the projectile waits in limbo before it was fired");
			health.CurrentValue.Should().Be(100);

			world.Seek((shot + hit!.Value) / 2);

			projectile.Visible.Should().BeTrue("the projectile is back in flight");
			projectile.GlobalPosition.Z.Should().BeInRange(target.GlobalPosition.Z, player.GlobalPosition.Z);
			health.CurrentValue.Should().Be(100);

			// Resumed from mid-flight, the same shot lands again.
			await SettlePhysics(level);

			for (int i = 0; i < 60 && projectile.Visible; i++)
			{
				RunTicks(level, world, 1);
			}

			projectile.Visible.Should().BeFalse();
			health.CurrentValue.Should().Be(90);
		}
		finally
		{
			level.Free();
		}
	}

	[TestCase(Timeout = 120000)]
	[RequireGodotRuntime]
	public async Task A_burning_enemy_s_flames_come_and_go_with_the_timeline()
	{
		Node level = GD.Load<PackedScene>(LevelPath).Instantiate();
		Root.AddChild(level);

		try
		{
			await SettlePhysics(level);

			ForgeWorldHistory world = level.GetNode<ForgeWorldHistory>("World History");
			CharacterBody3D enemy = level.GetNode("%Spawned").GetChildren().OfType<CharacterBody3D>().First();
			ForgeEntity enemyEntity = enemy.GetNode<ForgeEntity>("%Forge Entity");
			var fire = Tag.RequestTag(ForgeManagers.Instance.TagsManager, "effect.fire");

			level.GetNode<CharacterBody3D>("Player").GlobalPosition = new Vector3(0, 0, 1.3f);
			enemy.GlobalPosition = new Vector3(1.2f, 0, 1.3f);
			await SettlePhysics(level);
			long? ignition = null;

			for (int i = 0; i < 200 && ignition is null; i++)
			{
				RunTicks(level, world, 1);

				if (enemyEntity.Tags.AllTags.HasTag(fire))
				{
					ignition = world.History.CurrentTick;
				}
			}

			ignition.Should().NotBeNull("the fire spread to the enemy");
			RunTicks(level, world, 10);
			await Present(level);

			Flames(enemy).Should().ContainSingle("its burning shows the fire cue");

			world.Seek(ignition!.Value - 1);
			await Present(level);

			Flames(enemy).Should().BeEmpty("it wasn't burning yet");

			world.Seek(ignition.Value + 5);
			await Present(level);

			Flames(enemy).Should().ContainSingle("it's burning again");
		}
		finally
		{
			level.Free();
		}
	}

	[TestCase(Timeout = 180000)]
	[RequireGodotRuntime]
	public async Task A_replay_of_a_fight_runs_every_tick_the_way_it_was_recorded()
	{
		Node level = GD.Load<PackedScene>(LevelPath).Instantiate();
		Root.AddChild(level);
		SceneTree tree = level.GetTree();

		try
		{
			await SettlePhysics(level);

			ForgeWorldHistory world = level.GetNode<ForgeWorldHistory>("World History");
			ForgeEntity playerEntity = level.GetNode<ForgeEntity>("Player/%Forge Entity");
			Node spawned = level.GetNode("%Spawned");
			CharacterBody3D[] enemies = [.. spawned.GetChildren().OfType<CharacterBody3D>()];
			var desyncs = new List<WorldDesync>();
			world.Desynced += desyncs.Add;

			// Set by hand before the stretch replayed, since a replay runs only the simulation: the player by the
			// bonfire and an enemy beside it.
			level.GetNode<CharacterBody3D>("Player").GlobalPosition = new Vector3(0, 0, 1.3f);
			enemies[0].GlobalPosition = new Vector3(1.2f, 0, 1.3f);
			await SettlePhysics(level, 120);
			long start = world.History.CurrentTick!.Value;

			// The enemy burns to death and is replaced, the fire spreads, and the player shoots and dashes.
			ulong shoot = 1UL << playerEntity.Input!.Actions.IndexOf("skill_1");
			ulong dash = 1UL << playerEntity.Input.Actions.IndexOf("skill_2");

			for (int i = 0; i < 260; i++)
			{
				if (i == 30)
				{
					playerEntity.Input.Submit(new InputFrame { Held = shoot, Pressed = shoot });
				}

				if (i == 120)
				{
					playerEntity.Input.Submit(new InputFrame { Held = dash, Pressed = dash });
				}

				await SettlePhysics(level);
			}

			Replacements(spawned, enemies).Should().NotBeEmpty("the stretch replayed has a death and a spawn");

			// From the start of the stretch, and from part way into it.
			foreach (long from in (long[])[start, start + 90, start + 200])
			{
				tree.Paused = true;
				world.Seek(from);
				world.Replaying = true;
				tree.Paused = false;

				for (int i = 0; i < 400 && world.History.CurrentTick < world.History.NewestTick; i++)
				{
					await SettlePhysics(level);
				}

				world.Replaying = false;
				world.History.CurrentTick.Should().Be(world.History.NewestTick, "the replay reached the present");
			}

			desyncs.Should().BeEmpty(string.Join(
				"\n\n",
				desyncs.Select(x => $"{x.Tick - start} ticks in:\n{x.Describe(BodyName)}")));
		}
		finally
		{
			level.Free();
			tree.Paused = false;
		}
	}

	[TestCase(Timeout = 60000)]
	[RequireGodotRuntime]
	public async Task Leaving_for_the_hub_from_the_paused_timeline_unpauses()
	{
		Node level = GD.Load<PackedScene>(LevelPath).Instantiate();
		Root.AddChild(level);
		SceneTree tree = level.GetTree();

		try
		{
			await SettlePhysics(level);

			var press = new InputEventAction { Action = "rewind", Pressed = true };
			level.GetNode<RewindTimeline>("Rewind")._Input(press);

			tree.Paused.Should().BeTrue();
			level.GetNode("QuitScene").CanProcess().Should().BeTrue("Esc leaves for the hub from the timeline too");
		}
		finally
		{
			level.Free();
		}

		tree.Paused.Should().BeFalse("the hub isn't left paused");
	}

	// Lets the physics server step, which is when it takes bodies that were put in place: added to the level, moved by
	// hand or by a seek.
	private static async Task SettlePhysics(Node level, int steps = 1)
	{
		for (int i = 0; i < steps; i++)
		{
			await level.ToSignal(level.GetTree(), SceneTree.SignalName.PhysicsFrame);
		}
	}

	// Lets a frame end, which is when cues and abilities are presented.
	private static async Task Present(Node level)
	{
		await level.ToSignal(level.GetTree(), SceneTree.SignalName.ProcessFrame);
		await level.ToSignal(level.GetTree(), SceneTree.SignalName.ProcessFrame);
	}

	private static IEnumerable<Node> Flames(Node body)
	{
		return body.GetChildren().Where(x => x.SceneFilePath == FireEffectPath);
	}

	// Steers the enemies and ticks the world, as a physics step does.
	private static void RunTicks(Node level, ForgeWorldHistory world, int ticks)
	{
		for (int i = 0; i < ticks; i++)
		{
			foreach (ChaseInput chase in ChaseInputs(level))
			{
				chase._PhysicsProcess(Step);
			}

			world._PhysicsProcess(Step);
		}
	}

	private static string BodyName(IForgeEntity entity)
	{
		return entity is Node node && GodotObject.IsInstanceValid(node) ? node.GetParent().Name : "a released entity";
	}

	private static IEnumerable<CharacterBody3D> Replacements(Node spawned, CharacterBody3D[] enemies)
	{
		return spawned.GetChildren().OfType<CharacterBody3D>().Except(enemies);
	}

	private static IEnumerable<ChaseInput> ChaseInputs(Node node)
	{
		foreach (Node child in node.GetChildren())
		{
			if (child is ChaseInput chase && chase.CanProcess())
			{
				yield return chase;
			}

			foreach (ChaseInput nested in ChaseInputs(child))
			{
				yield return nested;
			}
		}
	}
}
