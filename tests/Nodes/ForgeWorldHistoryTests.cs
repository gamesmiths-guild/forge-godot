// Copyright © Gamesmiths Guild.

using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Gamesmiths.Forge.Abilities;
using Gamesmiths.Forge.Attributes;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Effects;
using Gamesmiths.Forge.Effects.Duration;
using Gamesmiths.Forge.Effects.Magnitudes;
using Gamesmiths.Forge.Effects.Modifiers;
using Gamesmiths.Forge.Effects.Periodic;
using Gamesmiths.Forge.Godot.Core.Statescript.Nodes;
using Gamesmiths.Forge.Godot.Core.Statescript.Nodes.Action;
using Gamesmiths.Forge.Godot.Core.Statescript.Nodes.State;
using Gamesmiths.Forge.Godot.Nodes;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Nodes;
using GdUnit4;
using Godot;
using ForgeGraph = Gamesmiths.Forge.Statescript.Graph;
using Node = Godot.Node;

namespace Gamesmiths.Forge.Godot.Tests.Nodes;

/// <summary>
/// A <see cref="ForgeWorldHistory"/> advances every <see cref="ForgeEntity"/> under its parent and rolls them back
/// together, keeping entities that died or joined in limbo: hidden and out of processing and physics, until no tick
/// held has them.
/// </summary>
[TestSuite]
public class ForgeWorldHistoryTests
{
	private const double Step = 1.0 / 64.0;

	private const string HealthKey = "WorldHealthAttributes.Health";

	private static Window Root => ((SceneTree)Engine.GetMainLoop()).Root;

	[TestCase]
	[RequireGodotRuntime]
	public void Seeking_puts_every_entity_back_as_the_tick_left_it()
	{
		var level = new Node3D();
		ForgeEntity first = AddEntity(level, out _);
		ForgeEntity second = AddEntity(level, out _);
		ForgeWorldHistory world = AddWorld(level);

		try
		{
			Burn(first);
			var healths = new Dictionary<long, (int First, int Second)>();

			for (int i = 0; i < 24; i++)
			{
				if (i == 8)
				{
					Burn(second);
				}

				world._PhysicsProcess(Step);
				healths[world.History.CurrentTick!.Value] = (HealthOf(first), HealthOf(second));
			}

			healths.Values.Should().Contain(x => x.First < 100 && x.Second < 100, "both burned");

			foreach (long tick in healths.Keys)
			{
				world.Seek(tick);

				(HealthOf(first), HealthOf(second)).Should().Be(healths[tick], $"tick {tick} was sought");
			}
		}
		finally
		{
			level.Free();
		}
	}

	[TestCase]
	[RequireGodotRuntime]
	public void An_entity_of_the_world_advances_only_with_the_world()
	{
		var level = new Node3D();
		ForgeEntity entity = AddEntity(level, out _);
		ForgeWorldHistory world = AddWorld(level);

		try
		{
			Burn(entity);
			world._PhysicsProcess(Step);
			int health = HealthOf(entity);

			for (int i = 0; i < 8; i++)
			{
				entity._PhysicsProcess(Step);
			}

			entity.WorldHistory.Should().BeSameAs(world);
			HealthOf(entity).Should().Be(health);
		}
		finally
		{
			level.Free();
		}
	}

	[TestCase]
	[RequireGodotRuntime]
	public void A_despawned_entity_waits_in_limbo_hidden_and_out_of_processing()
	{
		var level = new Node3D();
		AddEntity(level, out _);
		ForgeEntity dying = AddEntity(level, out Node3D body);
		ForgeWorldHistory world = AddWorld(level);

		try
		{
			Burn(dying);
			RunTicks(world, 4);
			long alive = world.History.CurrentTick!.Value;
			int health = HealthOf(dying);

			world.Despawn(dying);
			RunTicks(world, 4);

			body.Visible.Should().BeFalse();
			body.ProcessMode.Should().Be(Node.ProcessModeEnum.Disabled);
			world.History.Entities.Should().NotContain(dying);
			HealthOf(dying).Should().Be(health, "nothing advances an entity in limbo");

			world.Seek(alive);

			body.Visible.Should().BeTrue();
			body.ProcessMode.Should().Be(Node.ProcessModeEnum.Inherit);
			world.History.Entities.Should().Contain(dying);

			world.Seek(alive + 2);

			body.Visible.Should().BeFalse();
			body.ProcessMode.Should().Be(Node.ProcessModeEnum.Disabled);
		}
		finally
		{
			level.Free();
		}
	}

