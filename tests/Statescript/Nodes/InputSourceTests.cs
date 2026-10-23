// Copyright © Gamesmiths Guild.

using FluentAssertions;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Godot.Core.Statescript.Nodes.State;
using Gamesmiths.Forge.Godot.Core.Statescript.Resolvers;
using Gamesmiths.Forge.Godot.Nodes;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Nodes;
using Gamesmiths.Forge.Statescript.Nodes.Action;
using Gamesmiths.Forge.Statescript.Properties;
using GdUnit4;
using Godot;
using ForgeGraph = Gamesmiths.Forge.Statescript.Graph;
using InputAimResolver = Gamesmiths.Forge.Godot.Core.Statescript.Resolvers.InputAimResolver;
using InputAxisResolver = Gamesmiths.Forge.Godot.Core.Statescript.Resolvers.InputAxisResolver;
using NumericsVector2 = System.Numerics.Vector2;
using NumericsVector3 = System.Numerics.Vector3;

namespace Gamesmiths.Forge.Godot.Tests.Statescript.Nodes;

/// <summary>
/// The input nodes and resolvers read their Input Source entity's input, once per physics tick, when it has one, and
/// the local device only when it does not.
/// </summary>
[TestSuite]
public class InputSourceTests
{
	private const double Step = 1.0 / 60.0;

	private const string Jump = "forge_test_jump";
	private const string Left = "forge_test_left";
	private const string Right = "forge_test_right";
	private const string Up = "forge_test_up";
	private const string Down = "forge_test_down";

	private static Window Root => ((SceneTree)Engine.GetMainLoop()).Root;

	[TestCase]
	[RequireGodotRuntime]
	public void The_input_action_node_sees_a_tap_shorter_than_a_tick_in_its_source_input()
	{
		ForgeEntity entity = CreateEntity();

		try
		{
			GraphProcessor processor = WatchJump(entity);

			entity.Input!.Submit(new InputFrame { Pressed = 0b1, Released = 0b1 });
			EntitySimulation.Tick(entity, 1, Step);
			processor.FixedUpdateGraph(Step);

			Flag(processor, "pressed").Should().BeTrue();
			Flag(processor, "released").Should().BeTrue();
		}
		finally
		{
			entity.Free();
		}
	}

	[TestCase]
	[RequireGodotRuntime]
	public void A_button_held_in_the_source_input_when_the_watch_begins_is_not_a_press()
	{
		ForgeEntity entity = CreateEntity();

		try
		{
			entity.Input!.Submit(new InputFrame { Held = 0b1, Pressed = 0b1 });
			EntitySimulation.Tick(entity, 1, Step);
			GraphProcessor processor = WatchJump(entity);

			EntitySimulation.Tick(entity, 2, Step);
			processor.FixedUpdateGraph(Step);

			Flag(processor, "pressed").Should().BeFalse();
		}
		finally
		{
			entity.Free();
		}
	}

	[TestCase]
	[RequireGodotRuntime]
	public void A_source_with_input_is_never_read_from_the_device()
	{
		ForgeEntity entity = CreateEntity();
		InputMap.AddAction(Jump);

		try
		{
			GraphProcessor processor = WatchJump(entity);

			Input.ActionPress(Jump);
			processor.UpdateGraph(Step);
			processor.FixedUpdateGraph(Step);

			Flag(processor, "pressed").Should().BeFalse("the device is pressed, but the entity's input is not");
		}
		finally
		{
			Input.ActionRelease(Jump);
			InputMap.EraseAction(Jump);
			entity.Free();
		}
	}

	[TestCase]
	[RequireGodotRuntime]
	public void A_source_without_input_is_read_from_the_device_every_frame()
	{
		var entity = new ForgeEntity();
		Root.AddChild(entity);
		InputMap.AddAction(Jump);

		try
		{
			GraphProcessor processor = WatchJump(entity);

			Input.ActionPress(Jump);
			processor.UpdateGraph(Step);

			Flag(processor, "pressed").Should().BeTrue();
		}
		finally
		{
			Input.ActionRelease(Jump);
			InputMap.EraseAction(Jump);
			entity.Free();
		}
	}

