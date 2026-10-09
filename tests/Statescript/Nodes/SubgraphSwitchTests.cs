// Copyright © Gamesmiths Guild.

using System;
using System.Threading.Tasks;
using FluentAssertions;
using Gamesmiths.Forge.Abilities;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Godot.Core.Statescript.Nodes.State;
using Gamesmiths.Forge.Godot.Core.Statescript.Physics;
using Gamesmiths.Forge.Godot.Tests.Helpers;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Nodes;
using Gamesmiths.Forge.Statescript.Nodes.State;
using Gamesmiths.Forge.Statescript.Ports;
using GdUnit4;
using Godot;
using ForgeGraph = Gamesmiths.Forge.Statescript.Graph;
using ForgeNode = Gamesmiths.Forge.Statescript.Node;
using Node = Godot.Node;
using NumericsVector2 = System.Numerics.Vector2;
using NumericsVector3 = System.Numerics.Vector3;

namespace Gamesmiths.Forge.Godot.Tests.Statescript.Nodes;

/// <summary>
/// The nodes that swap one subgraph for another as what they watch changes, when ending the old subgraph stops the
/// graph on the way to the new one. Each runs in a real graph against a real scene, the physics ones as an ability of
/// an entity standing in it.
/// </summary>
[TestSuite]
public class SubgraphSwitchTests
{
	private static Window Root => ((SceneTree)Engine.GetMainLoop()).Root;

	[TestCase]
	[RequireGodotRuntime]
	public void Input_action_reports_no_release_when_leaving_its_pressed_subgraph_stops_the_graph()
	{
		const string action = "forge_test_switch";
		InputMap.AddAction(action);
		Input.ActionPress(action);

		try
		{
			var graph = new ForgeGraph();
			TimerNode pressed = WireSwitch(graph, new InputActionNode(action), InputActionNode.WhilePressedPort);

			var processor = new GraphProcessor(graph);
			processor.StartGraph();
			processor.GraphContext.GetNodeContext<TimerNodeContext>(pressed.NodeID).Active
				.Should().BeTrue("the action starts out pressed");

			Input.ActionRelease(action);

			processor.Invoking(x => x.UpdateGraph(1.0 / 60)).Should().NotThrow();
			processor.GraphContext.IsActive.Should().BeFalse("ending the pressed subgraph stopped the graph");
		}
		finally
		{
			Input.ActionRelease(action);
			InputMap.EraseAction(action);
		}
	}

	[TestCase]
	[RequireGodotRuntime]
	public async Task Ray_3D_switches_to_nothing_when_leaving_its_clear_subgraph_stops_the_graph()
	{
		var world = new Node3D();
		Root.AddChild(world);

		try
		{
			AddBox3D(world, new Vector3(0, 0, -5));

			var graph = new ForgeGraph();
			graph.VariableDefinitions.DefineVariable("origin", NumericsVector3.Zero);
			graph.VariableDefinitions.DefineVariable("direction", NumericsVector3.UnitZ);
			graph.VariableDefinitions.DefineVariable("distance", 10.0);

			var ray = new Ray3DNode();
			ray.BindInput(RaycastNodeParameters3D.OriginInput, "origin");
			ray.BindInput(RaycastNodeParameters3D.DirectionInput, "direction");
			ray.BindInput(RaycastNodeParameters3D.MaxDistanceInput, "distance");
			TimerNode clear = WireSwitch(graph, ray, Ray3DNode.WhileClearPort);

			await SwitchAfterTurning(world, graph, clear, x => x.SetVar("direction", -NumericsVector3.UnitZ));
		}
		finally
		{
			world.Free();
		}
	}

	[TestCase]
	[RequireGodotRuntime]
	public async Task Ray_2D_switches_to_nothing_when_leaving_its_clear_subgraph_stops_the_graph()
	{
		var world = new Node2D();
		Root.AddChild(world);

		try
		{
			AddBox2D(world, new Vector2(0, -50));

			var graph = new ForgeGraph();
			graph.VariableDefinitions.DefineVariable("origin", NumericsVector2.Zero);
			graph.VariableDefinitions.DefineVariable("direction", NumericsVector2.UnitY);
			graph.VariableDefinitions.DefineVariable("distance", 100.0);

			var ray = new Ray2DNode();
			ray.BindInput(RaycastNodeParameters2D.OriginInput, "origin");
			ray.BindInput(RaycastNodeParameters2D.DirectionInput, "direction");
			ray.BindInput(RaycastNodeParameters2D.MaxDistanceInput, "distance");
			TimerNode clear = WireSwitch(graph, ray, Ray2DNode.WhileClearPort);

			await SwitchAfterTurning(world, graph, clear, x => x.SetVar("direction", -NumericsVector2.UnitY));
		}
		finally
		{
			world.Free();
		}
	}

