// Copyright © Gamesmiths Guild.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Effects.Duration;
using Gamesmiths.Forge.Godot.Core.Shots;
using Gamesmiths.Forge.Godot.Nodes;
using Gamesmiths.Forge.Godot.Resources;
using Gamesmiths.Forge.Shots;
using GdUnit4;
using Godot;

namespace Gamesmiths.Forge.Godot.Tests.Nodes;

/// <summary>
/// Shots from <see cref="ForgeShots"/>, offline, in 3D and 2D: hitscans that go through what their penetration lets
/// them and hit hitboxes, lightweight projectiles that fly from their entity's ticks and blast where they land, and
/// shots that hit those projectiles.
/// </summary>
[TestSuite]
public class ForgeShotsTests
{
	private const uint WorldLayer = 1;
	private const uint CrateLayer = 2;
	private const uint HitboxLayer = 4;

	private static Window Root => ((SceneTree)Engine.GetMainLoop()).Root;

	private static double Step => 1.0 / Engine.PhysicsTicksPerSecond;

	[TestCase]
	[RequireGodotRuntime]
	public async Task A_shot_with_power_goes_through_a_crate_by_its_thickness_and_a_wall_stops_it()
	{
		var world = new Node3D();
		Root.AddChild(world);

		try
		{
			ForgeEntity shooter = AddEntity3D(world, Vector3.Zero);
			ForgeEntity target = AddEntity3D(world, new Vector3(0, 0, -10));
			StaticBody3D crate = AddBox3D(world, new Vector3(0, 1, -5), 1, WorldLayer);
			crate.SetMeta(ForgeShotSurface.MetaName, new ForgeShotSurface { Resistance = 10 });
			await world.ToSignal(world.GetTree(), SceneTree.SignalName.PhysicsFrame);

			var hits = new List<ShotHit3D>();
			ForgeShots.Hitscan3D(shooter, new Vector3(0, 1, 0), Vector3.Forward, Hitscan(power: 20), hits);

			hits.Select(x => (Node?)x.Collider).Should().Equal(crate, target.GetParent());
			hits[1].Scale.Should().BeApproximately(0.5f, 0.01f, "the crate's metre cost half of the shot's power");
			hits[1].Entity.Should().BeSameAs(target);
			target.EffectsManager.GetActiveEffects().Should().ContainSingle();

			crate.RemoveMeta(ForgeShotSurface.MetaName);
			hits.Clear();
			ForgeShots.Hitscan3D(shooter, new Vector3(0, 1, 0), Vector3.Forward, Hitscan(power: 20), hits);

			hits.Should().ContainSingle().Which.Collider.Should().Be(crate, "a wall has no surface to go through");
			target.EffectsManager.GetActiveEffects().Should().ContainSingle();
		}
		finally
		{
			world.Free();
		}
	}

	[TestCase]
	[RequireGodotRuntime]
	public async Task A_shot_goes_through_its_pierce_layers_keeping_a_share_each_time()
	{
		var world = new Node3D();
		Root.AddChild(world);

		try
		{
			ForgeEntity shooter = AddEntity3D(world, Vector3.Zero);
			ForgeEntity target = AddEntity3D(world, new Vector3(0, 0, -10));
			AddBox3D(world, new Vector3(0, 1, -4), 0.2f, CrateLayer);
			AddBox3D(world, new Vector3(0, 1, -6), 0.2f, CrateLayer);
			await world.ToSignal(world.GetTree(), SceneTree.SignalName.PhysicsFrame);

			ForgeHitscanData data = Hitscan(pierceMask: CrateLayer, maxPierces: 2);
			var hits = new List<ShotHit3D>();
			ForgeShots.Hitscan3D(shooter, new Vector3(0, 1, 0), Vector3.Forward, data, hits);

			hits.Should().HaveCount(3);
			hits[2].Entity.Should().BeSameAs(target);
			hits[2].Scale.Should().BeApproximately(0.25f, 0.001f);

			data.MaxPierces = 1;
			hits.Clear();
			ForgeShots.Hitscan3D(shooter, new Vector3(0, 1, 0), Vector3.Forward, data, hits);

			hits.Should().HaveCount(2, "the second crate is one more than it may go through");
		}
		finally
		{
			world.Free();
		}
	}

