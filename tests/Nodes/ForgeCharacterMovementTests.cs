// Copyright © Gamesmiths Guild.

using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Gamesmiths.Forge.Attributes;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Effects;
using Gamesmiths.Forge.Effects.Components;
using Gamesmiths.Forge.Effects.Duration;
using Gamesmiths.Forge.Godot.Core;
using Gamesmiths.Forge.Godot.Nodes;
using Gamesmiths.Forge.Godot.Resources;
using Gamesmiths.Forge.Tags;
using GdUnit4;
using Godot;
using NumericsVector2 = System.Numerics.Vector2;
using NumericsVector3 = System.Numerics.Vector3;

namespace Gamesmiths.Forge.Godot.Tests.Nodes;

/// <summary>
/// How <see cref="ForgeCharacterMovement3D"/> and <see cref="ForgeCharacterMovement2D"/> walk a body from its entity's
/// input in the movement stage, and capture and restore it so ticks can be simulated again.
/// </summary>
[TestSuite]
public class ForgeCharacterMovementTests
{
	private const string SpeedKey = "TestSpeedAttributes.Speed";

	private const string BlockTag = "movement.block";

	// Right, then forward and left, then nothing, the way a player's stick moves.
	private static readonly NumericsVector2[] _recordedInput =
	[
		.. Enumerable.Repeat(new NumericsVector2(1, 0), 20),
		.. Enumerable.Repeat(new NumericsVector2(-0.5f, -0.5f), 20),
		.. Enumerable.Repeat(NumericsVector2.Zero, 20),
	];

	private static Window Root => ((SceneTree)Engine.GetMainLoop()).Root;

	// Move and slide moves by Godot's physics step, so the ticks use the same one.
	private static double Step => 1.0 / Engine.PhysicsTicksPerSecond;

	[TestCase]
	[RequireGodotRuntime]
	public void The_body_walks_at_its_speed_attribute_in_the_attributes_units()
	{
		var world = new Node3D();
		Root.AddChild(world);

		try
		{
			ForgeEntity entity = AddCharacter3D(
				world,
				CharacterBody3D.MotionModeEnum.Floating,
				out CharacterBody3D body,
				out ForgeCharacterMovement3D movement);

			entity.Mover.Should().BeSameAs(movement);

			for (int i = 0; i < 10; i++)
			{
				Walk(entity, new NumericsVector2(1, 0));
			}

			body.Velocity.Should().Be(new Vector3(5, 0, 0), "500 with two decimal places is 5 units per second");

			entity.RemoveChild(movement);
			movement.Free();

			entity.Mover.Should().BeNull("the component left the entity");
		}
		finally
		{
			world.Free();
		}
	}

	[TestCase]
	[RequireGodotRuntime]
	public async Task The_same_recorded_input_walks_the_body_the_same_way()
	{
		var world = new Node3D();
		Root.AddChild(world);

		try
		{
			await AddFloor(world);

			BodyState[] first = Record(world);
			BodyState[] second = Record(world);

			second.Should().Equal(first);
			first[^1].Grounded.Should().BeTrue("the body landed on the floor");
			first[^1].Position.X.Should().BePositive("the body walked right before it walked left");
		}
		finally
		{
			world.Free();
		}
	}

