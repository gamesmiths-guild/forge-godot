// Copyright © Gamesmiths Guild.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Gamesmiths.Forge.Abilities;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Example;
using Gamesmiths.Forge.Godot.Core;
using Gamesmiths.Forge.Godot.Nodes;
using Gamesmiths.Forge.Godot.Resources;
using Gamesmiths.Forge.Godot.Resources.Abilities;
using Gamesmiths.Forge.Tags;
using GdUnit4;
using Godot;
using Node = Godot.Node;
using NumericsVector3 = System.Numerics.Vector3;

namespace Gamesmiths.Forge.Godot.Tests.Samples;

/// <summary>
/// The arena demo's scenes and settings, and its C# abilities played through the game loop.
/// </summary>
[TestSuite]
public class ArenaSampleTests
{
	private const string Arena = "res://forge_samples/demos/06_networked_arena/";

	private const uint Hitboxes = 16;

	private static readonly string[] _abilities =
	[
		"pistol", "rifle", "shotgun", "rocket_launcher", "shock_rifle", "next_weapon", "previous_weapon", "pistol_shot",
		"rifle_burst", "shotgun_blast", "rocket", "shock_beam", "shock_ball", "die", "dummy_die",
	];

	private static readonly string[] _hitscans =
	[
		"shots/pistol_bullet.tres", "shots/rifle_bullet.tres", "shots/shotgun_pellet.tres", "shots/shock_beam.tres",
	];

	private static readonly string[] _weapons =
	[
		"weapon.pistol", "weapon.rifle", "weapon.shotgun", "weapon.rocket_launcher", "weapon.shock_rifle",
	];

	private static readonly string[] _effects =
	[
		"effects/beam_damage", "effects/ball_damage", "effects/combo_damage", "effects/rocket_damage",
		"effects/pellet_damage", "effects/pistol_damage", "effects/rifle_damage", "effects/frag", "effects/death",
		"effects/respawn", "abilities/player_grants", "abilities/dummy_grants",
	];

	private static Window Root => ((SceneTree)Engine.GetMainLoop()).Root;

	private static SceneTree Tree => (SceneTree)Engine.GetMainLoop();

	[TestCase]
	[RequireGodotRuntime]
	public void The_level_loads_and_its_manifest_lists_what_the_server_spawns_launches_and_grants()
	{
		Node level = GD.Load<PackedScene>(Arena + "scenes/networked_arena.tscn").Instantiate();
		level.Free();

		ForgeNetworkManifest manifest = GD.Load<ForgeNetworkManifest>(Arena + "network_manifest.tres");
		manifest.Scenes.Should().Equal(Uid("scenes/player.tscn"));
		manifest.Projectiles.Should().BeEquivalentTo(Uid("shots/shock_ball.tres"), Uid("shots/rocket.tres"));
		manifest.Abilities.Should().BeEquivalentTo(_abilities.Select(x => Uid($"abilities/{x}.tres")));
		manifest.Effects.Should().BeEquivalentTo(_effects.Select(x => Uid($"{x}.tres")));
	}