	[TestCase]
	[RequireGodotRuntime]
	public async Task Sweep_3D_switches_to_nothing_when_leaving_its_clear_subgraph_stops_the_graph()
	{
		var world = new Node3D();
		Root.AddChild(world);

		try
		{
			AddBox3D(world, new Vector3(0, 0, -5));

			var graph = new ForgeGraph();
			graph.VariableDefinitions.DefineObjectVariable<Shape3D>("shape", new SphereShape3D { Radius = 0.25f });
			graph.VariableDefinitions.DefineVariable("origin", NumericsVector3.Zero);
			graph.VariableDefinitions.DefineVariable("direction", NumericsVector3.UnitZ);
			graph.VariableDefinitions.DefineVariable("distance", 10.0);

			var sweep = new Sweep3DNode();
			sweep.BindInput(ShapecastNodeParameters3D.ShapeInput, "shape");
			sweep.BindInput(ShapecastNodeParameters3D.OriginInput, "origin");
			sweep.BindInput(ShapecastNodeParameters3D.DirectionInput, "direction");
			sweep.BindInput(ShapecastNodeParameters3D.MaxDistanceInput, "distance");
			TimerNode clear = WireSwitch(graph, sweep, Sweep3DNode.WhileClearPort);

			await SwitchAfterTurning(world, graph, clear, x => x.SetVar("direction", -NumericsVector3.UnitZ));
		}
		finally
		{
			world.Free();
		}
	}

	[TestCase]
	[RequireGodotRuntime]
	public async Task Sweep_2D_switches_to_nothing_when_leaving_its_clear_subgraph_stops_the_graph()
	{
		var world = new Node2D();
		Root.AddChild(world);

		try
		{
			AddBox2D(world, new Vector2(0, -50));

			var graph = new ForgeGraph();
			graph.VariableDefinitions.DefineObjectVariable<Shape2D>("shape", new CircleShape2D { Radius = 2 });
			graph.VariableDefinitions.DefineVariable("origin", NumericsVector2.Zero);
			graph.VariableDefinitions.DefineVariable("direction", NumericsVector2.UnitY);
			graph.VariableDefinitions.DefineVariable("distance", 100.0);

			var sweep = new Sweep2DNode();
			sweep.BindInput(ShapecastNodeParameters2D.ShapeInput, "shape");
			sweep.BindInput(ShapecastNodeParameters2D.OriginInput, "origin");
			sweep.BindInput(ShapecastNodeParameters2D.DirectionInput, "direction");
			sweep.BindInput(ShapecastNodeParameters2D.MaxDistanceInput, "distance");
			TimerNode clear = WireSwitch(graph, sweep, Sweep2DNode.WhileClearPort);

			await SwitchAfterTurning(world, graph, clear, x => x.SetVar("direction", -NumericsVector2.UnitY));
		}
		finally
		{
			world.Free();
		}
	}

	[TestCase]
	[RequireGodotRuntime]
	public async Task Line_of_sight_3D_switches_to_nothing_when_leaving_its_clear_subgraph_stops_the_graph()
	{
		var world = new Node3D();
		Root.AddChild(world);

		try
		{
			AddBox3D(world, new Vector3(0, 0, -5));

			var graph = new ForgeGraph();
			graph.VariableDefinitions.DefineVariable("from", NumericsVector3.Zero);
			graph.VariableDefinitions.DefineVariable("to", new NumericsVector3(0, 0, 10));

			var sight = new LineOfSight3DNode();
			sight.BindInput(LineOfSight3DNode.FromInput, "from");
			sight.BindInput(LineOfSight3DNode.ToInput, "to");
			TimerNode clear = WireSwitch(graph, sight, LineOfSight3DNode.WhileClearPort);

			await SwitchAfterTurning(world, graph, clear, x => x.SetVar("to", new NumericsVector3(0, 0, -10)));
		}
		finally
		{
			world.Free();
		}
	}

	[TestCase]
	[RequireGodotRuntime]
	public async Task Line_of_sight_2D_switches_to_nothing_when_leaving_its_clear_subgraph_stops_the_graph()
	{
		var world = new Node2D();
		Root.AddChild(world);

		try
		{
			AddBox2D(world, new Vector2(0, -50));

			var graph = new ForgeGraph();
			graph.VariableDefinitions.DefineVariable("from", NumericsVector2.Zero);
			graph.VariableDefinitions.DefineVariable("to", new NumericsVector2(0, 100));

			var sight = new LineOfSight2DNode();
			sight.BindInput(LineOfSight2DNode.FromInput, "from");
			sight.BindInput(LineOfSight2DNode.ToInput, "to");
			TimerNode clear = WireSwitch(graph, sight, LineOfSight2DNode.WhileClearPort);

			await SwitchAfterTurning(world, graph, clear, x => x.SetVar("to", new NumericsVector2(0, -100)));
		}
		finally
		{
			world.Free();
		}
	}