	[TestCase]
	[RequireGodotRuntime]
	public async Task A_head_hitbox_scales_the_damage_and_is_reported()
	{
		var world = new Node3D();
		Root.AddChild(world);

		try
		{
			ForgeEntity shooter = AddEntity3D(world, Vector3.Zero);
			ForgeEntity target = AddEntity3D(world, new Vector3(0, 0, -10));
			ForgeHitbox3D head = AddHead3D(target);
			await world.ToSignal(world.GetTree(), SceneTree.SignalName.PhysicsFrame);

			var hits = new List<ShotHit3D>();
			ForgeShots.Hitscan3D(
				shooter,
				new Vector3(0, 2.4f, 0),
				Vector3.Forward,
				Hitscan(mask: WorldLayer | HitboxLayer),
				hits);

			ShotHit3D hit = hits.Should().ContainSingle().Subject;
			hit.Hitbox.Should().BeSameAs(head);
			hit.Entity.Should().BeSameAs(target);
			hit.Scale.Should().Be(2);
		}
		finally
		{
			world.Free();
		}
	}

	[TestCase]
	[RequireGodotRuntime]
	public async Task Pellets_spread_alike_from_a_seeded_random()
	{
		var world = new Node3D();
		Root.AddChild(world);

		try
		{
			ForgeEntity shooter = AddEntity3D(world, Vector3.Zero);
			await world.ToSignal(world.GetTree(), SceneTree.SignalName.PhysicsFrame);

			shooter.Random.BeginTick(7);
			var first = new System.Numerics.Vector3[8];
			ForgeShots.Spread3D(shooter, Vector3.Forward, 5, first);

			shooter.Random.BeginTick(7);
			var second = new System.Numerics.Vector3[8];
			ForgeShots.Spread3D(shooter, Vector3.Forward, 5, second);

			second.Should().Equal(first);
			first.Distinct().Should().HaveCount(8);
		}
		finally
		{
			world.Free();
		}
	}

	[TestCase]
	[RequireGodotRuntime]
	public async Task A_projectile_flies_from_its_entity_s_ticks_hits_and_blasts_where_it_lands()
	{
		var world = new Node3D();
		Root.AddChild(world);

		try
		{
			ForgeEntity shooter = AddEntity3D(world, Vector3.Zero);
			ForgeEntity target = AddEntity3D(world, new Vector3(0, 0, -10));
			ForgeEntity bystander = AddEntity3D(world, new Vector3(1.5f, 0, -10));
			await world.ToSignal(world.GetTree(), SceneTree.SignalName.PhysicsFrame);

			ForgeProjectileData rocket = Projectile(speed: 40);
			rocket.Splash = Splash(radius: 3);

			EntitySimulation.BeginTick(shooter, 100);
			ProjectileFlight flight = ForgeShots.Launch3D(shooter, rocket, new Vector3(0, 1, 0), Vector3.Forward);

			for (long tick = 101; tick < 140 && shooter.Shots.Flights[0].IsFlying; tick++)
			{
				EntitySimulation.Tick(shooter, tick, Step);
			}

			ProjectileFlight ended = shooter.Shots.Flights.Single(x => x.Id == flight.Id);
			ended.End.Should().Be(FlightEnd.Impact);
			ended.EndPoint.Z.Should().BeApproximately(-9.5f, 0.05f, "it ends where it touched the capsule");
			target.EffectsManager.GetActiveEffects().Should().HaveCount(2, "the hit and the blast");
			bystander.EffectsManager.GetActiveEffects().Should().ContainSingle("the blast reached it");
			shooter.EffectsManager.GetActiveEffects().Should().BeEmpty("the blast was too far from the shooter");
		}
		finally
		{
			world.Free();
		}
	}

