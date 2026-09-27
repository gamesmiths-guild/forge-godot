// Copyright © Gamesmiths Guild.

using FluentAssertions;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Godot.Core.Statescript.Nodes;
using Gamesmiths.Forge.Godot.Core.Statescript.Nodes.State;
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
using NumericsVector3 = System.Numerics.Vector3;

namespace Gamesmiths.Forge.Godot.Tests.Statescript.Nodes;

/// <summary>
/// What the plugin's state nodes do with a retrigger - a message reaching them while they are already running. Each is
/// run in a real graph against a real scene tree, with a timer sending its input again half a second in.
/// </summary>
[TestSuite]
public class StateNodeRestartTests
{
	private const double RetriggerAt = 0.5;
	private const float Tolerance = 0.001f;

	private delegate void SceneCheck(Node[] instances, Node? output, double subgraphElapsed);

	private static Window Root => ((SceneTree)Engine.GetMainLoop()).Root;

	[TestCase]
	[RequireGodotRuntime]
	public void A_retriggered_move_keeps_its_course_by_default()
	{
		MoveAndRetrigger(restartOnRetrigger: false).Should().BeApproximately(
			10.0f,
			Tolerance,
			"the move keeps its one-second course and arrives on time.");
	}

	[TestCase]
	[RequireGodotRuntime]
	public void A_restarted_move_starts_over_from_where_it_is()
	{
		MoveAndRetrigger(restartOnRetrigger: true).Should().BeApproximately(
			7.5f,
			Tolerance,
			"the move starts over from halfway, so half a second later it is half of the rest of the way there.");
	}

	[TestCase]
	[RequireGodotRuntime]
	public void A_retriggered_animation_keeps_playing_by_default()
	{
		PlayAndRetrigger(restartOnRetrigger: false).Should().BeApproximately(
			0.4,
			Tolerance,
			"the animation carries on from where it was.");
	}

	[TestCase]
	[RequireGodotRuntime]
	public void A_restarted_animation_plays_again_from_its_start()
	{
		PlayAndRetrigger(restartOnRetrigger: true).Should().BeApproximately(
			0.0,
			Tolerance,
			"Godot's Play leaves an animation that is already playing where it is, so the restart has to rewind it.");
	}

	[TestCase]
	[RequireGodotRuntime]
	public void A_retriggered_scene_keeps_its_instance_by_default()
	{
		SpawnAndRetrigger(restartOnRetrigger: false, (instances, output, subgraphElapsed) =>
		{
			instances.Should().ContainSingle("nothing is freed or spawned");
			instances[0].IsQueuedForDeletion().Should().BeFalse("the instance lives on");
			output.Should().BeSameAs(instances[0], "the instance output still names it");
			subgraphElapsed.Should().BeApproximately(0.5, Tolerance, "the subgraph carries on");
		});
	}

	[TestCase]
	[RequireGodotRuntime]
	public void A_restarted_scene_replaces_its_instance_and_starts_its_subgraph_over()
	{
		SpawnAndRetrigger(restartOnRetrigger: true, (instances, output, subgraphElapsed) =>
		{
			instances.Should().HaveCount(2, "a second instance is spawned");
			instances[0].IsQueuedForDeletion().Should().BeTrue("the first instance is freed");
			instances[1].IsQueuedForDeletion().Should().BeFalse("the second is the one kept");
			output.Should().BeSameAs(instances[1], "the instance output names the new one");

			// The subgraph works on the instance, so it has to start over with the new one rather than carry on.
			subgraphElapsed.Should().BeLessThan(0.2, "the subgraph starts over");
		});
	}

	private static float MoveAndRetrigger(bool restartOnRetrigger)
	{
		var entity = new TestEntity3D();
		Root.AddChild(entity);

		try
		{
			ForgeGraph graph = GraphWithEntity(entity);
			graph.VariableDefinitions.DefineVariable("destination", new NumericsVector3(10, 0, 0));
			graph.VariableDefinitions.DefineVariable("duration", 1.0);

			var move = new MoveTo3DNode(restartOnRetrigger: restartOnRetrigger);
			move.BindInput(MoveTo3DNode.EntityInput, "entity");
			move.BindInput(MoveTo3DNode.DestinationInput, "destination");
			move.BindInput(MoveTo3DNode.ValueInput, "duration");
			AddWithRetrigger(graph, move);

			var processor = new GraphProcessor(graph);
			processor.StartGraph();

			processor.FixedUpdateGraph(0.5);
			processor.UpdateGraph(0.5);
			processor.FixedUpdateGraph(0.5);

			return entity.GlobalPosition.X;
		}
		finally
		{
			entity.Free();
		}
	}