	[TestCase("scenes/player.tscn")]
	[TestCase("scenes/dummy.tscn")]
	[RequireGodotRuntime]
	public void A_character_s_head_and_body_are_hitboxes_shots_meet_and_its_movement_doesn_t(string scene)
	{
		Node character = GD.Load<PackedScene>(Arena + scene).Instantiate();

		try
		{
			ForgeHitbox3D[] hitboxes = [.. Descendants(character).OfType<ForgeHitbox3D>()];
			ForgeHitboxes3D tracker = character.GetChildren().OfType<ForgeHitboxes3D>().Single();

			hitboxes.Select(x => x.Name.ToString()).Should().BeEquivalentTo("Head Hitbox", "Body Hitbox");
			hitboxes.Should().OnlyContain(x => x.CollisionLayer == Hitboxes && x.CollisionMask == 0);
			hitboxes.Single(x => x.Name == "Head Hitbox").StrengthScale.Should().Be(2, "a headshot deals double");
			hitboxes.Single(x => x.Name == "Head Hitbox").Tags!.ContainerTags.Should().Equal("hitbox.head");
			tracker.Animators.Should().ContainSingle()
				.Which.ToString().Should().Be("../AnimationTree", "the lean poses the hitboxes");
			(((CollisionObject3D)character).CollisionMask & Hitboxes)
				.Should().Be(0, "its movement passes through hitboxes");
		}
		finally
		{
			character.Free();
		}

		foreach (string shot in _hitscans)
		{
			GD.Load<ForgeHitscanData>(Arena + shot).CollisionMask.Should().Be(1 | Hitboxes, "world and hitboxes");
		}

		foreach (string shot in new[] { "shots/shock_ball.tres", "shots/rocket.tres" })
		{
			GD.Load<ForgeProjectileData>(Arena + shot).CollisionMask.Should().Be(1 | Hitboxes, "world and hitboxes");
		}
	}

	[TestCase]
	[RequireGodotRuntime]
	public void A_crate_lets_hitscans_through_and_stops_projectiles()
	{
		StaticBody3D crate = GD.Load<PackedScene>(Arena + "scenes/crate.tscn").Instantiate<StaticBody3D>();
		var surface = (ForgeShotSurface)crate.GetMeta(ForgeShotSurface.MetaName).AsGodotObject();
		float thickness = ((BoxShape3D)crate.GetNode<CollisionShape3D>("Shape").Shape).Size.Z;
		crate.Free();

		foreach (string shot in _hitscans)
		{
			GD.Load<ForgeHitscanData>(Arena + shot).Power.Should().BeGreaterThan(surface.Resistance * thickness);
		}

		foreach (string shot in new[] { "shots/shock_ball.tres", "shots/rocket.tres" })
		{
			GD.Load<ForgeProjectileData>(Arena + shot).Power.Should().Be(0);
		}
	}

	[TestCase]
	[RequireGodotRuntime]
	public void Every_ability_is_a_coded_behavior_whose_context_comes_back_from_a_snapshot()
	{
		foreach (string name in _abilities)
		{
			ForgeAbilityData data = GD.Load<ForgeAbilityData>($"{Arena}abilities/{name}.tres");
			IAbilityBehavior behavior = data.AbilityBehavior!.GetBehavior();

			behavior.Should().BeAssignableTo<ISnapshotAbilityBehavior>($"{name} rolls back with its entity");
		}

		var fire = (ISnapshotAbilityBehavior)GD.Load<ForgeAbilityData>(Arena + "abilities/shock_beam.tres")
			.AbilityBehavior!.GetBehavior();
		var kept = new WeaponFire.FireContext { Wait = 0.25, BurstLeft = 2, Queued = true, Begun = true };
		fire.RestoreSnapshot(kept);
		var restored = (WeaponFire.FireContext)fire.CaptureSnapshot()!;
		restored.Should().NotBeSameAs(kept);
		restored.Should().BeEquivalentTo(kept);

		var die = (ISnapshotAbilityBehavior)GD.Load<ForgeAbilityData>(Arena + "abilities/die.tres")
			.AbilityBehavior!.GetBehavior();
		die.RestoreSnapshot(new Die.DieContext { Wait = 1.5 });
		((Die.DieContext)die.CaptureSnapshot()!).Wait.Should().Be(1.5);

		GD.Load<ForgeAbilityData>(Arena + "abilities/die.tres").ServerOnly.Should().BeTrue("a frag is the server's");
	}