	[TestCase]
	[RequireGodotRuntime]
	public void Queue_Free_on_an_entity_of_the_world_sends_it_to_limbo()
	{
		var level = new Node3D();
		ForgeEntity entity = AddEntity(level, out Node3D body);
		ForgeWorldHistory world = AddWorld(level);

		try
		{
			RunTicks(world, 2);
			AbilityHandle vanish = entity.Abilities.GrantAbilityPermanently(
				VanishAbility(body),
				1,
				LevelComparison.None,
				null);

			vanish.TryActivate(out _).Should().BeTrue();
			RunTicks(world, 1);

			GodotObject.IsInstanceValid(body).Should().BeTrue();
			body.IsQueuedForDeletion().Should().BeFalse();
			body.Visible.Should().BeFalse();
			world.History.Entities.Should().NotContain(entity);
		}
		finally
		{
			level.Free();
		}
	}

	[TestCase]
	[RequireGodotRuntime]
	public void A_spawned_entity_is_set_aside_before_it_joined_and_freed_when_its_ticks_are_left_behind()
	{
		var level = new Node3D();
		AddEntity(level, out _);
		ForgeWorldHistory world = AddWorld(level);

		try
		{
			RunTicks(world, 3);
			long before = world.History.CurrentTick!.Value;

			ForgeEntity spawned = AddEntity(level, out Node3D body);
			RunTicks(world, 3);

			spawned.WorldHistory.Should().BeSameAs(world);

			world.Seek(before);

			body.Visible.Should().BeFalse();

			world.Seek(before + 2);

			body.Visible.Should().BeTrue();

			world.Seek(before);
			RunTicks(world, 1);

			world.History.CurrentTick.Should().Be(before, "the first physics step after a seek lets physics catch up");

			RunTicks(world, 1);

			body.IsQueuedForDeletion().Should().BeTrue("the ticks it joined in were left behind");
		}
		finally
		{
			level.Free();
		}
	}

	[TestCase]
	[RequireGodotRuntime]
	public void Limbo_frees_an_entity_once_no_tick_held_has_it()
	{
		var level = new Node3D();
		AddEntity(level, out _);
		ForgeEntity dying = AddEntity(level, out Node3D body);
		ForgeWorldHistory world = AddWorld(level, seconds: 0.05);

		try
		{
			RunTicks(world, 2);
			world.Despawn(dying);
			RunTicks(world, world.History.Capacity - 1);

			body.IsQueuedForDeletion().Should().BeFalse("its last tick is still held");

			RunTicks(world, 1);

			body.IsQueuedForDeletion().Should().BeTrue();
		}
		finally
		{
			level.Free();
		}
	}

	[TestCase]
	[RequireGodotRuntime]
	public void An_entity_freed_some_other_way_is_forgotten()
	{
		var level = new Node3D();
		ForgeEntity staying = AddEntity(level, out _);
		AddEntity(level, out Node3D body);
		ForgeWorldHistory world = AddWorld(level);

		try
		{
			RunTicks(world, 2);
			long before = world.History.CurrentTick!.Value;

			body.Free();
			world.Seek(before);

			world.History.Entities.Should().Equal(staying);
		}
		finally
		{
			level.Free();
		}
	}