	private static double PlayAndRetrigger(bool restartOnRetrigger)
	{
		var entity = new TestEntity3D();
		var player = new AnimationPlayer
		{
			CallbackModeProcess = AnimationMixer.AnimationCallbackModeProcess.Manual,
		};

		var library = new AnimationLibrary();
		library.AddAnimation("swing", new Animation { Length = 1.0f });
		player.AddAnimationLibrary(string.Empty, library);
		entity.AddChild(player);
		Root.AddChild(entity);

		try
		{
			ForgeGraph graph = GraphWithEntity(entity);

			var play = new PlayAnimationNode(animation: "swing", restartOnRetrigger: restartOnRetrigger);
			play.BindInput(PlayAnimationNode.EntityInput, "entity");
			AddWithRetrigger(graph, play);

			var processor = new GraphProcessor(graph);
			processor.StartGraph();

			player.Advance(0.4);
			processor.UpdateGraph(0.5);

			player.CurrentAnimation.ToString().Should().Be("swing", "the animation is still playing either way");
			return player.CurrentAnimationPosition;
		}
		finally
		{
			entity.Free();
		}
	}

	private static void SpawnAndRetrigger(bool restartOnRetrigger, SceneCheck check)
	{
		var parent = new Node3D();
		Root.AddChild(parent);

		var template = new Node3D();
		var scene = new PackedScene();
		scene.Pack(template);
		template.Free();

		try
		{
			var graph = new ForgeGraph();
			graph.VariableDefinitions.DefineObjectVariable("scene", scene);
			graph.VariableDefinitions.DefineObjectVariable<Node>("parent", parent);
			graph.VariableDefinitions.DefineObjectVariable<Node>("instance");
			graph.VariableDefinitions.DefineVariable("subgraphDuration", 10.0);

			var spawn = new Scene3DNode(
				InstantiateParentMode.Node,
				passOwnership: false,
				restartOnRetrigger: restartOnRetrigger);
			spawn.BindInput(SceneNodeBase.SceneInput, "scene");
			spawn.BindInput(SceneNodeBase.ParentNodeInput, "parent");
			spawn.BindOutput(SceneNodeBase.InstanceOutput, "instance");
			AddWithRetrigger(graph, spawn);

			// A timer stands in for whatever the subgraph does with the instance, and its elapsed time tells a subgraph
			// that carried on from one that started over.
			var subgraph = new TimerNode();
			subgraph.BindInput(TimerNode.DurationInput, "subgraphDuration");
			graph.AddNode(subgraph);
			Connect(graph, spawn.OutputPorts[SceneNodeBase.SubgraphPort], subgraph);

			var processor = new GraphProcessor(graph);
			processor.StartGraph();

			// Split so the retrigger lands in a pass of its own: however the pass orders the two timers, a subgraph
			// that started over has run for at most that last tenth of a second.
			processor.UpdateGraph(0.4);
			processor.UpdateGraph(0.1);

			processor.GraphContext.GraphVariables.TryGetObject("instance", out Node? output);

			check(
				[.. parent.GetChildren()],
				output,
				processor.GraphContext.GetNodeContext<TimerNodeContext>(subgraph.NodeID).ElapsedTime);
		}
		finally
		{
			parent.Free();
		}
	}

	private static ForgeGraph GraphWithEntity(TestEntity3D entity)
	{
		var graph = new ForgeGraph();
		graph.VariableDefinitions.DefineObjectVariable<IForgeEntity>("entity", entity);
		return graph;
	}

	// The node starts from the entry, and a timer started alongside it sends its input again once it is under way.
	private static void AddWithRetrigger(ForgeGraph graph, ForgeNode node)
	{
		graph.VariableDefinitions.DefineVariable("retriggerAt", RetriggerAt);

		var timer = new TimerNode();
		timer.BindInput(TimerNode.DurationInput, "retriggerAt");

		graph.AddNode(node);
		graph.AddNode(timer);
		Connect(graph, graph.EntryNode.OutputPorts[EntryNode.OutputPort], timer);
		Connect(graph, graph.EntryNode.OutputPorts[EntryNode.OutputPort], node);
		Connect(graph, timer.OutputPorts[TimerNode.OnTimerEndPort], node);
	}

	private static void Connect(ForgeGraph graph, OutputPort from, ForgeNode to)
	{
		graph.AddConnection(new Connection(from, to.InputPorts[TimerNode.InputPort]));
	}
}
