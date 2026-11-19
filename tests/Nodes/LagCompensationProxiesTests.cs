// Copyright © Gamesmiths Guild.

using System.Threading.Tasks;
using FluentAssertions;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Godot.Core;
using Gamesmiths.Forge.Godot.Core.Statescript.Physics;
using Gamesmiths.Forge.Godot.Nodes;
using Gamesmiths.Forge.Networking.Replication;
using GdUnit4;
using Godot;
using Godot.Collections;
using NumericsQuaternion = System.Numerics.Quaternion;
using NumericsVector3 = System.Numerics.Vector3;

namespace Gamesmiths.Forge.Godot.Tests.Nodes;

[TestSuite]
public class LagCompensationProxiesTests
{
	private static Window Root => ((SceneTree)Engine.GetMainLoop()).Root;

	[TestCase]
	[RequireGodotRuntime]
	public async Task A_body_stands_where_the_instigator_saw_it_for_queries_until_restored_3D()
	{
		var world = new Node3D();
		Root.AddChild(world);
		var body = new CharacterBody3D { CollisionLayer = 4, Position = new Vector3(5, 0, 0) };
		body.AddChild(new CollisionShape3D { Shape = new SphereShape3D { Radius = 0.5f } });
		var entity = new ForgeEntity();
		body.AddChild(entity);
		world.AddChild(body);
		await world.ToSignal(world.GetTree(), SceneTree.SignalName.PhysicsFrame);
		await world.ToSignal(world.GetTree(), SceneTree.SignalName.PhysicsFrame);
		PhysicsDirectSpaceState3D space = world.GetWorld3D().DirectSpaceState;
		var proxies = new LagCompensationProxies();

		try
		{
			proxies.Place([new RewoundBody(entity, Body(NumericsVector3.Zero))]);

			Ray(space, 0).Should().BeSameAs(body, "a hit on where it was reports the entity's own body");
			Ray(space, 5).Should().BeNull("the live body is out of the queries meanwhile");

			proxies.Restore();

			Ray(space, 0).Should().BeNull();
			Ray(space, 5).Should().BeSameAs(body);
			body.CollisionLayer.Should().Be(4);
			body.Position.Should().Be(new Vector3(5, 0, 0));
		}
		finally
		{
			proxies.Clear();
			world.QueueFree();
		}
	}

	[TestCase]
	[RequireGodotRuntime]
	public async Task A_body_stands_where_the_instigator_saw_it_for_queries_until_restored_2D()
	{
		var world = new Node2D();
		Root.AddChild(world);
		var body = new CharacterBody2D { CollisionLayer = 4, Position = new Vector2(100, 0) };
		body.AddChild(new CollisionShape2D { Shape = new CircleShape2D { Radius = 10 } });
		var entity = new ForgeEntity();
		body.AddChild(entity);
		world.AddChild(body);
		await world.ToSignal(world.GetTree(), SceneTree.SignalName.PhysicsFrame);
		await world.ToSignal(world.GetTree(), SceneTree.SignalName.PhysicsFrame);
		PhysicsDirectSpaceState2D space = world.GetWorld2D().DirectSpaceState;
		var proxies = new LagCompensationProxies();

		try
		{
			proxies.Place([new RewoundBody(entity, Body(NumericsVector3.Zero))]);

			Ray(space, 0).Should().BeSameAs(body);
			Ray(space, 100).Should().BeNull();

			proxies.Restore();

			Ray(space, 0).Should().BeNull();
			Ray(space, 100).Should().BeSameAs(body);
		}
		finally
		{
			proxies.Clear();
			world.QueueFree();
		}
	}

	[TestCase]
	[RequireGodotRuntime]
	public async Task A_stand_in_takes_the_body_s_shapes_scale_layer_and_exclusions_as_they_are()
	{
		var world = new Node3D();
		Root.AddChild(world);
		var body = new StaticBody3D { CollisionLayer = 4, Position = new Vector3(5, 0, 0), Scale = Vector3.One * 2 };
		var shape = new CollisionShape3D { Shape = new SphereShape3D { Radius = 0.5f } };
		body.AddChild(shape);
		var entity = new ForgeEntity();
		body.AddChild(entity);
		world.AddChild(body);
		await world.ToSignal(world.GetTree(), SceneTree.SignalName.PhysicsFrame);
		await world.ToSignal(world.GetTree(), SceneTree.SignalName.PhysicsFrame);
		PhysicsDirectSpaceState3D space = world.GetWorld3D().DirectSpaceState;
		var proxies = new LagCompensationProxies();
		var excluded = new Array<Rid>();

		try
		{
			proxies.Place([new RewoundBody(entity, Body(NumericsVector3.Zero))]);

			Ray(space, 0.8f).Should().BeSameAs(body, "it stands in at the body's scale");
			PhysicsQuery3D.TryCollectExclusions([entity], excluded).Should().BeTrue();
			Ray(space, 0, exclude: excluded)
				.Should().BeNull("a query that leaves the entity out leaves its stand-in out");

			body.CollisionLayer = 8;

			QueryRay(world, 5, 8).Should().BeNull("the live body stays out while it's stood in for");
			QueryRay(world, 0, 8).Should().BeSameAs(body, "its stand-in takes the layer it was given");

			body.CollisionLayer = 0;

			QueryRay(world, 0, 8).Should().BeNull("a body taken off every layer is out where it stands in too");

			body.CollisionLayer = 8;

			proxies.Restore();

			Ray(space, 5, mask: 8).Should().BeSameAs(body, "what the hit set off changed its layer, and that stays");

			shape.Disabled = true;
			proxies.Place([new RewoundBody(entity, Body(NumericsVector3.Zero))]);

			Ray(space, 0, mask: 8).Should().BeNull("its shapes are copied as they are now");

			shape.Disabled = false;
			proxies.Restore();
			proxies.Place([new RewoundBody(entity, Body(NumericsVector3.Zero))]);
			proxies.Forget(entity);

			Ray(space, 5, mask: 8).Should().BeSameAs(body, "forgotten while placed, it gets its body back");
			proxies.Restore();
		}
		finally
		{
			proxies.Clear();
			world.QueueFree();
		}
	}

	// Through Forge's own query, which brings a layer set meanwhile to the stand-in first.
	private static Node3D? QueryRay(Node3D world, float x, uint mask)
	{
		return PhysicsQuery3D.TryRaycast(
			world.GetWorld3D(),
			new Vector3(x, 0, -3),
			new Vector3(0, 0, 1),
			6,
			mask,
			false,
			false,
			null,
			out RaycastResult3D result)
			? result.Node
			: null;
	}

	private static BodyState Body(NumericsVector3 position)
	{
		return new BodyState(position, NumericsQuaternion.Identity, NumericsVector3.Zero, true);
	}

	private static GodotObject? Ray(PhysicsDirectSpaceState3D space, float x, uint mask = 4, Array<Rid>? exclude = null)
	{
		Dictionary hit = space.IntersectRay(
			PhysicsRayQueryParameters3D.Create(new Vector3(x, 0, -3), new Vector3(x, 0, 3), mask, exclude));
		return hit.Count == 0 ? null : hit["collider"].AsGodotObject();
	}

	private static GodotObject? Ray(PhysicsDirectSpaceState2D space, float x)
	{
		Dictionary hit = space.IntersectRay(
			PhysicsRayQueryParameters2D.Create(new Vector2(x, -50), new Vector2(x, 50), 4));
		return hit.Count == 0 ? null : hit["collider"].AsGodotObject();
	}
}