	[TestCase]
	[RequireGodotRuntime]
	public void Queue_Free_on_a_body_whose_entity_sits_below_a_plain_node_sends_it_to_limbo()
	{
		var level = new Node3D();
		var body = new Node3D();
		var holder = new Node();
		var entity = new ForgeEntity();
		holder.AddChild(entity);
		body.AddChild(holder);
		level.AddChild(body);
		entity.Ready += () => entity.Attributes.AddAttributeSet(new WorldHealthAttributes());
		ForgeWorldHistory world = AddWorld(level);

		try
		{
			RunTicks(world, 2);
			AbilityHandle vanish = entity.Abilities.GrantAbilityPermanently(
				VanishAbility(body),
				1,
				LevelComparison.None,
				null);

			vanish.TryActivate(out _).Should().BeTrue();
			RunTicks(world, 1);

			body.IsQueuedForDeletion().Should().BeFalse();
			body.Visible.Should().BeFalse();
		}
		finally
		{
			level.Free();
		}
	}

	[TestCase]
	[RequireGodotRuntime]
	public void An_entity_moved_out_from_under_the_world_leaves_it_and_can_join_another()
	{
		var level = new Node3D();
		ForgeEntity entity = AddEntity(level, out Node3D body);
		ForgeWorldHistory world = AddWorld(level);
		var otherLevel = new Node3D();
		ForgeWorldHistory other = AddWorld(otherLevel);

		try
		{
			RunTicks(world, 2);

			level.RemoveChild(body);
			RunTicks(world, 1);

			entity.WorldHistory.Should().BeNull();
			world.History.Entities.Should().NotContain(entity);

			otherLevel.AddChild(body);

			entity.WorldHistory.Should().BeSameAs(other);
			other.History.Entities.Should().Contain(entity);
		}
		finally
		{
			level.Free();
			otherLevel.Free();
		}
	}

	[TestCase]
	[RequireGodotRuntime]
	public void A_world_put_back_in_the_tree_takes_its_entities_back()
	{
		var level = new Node3D();
		ForgeEntity entity = AddEntity(level, out _);
		ForgeWorldHistory world = AddWorld(level);

		try
		{
			RunTicks(world, 2);

			level.RemoveChild(world);
			entity.WorldHistory.Should().BeNull();
			level.AddChild(world);

			entity.WorldHistory.Should().BeSameAs(world);
			ForgeEntity spawned = AddEntity(level, out _);
			spawned.WorldHistory.Should().BeSameAs(world);
		}
		finally
		{
			level.Free();
		}
	}

	[TestCase]
	[RequireGodotRuntime]
	public void An_entity_whose_processing_is_off_stays_as_it_is()
	{
		var level = new Node3D();
		ForgeEntity entity = AddEntity(level, out _);
		ForgeWorldHistory world = AddWorld(level);

		try
		{
			Burn(entity);
			entity.SetPhysicsProcess(false);
			RunTicks(world, 8);

			HealthOf(entity).Should().Be(100);
			world.History.Entities.Should().Contain(entity);

			entity.SetPhysicsProcess(true);
			RunTicks(world, 8);

			HealthOf(entity).Should().BeLessThan(100);
		}
		finally
		{
			level.Free();
		}
	}

	[TestCase]
	[RequireGodotRuntime]
	public void A_replay_finds_the_tick_changed_from_outside_the_simulation_and_waits_at_the_newest()
	{
		var level = new Node3D();
		AddEntity(level, out _);
		ForgeEntity second = AddEntity(level, out _);
		ForgeWorldHistory world = AddWorld(level);
		var desyncs = new List<WorldDesync>();
		world.Desynced += desyncs.Add;

		try
		{
			RunTicks(world, 12);

			// Between ticks, which a replay can't run again.
			Burn(second);
			RunTicks(world, 12);
			long newest = world.History.NewestTick!.Value;
			int health = HealthOf(second);

			world.Seek(newest - 20);
			world.Replaying = true;
			RunTicks(world, 30);

			desyncs.Select(x => x.Tick).Should().Equal(newest - 11);
			world.History.CurrentTick.Should().Be(newest, "a replay waits at the newest tick");
			HealthOf(second).Should().Be(health, "the world is as the recording left it");

			world.Replaying = false;
			RunTicks(world, 1);

			world.History.CurrentTick.Should().Be(newest + 1);
		}
		finally
		{
			level.Free();
		}
	}