	[TestCase]
	[RequireGodotRuntime]
	public async Task A_blast_at_the_shooter_s_feet_pushes_it_up_as_a_rocket_jump()
	{
		var world = new Node3D();
		Root.AddChild(world);

		try
		{
			ForgeEntity shooter = AddEntity3D(world, Vector3.Zero, moves: true);
			AddBox3D(world, new Vector3(0, -0.5f, 0), 20, WorldLayer);
			await world.ToSignal(world.GetTree(), SceneTree.SignalName.PhysicsFrame);

			ForgeSplashData blast = Splash(radius: 3);
			blast.Knockback = 12;
			var hits = new List<SplashHit3D>();
			ForgeShots.Splash3D(shooter, new Vector3(0.5f, 0.05f, 0), blast, hits);

			SplashHit3D hit = hits.Should().ContainSingle().Subject;
			hit.Scale.Should().BeLessThan(blast.ScaleAt(hit.Distance), "the shooter takes its own share");
			((CharacterBody3D)shooter.GetParent()).Velocity.Y.Should().BePositive();
			hit.Knockback.X.Should().BeNegative("away from the blast");
		}
		finally
		{
			world.Free();
		}
	}

	[TestCase]
	[RequireGodotRuntime]
	public async Task A_shot_that_hits_projectiles_ends_one_where_its_shooter_saw_it_and_blasts()
	{
		var world = new Node3D();
		Root.AddChild(world);

		try
		{
			ForgeEntity shooter = AddEntity3D(world, Vector3.Zero);
			ForgeEntity target = AddEntity3D(world, new Vector3(1, 0, -2));
			await world.ToSignal(world.GetTree(), SceneTree.SignalName.PhysicsFrame);

			ForgeProjectileData ball = Projectile(speed: 4);
			ball.Shootable = true;
			ball.Radius = 0.5f;
			ball.StruckSplash = Splash(radius: 3);

			long tick = Core.ForgeManagers.Instance.Clock.Tick;
			EntitySimulation.BeginTick(shooter, tick - 20);
			ProjectileFlight flight = ForgeShots.Launch3D(shooter, ball, new Vector3(0, 1, 0), Vector3.Forward);

			for (long t = tick - 19; t < tick; t++)
			{
				EntitySimulation.Tick(shooter, t, Step);
			}

			EntitySimulation.BeginTick(shooter, tick);
			ForgeHitscanData beam = Hitscan();
			beam.HitsProjectiles = true;
			var hits = new List<ShotHit3D>();
			ForgeShots.Hitscan3D(shooter, new Vector3(0, 1, 0), Vector3.Forward, beam, hits);

			hits.Should().ContainSingle().Which.Flight!.Value.Id.Should().Be(flight.Id);
			shooter.Shots.Flights.Single().End.Should().Be(FlightEnd.Struck);
			target.EffectsManager.GetActiveEffects().Should().ContainSingle("the struck ball's blast reached it");
		}
		finally
		{
			world.Free();
		}
	}

