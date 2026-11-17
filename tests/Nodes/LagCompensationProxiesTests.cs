// Copyright © Gamesmiths Guild.

using System.Threading.Tasks;
using FluentAssertions;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Godot.Core;
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

	private static BodyState Body(NumericsVector3 position)
	{
		return new BodyState(position, NumericsQuaternion.Identity, NumericsVector3.Zero, true);
	}

	private static GodotObject? Ray(PhysicsDirectSpaceState3D space, float x)
	{
		Dictionary hit = space.IntersectRay(
			PhysicsRayQueryParameters3D.Create(new Vector3(x, 0, -3), new Vector3(x, 0, 3), 4));
		return hit.Count == 0 ? null : hit["collider"].AsGodotObject();
	}

	private static GodotObject? Ray(PhysicsDirectSpaceState2D space, float x)
	{
		Dictionary hit = space.IntersectRay(
			PhysicsRayQueryParameters2D.Create(new Vector2(x, -50), new Vector2(x, 50), 4));
		return hit.Count == 0 ? null : hit["collider"].AsGodotObject();
	}
}