	[TestCase]
	[RequireGodotRuntime]
	public async Task The_beam_doubles_on_a_head_goes_through_a_crate_weaker_and_stops_at_a_wall()
	{
		var world = new Node3D();
		Root.AddChild(world);

		try
		{
			AddFloor(world);
			Node3D player = Add(world, "scenes/player.tscn", Vector3.Zero);
			ForgeEntity body = Dummy(world, new Vector3(-3, 0, -8));
			ForgeEntity head = Dummy(world, new Vector3(3, 0, -8));
			ForgeEntity crated = Dummy(world, new Vector3(-2, 0, 8));
			ForgeEntity walled = Dummy(world, new Vector3(2, 0, 8));

			// Halfway along each line of fire.
			Add(world, "scenes/crate.tscn", new Vector3(-1, 0, 4));
			Add(world, "scenes/crate.tscn", new Vector3(-1, 1.2f, 4));
			AddWall(world, new Vector3(1, 1.5f, 4));
			Driver driver = Drive(player);

			await driver.Run(4);
			Holds(Entity(player)).Should().Equal(["weapon.pistol"], "the pistol is out from the start");
			await driver.Press("weapon_5");

			await driver.Fire(new Vector3(-3, 0.75f, -8));
			Health(body).Should().Be(660);

			await driver.Fire(new Vector3(3, 1.65f, -8));
			Health(head).Should().Be(320, "a headshot deals double");

			await driver.Fire(new Vector3(-2, 1.65f, 8));
			Health(crated)
				.Should().BeInRange(1000 - 280, 1000 - 240, "a crate's 1.2 units cost 0.6 of the beam's power");

			await driver.Fire(new Vector3(2, 1.65f, 8));
			Health(walled).Should().Be(1000, "concrete stops the beam");
		}
		finally
		{
			world.Free();
		}
	}

	[TestCase]
	[RequireGodotRuntime]
	public async Task The_pistol_fires_a_shot_a_press_and_the_rifle_a_burst_of_three_with_recoil()
	{
		var world = new Node3D();
		Root.AddChild(world);

		try
		{
			AddFloor(world);
			Node3D player = Add(world, "scenes/player.tscn", Vector3.Zero);
			ForgeEntity target = Dummy(world, new Vector3(0, 0, -6));
			Driver driver = Drive(player);
			await driver.Run(4);

			await driver.Fire(new Vector3(0, 0.75f, -6));
			Health(target).Should().Be(1000 - 120);

			await driver.Press("weapon_2");
			await driver.Fire(new Vector3(0, 0.75f, -6));
			Health(target).Should().Be(1000 - 120 - (3 * 100), "one press fires the whole burst");

			var burst = (HitscanWeapon)GD.Load<ForgeAbilityData>(Arena + "abilities/rifle_burst.tres")
				.AbilityBehavior!;
			burst.RecoilCue!.Tag.Should().Be("cue.arena.recoil");
		}
		finally
		{
			world.Free();
		}
	}

	[TestCase]
	[RequireGodotRuntime]
	public async Task The_keys_and_the_wheel_take_out_each_weapon_a_rocket_jumps_and_the_shotgun_spreads()
	{
		var world = new Node3D();
		Root.AddChild(world);

		try
		{
			AddFloor(world);
			Node3D player = Add(world, "scenes/player.tscn", Vector3.Zero);
			ForgeEntity target = Dummy(world, new Vector3(0, 0, -3));
			ForgeEntity entity = Entity(player);
			Driver driver = Drive(player);
			await driver.Run(4);

			for (int i = 0; i < _weapons.Length; i++)
			{
				await driver.Press($"weapon_{i + 1}");
				Holds(entity).Should().Equal(_weapons[i]);
			}

			await driver.Press("weapon_next");
			Holds(entity).Should().Equal(["weapon.pistol"], "the wheel goes round");
			await driver.Press("weapon_previous");
			Holds(entity).Should().Equal("weapon.shock_rifle");
			await driver.Press("weapon_3");

			await driver.Fire(new Vector3(0, 0.75f, -3));
			Health(target).Should().BeLessThanOrEqualTo(1000 - (4 * 70), "most of the pellets hit at three units");

			await driver.Press("weapon_4");
			float highest = 0;
			driver.Aim = new Vector3(0, -10, 0);
			driver.Hold("fire", 1);
			await driver.Run(40, () => highest = Math.Max(highest, player.GlobalPosition.Y));

			highest.Should().BeGreaterThan(1, "the rocket at its feet threw it up");
			Health(entity).Should().BeInRange(750, 950, "it took the blast's instigator share");
		}
		finally
		{
			world.Free();
		}
	}