	[TestCase]
	[RequireGodotRuntime]
	public void An_entity_spawned_again_into_a_tick_is_seeded_the_way_the_first_one_was()
	{
		var level = new Node3D();
		AddEntity(level, out _);
		ForgeWorldHistory world = AddWorld(level);

		try
		{
			RunTicks(world, 3);
			long before = world.History.CurrentTick!.Value;
			ForgeEntity first = AddEntity(level, out _);
			ForgeEntity second = AddEntity(level, out _);
			RunTicks(world, 2);

			world.Seek(before);
			ForgeEntity again = AddEntity(level, out _);

			first.SpawnTick.Should().Be(before + 1);
			first.SpawnSeed.Should().NotBeNull().And.NotBe(second.SpawnSeed, "each spawn of a tick has its own");
			again.SpawnSeed.Should().Be(first.SpawnSeed);
		}
		finally
		{
			level.Free();
		}
	}

	[TestCase]
	[RequireGodotRuntime]
	public void A_spawn_between_ticks_and_one_the_next_tick_makes_get_seeds_of_their_own()
	{
		var level = new Node3D();
		ForgeEntity spawner = AddEntity(level, out _);
		ForgeWorldHistory world = AddWorld(level);

		try
		{
			RunTicks(world, 3);
			ForgeEntity between = AddEntity(level, out _);
			ForgeEntity? during = null;
			spawner.Mover = new SpawningMover(() => during = AddEntity(level, out _));

			RunTicks(world, 1);

			during.Should().NotBeNull();
			during!.SpawnTick.Should().Be(between.SpawnTick);
			during.SpawnSeed.Should().NotBe(between.SpawnSeed);
		}
		finally
		{
			level.Free();
		}
	}

	[TestCase]
	[RequireGodotRuntime]
	public void A_seek_leaves_a_one_shot_that_played_out_alone()
	{
		var level = new Node3D();
		ForgeEntity entity = AddEntity(level, out Node3D body);
		var flames = new CpuParticles3D { Emitting = true };
		var spark = new CpuParticles3D { OneShot = true, Emitting = false };
		body.AddChild(flames);
		body.AddChild(spark);
		ForgeWorldHistory world = AddWorld(level);

		try
		{
			RunTicks(world, 2);
			world.Despawn(entity);
			RunTicks(world, 2);

			// Back from limbo, shown again.
			world.Seek(world.History.OldestTick!.Value);
			world.AfterPresentation();

			body.Visible.Should().BeTrue();
			flames.Emitting.Should().BeTrue();
			spark.Emitting.Should().BeFalse("a one-shot that played out isn't played again");
		}
		finally
		{
			level.Free();
		}
	}

	[TestCase]
	[RequireGodotRuntime]
	public void A_scene_node_sends_an_entity_it_spawned_to_limbo_rather_than_freeing_it()
	{
		var level = new Node3D();
		ForgeEntity entity = AddEntity(level, out _);
		ForgeWorldHistory world = AddWorld(level);

		var template = new Node3D();
		var templateEntity = new ForgeEntity();
		template.AddChild(templateEntity);
		templateEntity.Owner = template;
		var scene = new PackedScene();
		scene.Pack(template);
		template.Free();

		try
		{
			RunTicks(world, 2);
			AbilityHandle summon = entity.Abilities.GrantAbilityPermanently(
				SummonAbility(scene, level),
				1,
				LevelComparison.None,
				null);

			summon.TryActivate(out _).Should().BeTrue();
			RunTicks(world, 2);
			Node3D summoned = level.GetChildren().OfType<Node3D>().Last();
			long held = world.History.CurrentTick!.Value;

			summon.Cancel();
			RunTicks(world, 1);

			summoned.IsQueuedForDeletion().Should().BeFalse();
			summoned.Visible.Should().BeFalse("it waits in limbo");

			world.Seek(held);

			summoned.Visible.Should().BeTrue("the summon is back while the node held it");
		}
		finally
		{
			level.Free();
		}
	}

