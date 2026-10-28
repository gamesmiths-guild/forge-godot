// Copyright © Gamesmiths Guild.

using System.Threading.Tasks;
using FluentAssertions;
using Gamesmiths.Forge.Abilities;
using Gamesmiths.Forge.Attributes;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Godot.Core;
using Gamesmiths.Forge.Godot.Core.Statescript.Interop;
using Gamesmiths.Forge.Godot.Core.Statescript.Nodes.State;
using Gamesmiths.Forge.Godot.Nodes;
using Gamesmiths.Forge.Godot.Resources;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Nodes;
using Gamesmiths.Forge.Statescript.Nodes.State;
using Gamesmiths.Forge.Tags;
using GdUnit4;
using Godot;
using ForgeGraph = Gamesmiths.Forge.Statescript.Graph;
using Node = Godot.Node;
using NumericsVector2 = System.Numerics.Vector2;

namespace Gamesmiths.Forge.Godot.Tests.Nodes;

/// <summary>
/// A <see cref="ForgeEntity"/> rolls back and replays like any Forge entity: its body through its movement component,
/// and its Statescript abilities with their graphs and what those graphs show in the scene.
/// </summary>
[TestSuite]
public class ForgeEntityRollbackTests
{
	private const string SpeedKey = "RollbackSpeedAttributes.Speed";

	private const string BlockTag = "movement.block";

	private const int Ticks = 40;

	private static Window Root => ((SceneTree)Engine.GetMainLoop()).Root;

	// Move and slide moves by Godot's physics step, so the ticks use the same one.
	private static double Step => 1.0 / Engine.PhysicsTicksPerSecond;

	[TestCase]
	[RequireGodotRuntime]
	public async Task A_rolled_back_player_walks_and_dashes_the_same_way_again()
	{
		var world = new Node3D();
		Root.AddChild(world);

		try
		{
			await AddFloor(world);

			ForgeEntity entity = AddPlayer(world, out ForgeCharacterMovement3D movement);
			AbilityHandle dash = entity.Abilities.GrantAbilityPermanently(DashAbility(), 1, LevelComparison.None, null);
			var history = new EntityHistory(entity, Ticks + 1, Step);
			var states = new PlayerState[Ticks + 1];

			history.Record(0);
			states[0] = Describe(movement, dash);

			for (int tick = 1; tick <= Ticks; tick++)
			{
				entity.Input!.Submit(Frame(tick));
				EntitySimulation.Tick(entity, tick, Step);
				history.Record(tick);
				states[tick] = Describe(movement, dash);
			}

			states.Should().Contain(x => x.Dashing, "the dash ran");
			states[Ticks].Dashing.Should().BeFalse("the dash ended");

			for (int tick = 0; tick < Ticks; tick++)
			{
				history.Rollback(tick);
				Describe(movement, dash).Should().Be(states[tick], $"tick {tick} was rolled back to");

				for (int replayed = tick + 1; replayed <= Ticks; replayed++)
				{
					history.Replay(replayed);
					Describe(movement, dash)
						.Should().Be(states[replayed], $"tick {replayed} replayed from tick {tick}");
				}
			}
		}
		finally
		{
			world.Free();
		}
	}

	[TestCase]
	[RequireGodotRuntime]
	public void A_restored_ability_shows_what_its_graph_holds_in_the_next_presentation_pass()
	{
		var entity = new ForgeEntity();
		var light = new OmniLight3D { LightEnergy = 1.0f };
		Root.AddChild(entity);
		Root.AddChild(light);
		ForgeBootstrap bootstrap = Root.GetNode<ForgeBootstrap>("Forge Bootstrap");

		try
		{
			AbilityHandle glow = entity.Abilities.GrantAbilityPermanently(
				GlowAbility(light),
				1,
				LevelComparison.None,
				null);

			glow.TryActivate(out _).Should().BeTrue();
			bootstrap._Process(Step);
			light.LightEnergy.Should().Be(5.0f);

			EntitySnapshot snapshot = EntitySimulation.CaptureSnapshot(entity);
			RunTicks(entity, 1);
			bootstrap._Process(Step);

			glow.IsActive.Should().BeFalse();
			light.LightEnergy.Should().Be(1.0f, "the glow ended with its timer");

			EntitySimulation.RestoreSnapshot(entity, snapshot);

			glow.IsActive.Should().BeTrue();
			light.LightEnergy.Should().Be(1.0f, "a restore is silent");

			bootstrap._Process(Step);
			light.LightEnergy.Should().Be(5.0f, "the presentation pass shows the restored glow");

			RunTicks(entity, Ticks + 1);
			EntitySimulation.RestoreSnapshot(entity, snapshot);
			bootstrap._Process(Step);

			light.LightEnergy.Should().Be(5.0f, "the glow ended and came back between two passes, so nothing changed");

			RunTicks(entity, (2 * Ticks) + 1);
			bootstrap._Process(Step);

			glow.IsActive.Should().BeFalse();
			light.LightEnergy.Should().Be(1.0f, "the restored glow ended with its timer");
		}
		finally
		{
			entity.Free();
			light.Free();
		}
	}