	[TestCase]
	[RequireGodotRuntime]
	public async Task A_rocket_fired_against_a_wall_blasts_there_and_doesn_t_go_through()
	{
		var world = new Node3D();
		Root.AddChild(world);

		try
		{
			AddFloor(world);
			Node3D player = Add(world, "scenes/player.tscn", Vector3.Zero);
			ForgeEntity behind = Dummy(world, new Vector3(0, 0, -8));

			// Its face half a unit from the eye, closer than the rocket starts.
			AddWall(world, new Vector3(0, 1.5f, -0.75f));
			Driver driver = Drive(player);
			await driver.Run(4);
			await driver.Press("weapon_4");

			await driver.Fire(new Vector3(0, 1.6f, -8));

			Health(behind).Should().Be(1000, "the wall stopped the rocket");
			Health(Entity(player)).Should().BeLessThan(1000, "it blasted at the wall, by its shooter");
		}
		finally
		{
			world.Free();
		}
	}

	[TestCase]
	[RequireGodotRuntime]
	public async Task A_frag_counts_for_both_and_the_fragged_respawns_where_the_fragger_isn_t()
	{
		var world = new Node3D();
		Root.AddChild(world);

		try
		{
			AddFloor(world);
			Node3D fragger = Add(world, "scenes/player.tscn", Vector3.Zero);
			Node3D fragged = Add(world, "scenes/player.tscn", new Vector3(0, 0, -8));
			var near = new Marker3D { Position = new Vector3(2, 0, 2) };
			var far = new Marker3D { Position = new Vector3(0, 0, 20) };
			world.AddChild(near);
			world.AddChild(far);
			near.AddToGroup(ArenaGame.SpawnsGroup);
			far.AddToGroup(ArenaGame.SpawnsGroup);
			Driver driver = Drive(fragger);
			Drive(fragged);
			await driver.Run(4);
			await driver.Press("weapon_5");

			for (int i = 0; i < 3; i++)
			{
				await driver.Fire(new Vector3(0, 0.75f, -8));
			}

			ForgeEntity entity = Entity(fragged);
			Read(entity, "ArenaAttributes.Deaths").Should().Be(1);
			Read(Entity(fragger), "ArenaAttributes.Kills").Should().Be(1);
			entity.Tags.AllTags.HasTag(TagOf("state.dead")).Should().BeTrue();
			fragged.GlobalPosition.Y.Should().BeLessThan(-30, "it waits out of the fight");

			await driver.Run(170);

			entity.Tags.AllTags.HasTag(TagOf("state.dead")).Should().BeFalse();
			Health(entity).Should().Be(1000);
			fragged.GlobalPosition.Z.Should().BeApproximately(20, 0.5f, "the spawn furthest from the fragger");
		}
		finally
		{
			world.Free();
		}
	}

	private static string Uid(string file)
	{
		return ResourceUid.IdToText(ResourceLoader.GetResourceUid(Arena + file));
	}

	private static IEnumerable<Node> Descendants(Node node)
	{
		foreach (Node child in node.GetChildren())
		{
			yield return child;

			foreach (Node descendant in Descendants(child))
			{
				yield return descendant;
			}
		}
	}

	private static Node3D Add(Node3D world, string scene, Vector3 position)
	{
		Node3D node = GD.Load<PackedScene>(Arena + scene).Instantiate<Node3D>();
		node.Position = position;
		world.AddChild(node);
		return node;
	}