	[TestCase]
	[RequireGodotRuntime]
	public void The_input_resolvers_read_their_source_input()
	{
		ForgeEntity entity = CreateEntity();

		try
		{
			entity.Input!
				.Submit(new InputFrame { Held = 0b1, Pressed = 0b1 }.WithVector(0, new NumericsVector2(0, -1)));
			EntitySimulation.Tick(entity, 1, Step);
			var source = new FixedEntityResolver(entity);
			var context = new GraphContext();

			new InputActionPressedResolver(source, Jump, InputActionMode.Pressed).Resolve(context).AsBool()
				.Should().BeTrue();
			new InputActionPressedResolver(source, Jump, InputActionMode.JustPressed).Resolve(context).AsBool()
				.Should().BeTrue();
			new InputActionPressedResolver(source, Jump, InputActionMode.JustReleased).Resolve(context).AsBool()
				.Should().BeFalse();
			new InputActionStrengthResolver(source, Up).Resolve(context).AsFloat().Should().Be(1);
			new InputAxisResolver(source, Up, Down).Resolve(context).AsFloat().Should().Be(-1);
			new InputVector2Resolver(source, Left, Right, Up, Down).Resolve(context).AsVector2()
				.Should().Be(new NumericsVector2(0, -1));
		}
		finally
		{
			entity.Free();
		}
	}

	[TestCase]
	[RequireGodotRuntime]
	public void The_mouse_and_camera_resolvers_read_their_source_inputs_aim()
	{
		ForgeEntity entity = CreateEntity();

		try
		{
			var source = new FixedEntityResolver(entity);
			var context = new GraphContext();
			var point = new InputAimResolver(source, new MouseWorldPosition2DResolver(), InputAimValue.Point2D);
			var direction = new InputAimResolver(source, new CameraForward3DResolver(), InputAimValue.Direction3D);

			EntitySimulation.Tick(entity, 1, Step);
			point.Resolve(context).AsVector2()
				.Should().Be(NumericsVector2.Zero, "an input without aim has none to give");

			entity.Input!
				.Submit(default(InputFrame).WithAim(new NumericsVector3(4, 5, 0), new NumericsVector3(0, 0, -1)));
			EntitySimulation.Tick(entity, 2, Step);

			point.Resolve(context).AsVector2().Should().Be(new NumericsVector2(4, 5));
			direction.Resolve(context).AsVector3().Should().Be(new NumericsVector3(0, 0, -1));
		}
		finally
		{
			entity.Free();
		}
	}

	private static ForgeEntity CreateEntity()
	{
		var entity = new ForgeEntity
		{
			Input = new EntityInput(new InputActionSet([Jump], [new InputVector(Left, Right, Up, Down)])),
		};

		Root.AddChild(entity);
		return entity;
	}

	private static GraphProcessor WatchJump(ForgeEntity entity)
	{
		var graph = new ForgeGraph();
		graph.VariableDefinitions.DefineObjectVariable<IForgeEntity>("source", entity);
		graph.VariableDefinitions.DefineVariable("yes", true);
		graph.VariableDefinitions.DefineVariable("pressed", false);
		graph.VariableDefinitions.DefineVariable("released", false);

		var watch = new InputActionNode(Jump);
		watch.BindInput(Forge.Statescript.Nodes.State.InputActionNode.InputSourceInput, "source");
		SetVariableNode pressed = Raise("pressed");
		SetVariableNode released = Raise("released");

		graph.AddNode(watch);
		graph.AddNode(pressed);
		graph.AddNode(released);
		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			watch.InputPorts[InputActionNode.InputPort]));
		graph.AddConnection(new Connection(
			watch.OutputPorts[Forge.Statescript.Nodes.State.InputActionNode.OnPressedPort],
			pressed.InputPorts[ActionNode.InputPort]));
		graph.AddConnection(new Connection(
			watch.OutputPorts[Forge.Statescript.Nodes.State.InputActionNode.OnReleasedPort],
			released.InputPorts[ActionNode.InputPort]));

		var processor = new GraphProcessor(graph);
		processor.StartGraph();
		return processor;
	}

	// Sets a graph variable to true when it executes, which is how a test sees that a port emitted.
	private static SetVariableNode Raise(string variable)
	{
		var node = new SetVariableNode();
		node.BindInput(SetVariableNode.SourceInput, "yes");
		node.BindOutput(SetVariableNode.TargetOutput, variable);
		return node;
	}

	private static bool Flag(GraphProcessor processor, string variable)
	{
		return processor.GraphContext.GraphVariables.TryGetVar(variable, out bool value) && value;
	}

	private sealed class FixedEntityResolver(IForgeEntity entity) : ObjectResolver<IForgeEntity>, IEntityResolver
	{
		private readonly IForgeEntity _entity = entity;

		public override IForgeEntity Resolve(GraphContext graphContext)
		{
			return _entity;
		}
	}
}