	private static ForgeEntity AddEntity(Node3D level, out Node3D body)
	{
		body = new Node3D();
		var entity = new ForgeEntity();
		body.AddChild(entity);
		level.AddChild(body);

		if (level.IsInsideTree())
		{
			entity.Attributes.AddAttributeSet(new WorldHealthAttributes());
		}
		else
		{
			entity.Ready += () => entity.Attributes.AddAttributeSet(new WorldHealthAttributes());
		}

		return entity;
	}

	private static ForgeWorldHistory AddWorld(Node3D level, double seconds = 1)
	{
		var world = new ForgeWorldHistory { HistorySeconds = seconds };
		level.AddChild(world);
		Root.AddChild(level);
		return world;
	}

	private static void RunTicks(ForgeWorldHistory world, int ticks)
	{
		for (int i = 0; i < ticks; i++)
		{
			world._PhysicsProcess(Step);
		}
	}

	private static int HealthOf(ForgeEntity entity)
	{
		return entity.Attributes[HealthKey].CurrentValue;
	}

	// Takes a point of health every sixteenth of a second, for half a second.
	private static void Burn(ForgeEntity entity)
	{
		var burning = new EffectData(
			"Burning",
			new DurationData(
				DurationType.HasDuration,
				new ModifierMagnitude(MagnitudeCalculationType.ScalableFloat, new ScalableFloat(0.5f))),
			[
				new Modifier(
					HealthKey,
					ModifierOperation.FlatBonus,
					new ModifierMagnitude(MagnitudeCalculationType.ScalableFloat, new ScalableFloat(-1))),
			],
			periodicData: new PeriodicData(
				new ScalableFloat(0.0625f),
				false,
				PeriodInhibitionRemovedPolicy.NeverReset));

		entity.EffectsManager.ApplyEffect(new Effect(burning, new EffectOwnership(entity, entity)));
	}

	// Frees the node it's given as it starts.
	private static AbilityData VanishAbility(Node body)
	{
		var graph = new ForgeGraph();
		graph.VariableDefinitions.DefineObjectVariable("body", body);

		var free = new QueueFreeNode();
		free.BindInput(QueueFreeNode.NodeInput, "body");
		graph.AddNode(free);
		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			free.InputPorts[ActionNode.InputPort]));

		return new AbilityData("Vanish", behaviorFactory: () => new GraphAbilityBehavior(graph));
	}

	// Holds an instance of the scene under the parent for as long as it runs.
	private static AbilityData SummonAbility(PackedScene scene, Node parent)
	{
		var graph = new ForgeGraph();
		graph.VariableDefinitions.DefineObjectVariable("scene", scene);
		graph.VariableDefinitions.DefineObjectVariable("parent", parent);

		var summon = new Scene3DNode(InstantiateParentMode.Node, passOwnership: false);
		summon.BindInput(SceneNodeBase.SceneInput, "scene");
		summon.BindInput(SceneNodeBase.ParentNodeInput, "parent");
		graph.AddNode(summon);
		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			summon.InputPorts[Scene3DNode.InputPort]));

		return new AbilityData("Summon", behaviorFactory: () => new GraphAbilityBehavior(graph));
	}

	// Spawns once, the first time it moves, which is during a tick.
	private sealed class SpawningMover(System.Action spawn) : IEntityMover
	{
		private bool _spawned;

		public void Move(double fixedDeltaTime)
		{
			if (!_spawned)
			{
				_spawned = true;
				spawn();
			}
		}

		public BodyState CaptureState()
		{
			return default;
		}

		public void RestoreState(in BodyState state)
		{
		}
	}

	private sealed class WorldHealthAttributes : AttributeSet
	{
		public EntityAttribute Health { get; }

		public WorldHealthAttributes()
		{
			Health = InitializeAttribute(nameof(Health), 100, 0, 100);
		}
	}
}
