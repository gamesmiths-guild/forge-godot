// Copyright © Gamesmiths Guild.

using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Godot.Core.Statescript.Physics;
using Gamesmiths.Forge.Godot.Tests.Helpers;
using GdUnit4;
using Godot;
using Node = Godot.Node;

namespace Gamesmiths.Forge.Godot.Tests.Statescript;

/// <summary>
/// What an overlap finds comes out nearest the query first, whatever order the physics server keeps its bodies in, so
/// a tick and its replay take the same entities in the same order.
/// </summary>
[TestSuite]
public class PhysicsQueryTests
{
	private static Window Root => ((SceneTree)Engine.GetMainLoop()).Root;

	[TestCase(Timeout = 10000)]
	[RequireGodotRuntime]
	public async Task A_3D_overlap_finds_entities_nearest_first()
	{
		var world = new Node3D();
		Root.AddChild(world);

		try
		{
			IForgeEntity far = AddBox3D(world, new Vector3(4, 0, 0));
			IForgeEntity near = AddBox3D(world, new Vector3(1, 0, 0));
			IForgeEntity middle = AddBox3D(world, new Vector3(0, 0, -2.5f));
			await world.ToSignal(world.GetTree(), SceneTree.SignalName.PhysicsFrame);
			var found = new HashSet<IForgeEntity>();

			PhysicsQuery3D.CollectShapeOverlaps(
				world.GetWorld3D(),
				new SphereShape3D { Radius = 10 },
				Transform3D.Identity,
				uint.MaxValue,
				includeAreas: false,
				excluded: null,
				found);

			found.Should().Equal(near, middle, far);
		}
		finally
		{
			world.Free();
		}
	}

	[TestCase(Timeout = 10000)]
	[RequireGodotRuntime]
	public async Task A_2D_overlap_finds_entities_nearest_first()
	{
		var world = new Node2D();
		Root.AddChild(world);

		try
		{
			IForgeEntity far = AddBox2D(world, new Vector2(400, 0));
			IForgeEntity near = AddBox2D(world, new Vector2(100, 0));
			IForgeEntity middle = AddBox2D(world, new Vector2(0, -250));
			await world.ToSignal(world.GetTree(), SceneTree.SignalName.PhysicsFrame);
			var found = new HashSet<IForgeEntity>();

			PhysicsQuery2D.CollectShapeOverlaps(
				world.GetWorld2D(),
				new CircleShape2D { Radius = 1000 },
				Transform2D.Identity,
				uint.MaxValue,
				includeAreas: false,
				excluded: null,
				found);

			found.Should().Equal(near, middle, far);
		}
		finally
		{
			world.Free();
		}
	}

	// A box with an entity of its own, since an overlap counts entities rather than colliders.
	private static TestAbilityEntity AddBox3D(Node world, Vector3 position)
	{
		var body = new StaticBody3D { Position = position };
		body.AddChild(new CollisionShape3D { Shape = new BoxShape3D() });
		var entity = new TestAbilityEntity();
		entity.AddChild(body);
		world.AddChild(entity);
		return entity;
	}

	private static TestAbilityEntity AddBox2D(Node world, Vector2 position)
	{
		var body = new StaticBody2D { Position = position };
		body.AddChild(new CollisionShape2D { Shape = new RectangleShape2D { Size = new Vector2(20, 20) } });
		var entity = new TestAbilityEntity();
		entity.AddChild(body);
		world.AddChild(entity);
		return entity;
	}
}