	private static ForgeEntity AddPlayer(Node3D world, out ForgeCharacterMovement3D movement)
	{
		var body = new CharacterBody3D
		{
			MotionMode = CharacterBody3D.MotionModeEnum.Grounded,
			Position = new Vector3(0, 1, 0),
		};

		body.AddChild(new CollisionShape3D { Shape = new CapsuleShape3D() });
		movement = new ForgeCharacterMovement3D
		{
			SpeedAttribute = SpeedKey,
			BlockingTags = new ForgeTagContainer { ContainerTags = [BlockTag] },
		};

		var entity = new ForgeEntity();
		entity.AddChild(movement);
		body.AddChild(entity);
		world.AddChild(body);
		entity.Attributes.AddAttributeSet(new RollbackSpeedAttributes());
		entity.Input = new EntityInput(
			new InputActionSet(["dash"], [new InputVector("move_left", "move_right", "move_up", "move_down")]));
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

	// Walks right, presses dash on tick 10 while turning to walk forward, then walks left once the dash is over.
	private static InputFrame Frame(int tick)
	{
		NumericsVector2 move = tick switch
		{
			< 10 => new NumericsVector2(1, 0),
			< 25 => new NumericsVector2(0, -1),
			_ => new NumericsVector2(-1, 0),
		};

		InputFrame frame = default(InputFrame).WithVector(0, move);

		return tick == 10 ? frame with { Held = 1, Pressed = 1 } : frame;
	}

	// Holds the walk back with its tag while a quarter of a second timer runs.
	private static AbilityData DashAbility()
	{
		TagsManager tagsManager = ForgeManagers.Instance.TagsManager;
		var graph = new ForgeGraph();
		graph.VariableDefinitions.DefineVariable("duration", 0.25);
		AddTimer(graph);

		return new AbilityData(
			"Dash",
			activationOwnedTags: new TagContainer(tagsManager, [Tag.RequestTag(tagsManager, BlockTag)]),
			behaviorFactory: () => new GraphAbilityBehavior(graph),
			inputBinding: AbilityInputBinding.ForAction("dash"));
	}

	// Raises the light's energy while a half second timer runs.
	private static AbilityData GlowAbility(Node light)
	{
		var graph = new ForgeGraph();
		graph.VariableDefinitions.DefineVariable("duration", 0.5);
		graph.VariableDefinitions.DefineObjectVariable("light", light);
		graph.VariableDefinitions.DefineVariable("energy", 5.0);
		TimerNode timer = AddTimer(graph);

		var hold = new NodePropertyOverrideNode("light_energy", InteropValueType.Float);
		hold.BindInput(NodePropertyOverrideNode.NodeInput, "light");
		hold.BindInput(NodePropertyOverrideNode.ValueInput, "energy");
		graph.AddNode(hold);
		graph.AddConnection(new Connection(
			timer.OutputPorts[StateNode<TimerNodeContext>.SubgraphPort],
			hold.InputPorts[StateNode<StateNodeContext>.InputPort]));

		return new AbilityData("Glow", behaviorFactory: () => new GraphAbilityBehavior(graph));
	}

	private static TimerNode AddTimer(ForgeGraph graph)
	{
		var timer = new TimerNode();
		timer.BindInput(TimerNode.DurationInput, "duration");
		graph.AddNode(timer);
		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			timer.InputPorts[StateNode<TimerNodeContext>.InputPort]));
		return timer;
	}

	private static void RunTicks(ForgeEntity entity, int firstTick)
	{
		for (int tick = firstTick; tick < firstTick + Ticks; tick++)
		{
			EntitySimulation.Tick(entity, tick, Step);
		}
	}

	private static PlayerState Describe(ForgeCharacterMovement3D movement, AbilityHandle dash)
	{
		return new PlayerState(movement.CaptureState(), dash.IsActive);
	}

	private readonly record struct PlayerState(BodyState Body, bool Dashing);

	private sealed class RollbackSpeedAttributes : AttributeSet
	{
		public EntityAttribute Speed { get; }

		public RollbackSpeedAttributes()
		{
			Speed = InitializeAttribute(nameof(Speed), 500, 0, 2000, decimalPlaces: 2);
		}
	}
}