	[TestCase]
	[RequireGodotRuntime]
	public async Task In_2D_a_shot_goes_through_a_crate_and_a_projectile_lands()
	{
		var world = new Node2D();
		Root.AddChild(world);

		try
		{
			ForgeEntity shooter = AddEntity2D(world, Vector2.Zero);
			ForgeEntity target = AddEntity2D(world, new Vector2(100, 0));
			var crate = new StaticBody2D { Position = new Vector2(50, 0), CollisionLayer = WorldLayer };
			crate.AddChild(new CollisionShape2D { Shape = new RectangleShape2D { Size = new Vector2(10, 40) } });
			crate.SetMeta(ForgeShotSurface.MetaName, new ForgeShotSurface { Resistance = 1 });
			world.AddChild(crate);
			await world.ToSignal(world.GetTree(), SceneTree.SignalName.PhysicsFrame);

			var hits = new List<ShotHit2D>();
			ForgeShots.Hitscan2D(shooter, Vector2.Zero, Vector2.Right, Hitscan(power: 40, range: 500), hits);

			hits.Select(x => (Node?)x.Collider).Should().Equal(crate, target.GetParent());
			hits[1].Scale.Should().BeApproximately(0.75f, 0.01f);

			ForgeProjectileData ball = Projectile(speed: 640);
			ball.CollisionMask = WorldLayer;
			ball.Radius = 2;
			EntitySimulation.BeginTick(shooter, 10);
			ForgeShots.Launch2D(shooter, ball, Vector2.Zero, Vector2.Right);

			for (long tick = 11; tick < 30; tick++)
			{
				EntitySimulation.Tick(shooter, tick, Step);
			}

			ProjectileFlight flight = shooter.Shots.Flights.Single();
			flight.End.Should().Be(FlightEnd.Impact);
			flight.EndPoint.X.Should().BeApproximately(45, 0.5f);
		}
		finally
		{
			world.Free();
		}
	}

	private static ForgeEntity AddEntity3D(Node3D world, Vector3 position, bool moves = false)
	{
		var body = new CharacterBody3D { Position = position, CollisionLayer = WorldLayer };
		body.AddChild(new CollisionShape3D { Shape = new CapsuleShape3D(), Position = new Vector3(0, 1, 0) });
		var entity = new ForgeEntity();

		if (moves)
		{
			entity.AddChild(new ForgeCharacterMovement3D());
		}

		body.AddChild(entity);
		world.AddChild(body);
		return entity;
	}

	private static ForgeEntity AddEntity2D(Node2D world, Vector2 position)
	{
		var body = new CharacterBody2D { Position = position, CollisionLayer = WorldLayer };
		body.AddChild(new CollisionShape2D { Shape = new CircleShape2D { Radius = 10 } });
		var entity = new ForgeEntity();
		body.AddChild(entity);
		world.AddChild(body);
		return entity;
	}

	private static StaticBody3D AddBox3D(Node3D world, Vector3 position, float depth, uint layer)
	{
		var box = new StaticBody3D { Position = position, CollisionLayer = layer };
		box.AddChild(new CollisionShape3D { Shape = new BoxShape3D { Size = new Vector3(4, 4, depth) } });
		world.AddChild(box);
		return box;
	}

	// A head above a capsule standing at the entity's feet, scaled to take double.
	private static ForgeHitbox3D AddHead3D(ForgeEntity entity)
	{
		var body = (Node3D)entity.GetParent();
		var head = new ForgeHitbox3D
		{
			Position = new Vector3(0, 2.4f, 0),
			CollisionLayer = HitboxLayer,
			DamageScale = 2,
		};
		head.AddChild(new CollisionShape3D { Shape = new SphereShape3D { Radius = 0.3f } });
		body.AddChild(head);
		body.AddChild(new ForgeHitboxes3D());
		return head;
	}

	private static ForgeHitscanData Hitscan(
		float power = 0,
		uint pierceMask = 0,
		int maxPierces = 1,
		uint mask = WorldLayer | CrateLayer,
		float range = 50)
	{
		return new ForgeHitscanData
		{
			Range = range,
			CollisionMask = mask,
			Power = power,
			PierceMask = pierceMask,
			MaxPierces = maxPierces,
			PierceScale = 0.5f,
			Effects = [Hit()],
		};
	}

	private static ForgeProjectileData Projectile(float speed)
	{
		return new ForgeProjectileData
		{
			Speed = speed,
			Radius = 0.2f,
			Lifetime = 5,
			CollisionMask = WorldLayer,
			Effects = [Hit()],
		};
	}

	private static ForgeSplashData Splash(float radius)
	{
		return new ForgeSplashData
		{
			Radius = radius,
			CollisionMask = WorldLayer,
			Effects = [Hit()],
		};
	}

	private static ForgeEffectData Hit()
	{
		return new ForgeEffectData { Name = "Hit", DurationType = DurationType.Infinite };
	}
}