	[TestCase]
	[RequireGodotRuntime]
	public void Blocking_tags_stop_the_walk_and_the_turn_but_not_the_fall()
	{
		var world = new Node3D();
		Root.AddChild(world);

		try
		{
			ForgeEntity entity = AddCharacter3D(
				world,
				CharacterBody3D.MotionModeEnum.Grounded,
				out CharacterBody3D body,
				out ForgeCharacterMovement3D movement);
			movement.FaceAim = true;
			movement.BlockingTags = new ForgeTagContainer { ContainerTags = [BlockTag] };

			Walk(entity, new NumericsVector2(1, 0), aim: new NumericsVector3(1, 0, 0));
			body.Velocity.X.Should().BePositive();
			Faces(body, Vector3.Right).Should().BeTrue("it faces the aim");

			ActiveEffectHandle block = Block(entity);
			float fallSpeed = body.Velocity.Y;
			Walk(entity, new NumericsVector2(1, 0), aim: new NumericsVector3(0, 0, 1));

			body.Velocity.X.Should().Be(0);
			body.Velocity.Z.Should().Be(0);
			body.Velocity.Y.Should().BeLessThan(fallSpeed, "it still falls");
			Faces(body, Vector3.Right).Should().BeTrue("its facing stays put");

			entity.EffectsManager.RemoveEffect(block);
			Walk(entity, new NumericsVector2(1, 0), aim: new NumericsVector3(0, 0, 1));

			body.Velocity.X.Should().BePositive();
			Faces(body, Vector3.Back).Should().BeTrue("it faces the aim again");
		}
		finally
		{
			world.Free();
		}
	}

	[TestCase]
	[RequireGodotRuntime]
	public async Task Restoring_a_captured_body_replays_the_same_ticks()
	{
		var world = new Node3D();
		Root.AddChild(world);

		try
		{
			await AddFloor(world);

			ForgeEntity entity = AddCharacter3D(
				world, CharacterBody3D.MotionModeEnum.Grounded, out _, out ForgeCharacterMovement3D movement);

			foreach (NumericsVector2 move in _recordedInput.Take(10))
			{
				Walk(entity, move);
			}

			BodyState captured = movement.CaptureState();
			BodyState[] played = [.. _recordedInput.Skip(10).Select(move => WalkAndCapture(entity, movement, move))];

			movement.RestoreState(captured);
			movement.CaptureState().Should().Be(captured);

			BodyState[] replayed = [.. _recordedInput.Skip(10).Select(move => WalkAndCapture(entity, movement, move))];
			replayed.Should().Equal(played);
		}
		finally
		{
			world.Free();
		}
	}

	[TestCase]
	[RequireGodotRuntime]
	public void A_floating_2D_body_walks_both_axes_and_a_grounded_one_walks_and_falls()
	{
		var world = new Node2D();
		Root.AddChild(world);

		try
		{
			ForgeEntity topDown = AddCharacter2D(
				world, CharacterBody2D.MotionModeEnum.Floating, out CharacterBody2D topDownBody, out _);
			ForgeEntity sideView = AddCharacter2D(
				world, CharacterBody2D.MotionModeEnum.Grounded, out CharacterBody2D sideViewBody, out _);
			sideViewBody.Position = new Vector2(1000, 0);

			Walk(topDown, new NumericsVector2(0, 1));
			Walk(sideView, new NumericsVector2(1, -1));

			topDownBody.Velocity.Should().Be(new Vector2(0, 5));
			sideViewBody.Velocity.X.Should().Be(5);
			sideViewBody.Velocity.Y.Should().BePositive("it falls, and the up input doesn't lift it");
		}
		finally
		{
			world.Free();
		}
	}

	[TestCase]
	[RequireGodotRuntime]
	public void A_2D_body_turns_to_the_aim_and_comes_back_when_restored()
	{
		var world = new Node2D();
		Root.AddChild(world);

		try
		{
			ForgeEntity entity = AddCharacter2D(
				world,
				CharacterBody2D.MotionModeEnum.Floating,
				out CharacterBody2D body,
				out ForgeCharacterMovement2D movement);
			movement.FaceAim = true;

			Walk(entity, new NumericsVector2(1, 0));
			BodyState captured = movement.CaptureState();

			Walk(entity, new NumericsVector2(0, 1), aim: new NumericsVector3(0, 1, 0));
			body.Rotation.Should().BeApproximately(Mathf.Pi / 2, 1e-5f, "it faces the aim, along +Y");

			movement.RestoreState(captured);

			movement.CaptureState().Should().Be(captured);
			body.Rotation.Should().Be(0);
		}
		finally
		{
			world.Free();
		}
	}