	[TestCase]
	[RequireGodotRuntime]
	public async Task Overlap_3D_switches_to_nothing_when_leaving_its_empty_subgraph_stops_the_graph()
	{
		var world = new Node3D();
		Root.AddChild(world);

		try
		{
			AddBox3D(world, new Vector3(0, 0, -5));

			var graph = new ForgeGraph();
			graph.VariableDefinitions.DefineObjectVariable<Shape3D>("shape", new BoxShape3D());
			graph.VariableDefinitions.DefineVariable("position", new NumericsVector3(0, 0, 5));

			var overlap = new Overlap3DNode(OverlapSourceMode.TransientShape);
			overlap.BindInput(Overlap3DNode.ShapeInput, "shape");
			overlap.BindInput(Overlap3DNode.PositionInput, "position");
			TimerNode empty = WireSwitch(graph, overlap, Overlap3DNode.WhileEmptyPort);

			await SwitchAfterTurning(world, graph, empty, x => x.SetVar("position", new NumericsVector3(0, 0, -5)));
		}
		finally
		{
			world.Free();
		}
	}

	[TestCase]
	[RequireGodotRuntime]
	public async Task Overlap_2D_switches_to_nothing_when_leaving_its_empty_subgraph_stops_the_graph()
	{
		var world = new Node2D();
		Root.AddChild(world);

		try
		{
			AddBox2D(world, new Vector2(0, -50));

			var graph = new ForgeGraph();
			graph.VariableDefinitions.DefineObjectVariable<Shape2D>("shape", new RectangleShape2D());
			graph.VariableDefinitions.DefineVariable("position", new NumericsVector2(0, 50));

			var overlap = new Overlap2DNode(OverlapSourceMode.TransientShape);
			overlap.BindInput(Overlap2DNode.ShapeInput, "shape");
			overlap.BindInput(Overlap2DNode.PositionInput, "position");
			TimerNode empty = WireSwitch(graph, overlap, Overlap2DNode.WhileEmptyPort);

			await SwitchAfterTurning(world, graph, empty, x => x.SetVar("position", new NumericsVector2(0, -50)));
		}
		finally
		{
			world.Free();
		}
	}

	// Starts the node from the entry and puts a timer under its old subgraph port that exits as it is disabled, so
	// ending that subgraph stops the graph on the way to the new one.
	private static TimerNode WireSwitch(ForgeGraph graph, ForgeNode node, byte oldSubgraphPort)
	{
		graph.VariableDefinitions.DefineVariable("childDuration", 10.0);

		var child = new TimerNode();
		child.BindInput(TimerNode.DurationInput, "childDuration");
		var exit = new ExitNode();
		graph.AddNode(node);
		graph.AddNode(child);
		graph.AddNode(exit);
		Connect(graph, graph.EntryNode.OutputPorts[EntryNode.OutputPort], node);
		Connect(graph, node.OutputPorts[oldSubgraphPort], child);
		Connect(graph, child.OutputPorts[TimerNode.OnDeactivatePort], exit);

		return child;
	}

	// Runs the graph as an ability of an entity standing in the world, turns what its switch watches, and steps the
	// graph once.
	private static async Task SwitchAfterTurning(
		Node world,
		ForgeGraph graph,
		TimerNode oldSubgraph,
		Action<Variables> turn)
	{
		var owner = new TestAbilityEntity();
		world.AddChild(owner);

		// A body is in the broadphase only once physics has stepped with it.
		await world.ToSignal(world.GetTree(), SceneTree.SignalName.PhysicsFrame);

		GraphAbilityBehavior? behavior = null;
		var ability = new AbilityData("Switch", behaviorFactory: () => behavior = new GraphAbilityBehavior(graph));
		owner.Abilities.GrantAbilityPermanently(ability, 1, LevelComparison.None, sourceEntity: null)
			.TryActivate(out _).Should().BeTrue();

		GraphProcessor processor = behavior!.Processor;
		processor.GraphContext.GetNodeContext<TimerNodeContext>(oldSubgraph.NodeID).Active
			.Should().BeTrue("the switch starts out on its old subgraph");

		turn(processor.GraphContext.GraphVariables);

		processor.Invoking(x => x.FixedUpdateGraph(1.0 / 60)).Should().NotThrow();
		processor.GraphContext.IsActive.Should().BeFalse("ending the old subgraph stopped the graph");
	}

	// A box with an entity of its own, since an overlap counts entities rather than colliders.
	private static void AddBox3D(Node world, Vector3 position)
	{
		var body = new StaticBody3D { Position = position };
		body.AddChild(new CollisionShape3D { Shape = new BoxShape3D() });
		var entity = new TestAbilityEntity();
		entity.AddChild(body);
		world.AddChild(entity);
	}

	private static void AddBox2D(Node world, Vector2 position)
	{
		var body = new StaticBody2D { Position = position };
		body.AddChild(new CollisionShape2D { Shape = new RectangleShape2D { Size = new Vector2(20, 20) } });
		var entity = new TestAbilityEntity();
		entity.AddChild(body);
		world.AddChild(entity);
	}

	private static void Connect(ForgeGraph graph, OutputPort from, ForgeNode to)
	{
		graph.AddConnection(new Connection(from, to.InputPorts[0]));
	}
}