	private static ForgeEntity Dummy(Node3D world, Vector3 position)
	{
		StrafingDummy3D dummy = GD.Load<PackedScene>(Arena + "scenes/dummy.tscn").Instantiate<StrafingDummy3D>();
		dummy.Position = position;
		dummy.Distance = 0;
		world.AddChild(dummy);
		return Entity(dummy);
	}

	private static void AddFloor(Node3D world)
	{
		var floor = new StaticBody3D { Position = new Vector3(0, -0.5f, 0) };
		floor.AddChild(new CollisionShape3D { Shape = new BoxShape3D { Size = new Vector3(80, 1, 80) } });
		world.AddChild(floor);
	}

	private static void AddWall(Node3D world, Vector3 position)
	{
		var wall = new StaticBody3D { Position = position };
		wall.AddChild(new CollisionShape3D { Shape = new BoxShape3D { Size = new Vector3(1.5f, 3, 0.5f) } });
		world.AddChild(wall);
	}

	private static ForgeEntity Entity(Node node)
	{
		return node.GetNode<ForgeEntity>("Forge Entity");
	}

	private static int Health(IForgeEntity entity)
	{
		return Read(entity, "CharacterAttributes.Health");
	}

	private static int Read(IForgeEntity entity, string attribute)
	{
		return entity.Attributes[attribute].CurrentValue;
	}

	private static Tag TagOf(string name)
	{
		return Tag.RequestTag(ForgeManagers.Instance.TagsManager, name);
	}

	private static string[] Holds(ForgeEntity entity)
	{
		return [.. _weapons.Where(x => entity.Tags.AllTags.HasTag(TagOf(x)))];
	}

	// Takes a player's input from its collector, which reads the keyboard, to feed it frames instead.
	private static Driver Drive(Node3D player)
	{
		ForgeEntity entity = Entity(player);
		entity.GetNode("Input").Free();
		return new Driver(player, entity.Input!);
	}

	// Feeds a player's input a frame every physics tick, before the entities' ticks read it.
	private sealed class Driver
	{
		private readonly Node3D _player;
		private readonly EntityInput _input;
		private ulong _held;
		private ulong _pressed;

		public Vector3 Aim { get; set; } = new(0, 1.6f, -10);

		public Driver(Node3D player, EntityInput input)
		{
			_player = player;
			_input = input;
			Tree.PhysicsFrame += Submit;
			_player.TreeExiting += () => Tree.PhysicsFrame -= Submit;
		}

		public void Hold(string action, int ticks)
		{
			ulong bit = 1UL << _input.Actions.IndexOf(action);
			_held |= bit;
			_pressed |= bit;
			_ = Release(bit, ticks);
		}

		public async Task Press(string action)
		{
			Hold(action, 1);
			await Run(3);
		}

		// Aims at a point and fires once, then waits for the trigger to be ready again.
		public async Task Fire(Vector3 at)
		{
			Aim = at;
			Hold("fire", 1);
			await Run(70);
		}

		public async Task Run(int ticks, Action? eachTick = null)
		{
			for (int i = 0; i < ticks; i++)
			{
				await _player.ToSignal(Tree, SceneTree.SignalName.PhysicsFrame);
				eachTick?.Invoke();
			}
		}

		private async Task Release(ulong bit, int ticks)
		{
			await Run(ticks);
			_held &= ~bit;
		}

		private void Submit()
		{
			Vector3 eye = _player.GlobalPosition + new Vector3(0, 1.6f, 0);
			Vector3 direction = (Aim - eye).Normalized();
			InputFrame frame = new InputFrame { Held = _held, Pressed = _pressed }.WithAim(
				new NumericsVector3(Aim.X, Aim.Y, Aim.Z),
				new NumericsVector3(direction.X, direction.Y, direction.Z));
			_input.Submit(frame);
			_pressed = 0;
		}
	}
}