	private static ForgeEntity AddCharacter3D(
		Node world,
		CharacterBody3D.MotionModeEnum mode,
		out CharacterBody3D body,
		out ForgeCharacterMovement3D movement)
	{
		body = new CharacterBody3D { MotionMode = mode, Position = new Vector3(0, 1, 0) };
		body.AddChild(new CollisionShape3D { Shape = new CapsuleShape3D() });
		movement = new ForgeCharacterMovement3D { SpeedAttribute = SpeedKey };
		return AddEntity(world, body, movement);
	}

	private static ForgeEntity AddCharacter2D(
		Node world,
		CharacterBody2D.MotionModeEnum mode,
		out CharacterBody2D body,
		out ForgeCharacterMovement2D movement)
	{
		body = new CharacterBody2D { MotionMode = mode };
		body.AddChild(new CollisionShape2D { Shape = new CircleShape2D() });
		movement = new ForgeCharacterMovement2D { SpeedAttribute = SpeedKey };
		return AddEntity(world, body, movement);
	}

	private static ForgeEntity AddEntity(Node world, Node body, Node movement)
	{
		var entity = new ForgeEntity();
		entity.AddChild(movement);
		body.AddChild(entity);
		world.AddChild(body);
		entity.Attributes.AddAttributeSet(new TestSpeedAttributes());
		entity.Input = new EntityInput(
			new InputActionSet([], [new InputVector("move_left", "move_right", "move_up", "move_down")]));
		return entity;
	}

	// A floor whose top is at zero, under a capsule standing at one. A body is in the broadphase only once physics has
	// stepped with it.
	private static async Task AddFloor(Node3D world)
	{
		var floor = new StaticBody3D { Position = new Vector3(0, -0.5f, 0) };
		floor.AddChild(new CollisionShape3D { Shape = new BoxShape3D { Size = new Vector3(100, 1, 100) } });
		world.AddChild(floor);

		await world.ToSignal(world.GetTree(), SceneTree.SignalName.PhysicsFrame);
	}

	private static BodyState[] Record(Node3D world)
	{
		ForgeEntity entity = AddCharacter3D(
			world,
			CharacterBody3D.MotionModeEnum.Grounded,
			out CharacterBody3D body,
			out ForgeCharacterMovement3D movement);

		try
		{
			return [.. _recordedInput.Select(move => WalkAndCapture(entity, movement, move))];
		}
		finally
		{
			body.Free();
		}
	}

	private static BodyState WalkAndCapture(ForgeEntity entity, ForgeCharacterMovement3D mover, NumericsVector2 move)
	{
		Walk(entity, move);
		return mover.CaptureState();
	}

	private static void Walk(ForgeEntity entity, NumericsVector2 move, NumericsVector3? aim = null)
	{
		InputFrame frame = default(InputFrame).WithVector(0, move);

		if (aim is NumericsVector3 direction)
		{
			frame = frame.WithAim(NumericsVector3.Zero, direction);
		}

		entity.Input!.Submit(frame);
		entity._PhysicsProcess(Step);
	}

	private static bool Faces(Node3D body, Vector3 direction)
	{
		return (-body.GlobalBasis.Z).IsEqualApprox(direction);
	}

	private static ActiveEffectHandle Block(ForgeEntity entity)
	{
		TagsManager tagsManager = ForgeManagers.Instance.TagsManager;
		var data = new EffectData(
			"Block",
			new DurationData(DurationType.Infinite),
			effectComponents:
			[
				new ModifierTagsEffectComponent(new TagContainer(tagsManager, [Tag.RequestTag(tagsManager, BlockTag)])),
			]);

		return entity.EffectsManager.ApplyEffect(new Effect(data, new EffectOwnership(entity, entity)))!;
	}

	private sealed class TestSpeedAttributes : AttributeSet
	{
		public EntityAttribute Speed { get; }

		public TestSpeedAttributes()
		{
			Speed = InitializeAttribute(nameof(Speed), 500, 0, 2000, decimalPlaces: 2);
		}
	}
}
