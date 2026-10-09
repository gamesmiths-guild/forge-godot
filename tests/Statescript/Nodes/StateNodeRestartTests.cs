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
	public void An_animation_restart_that_stops_the_graph_on_the_way_ends_with_it()
	{
		var entity = new TestEntity3D();
		AnimationPlayer player = AddSwingPlayer(entity);
		Root.AddChild(entity);

		try
		{
			ForgeGraph graph = GraphWithEntity(entity);
			var play = new PlayAnimationNode(animation: "swing", restartOnRetrigger: true);
			play.BindInput(PlayAnimationNode.EntityInput, "entity");
			AddWithRetrigger(graph, play);

			// Stopping the animation for the restart emits current_animation_changed, and a listener on it stops the
			// graph from under the restart. Started after Play Animation, so its first play goes unheard.
			graph.VariableDefinitions.DefineObjectVariable<Node>("player", player);
			var listener = new SignalListenerNode("current_animation_changed");
			listener.BindInput(SignalListenerNode.NodeInput, "player");
			var exit = new ExitNode();
			graph.AddNode(listener);
			graph.AddNode(exit);
			Connect(graph, graph.EntryNode.OutputPorts[EntryNode.OutputPort], listener);
			Connect(graph, listener.OutputPorts[SignalListenerNode.OnSignalPort], exit);

			var processor = new GraphProcessor(graph);
			processor.StartGraph();

			processor.Invoking(x => x.UpdateGraph(RetriggerAt)).Should().NotThrow();

			processor.GraphContext.IsActive.Should().BeFalse("the graph stopped");
			player.IsPlaying().Should().BeFalse("the restart ends with the graph instead of playing again");
		}
		finally
		{
			entity.Free();
		}
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

	[TestCase]
	[RequireGodotRuntime]
	public void A_scene_restart_whose_subgraph_stops_the_graph_ends_with_it()
	{
		var parent = new Node3D();
		Root.AddChild(parent);

		try
		{
			var graph = new ForgeGraph();
			Scene3DNode spawn = AddSpawn(graph, parent, restartOnRetrigger: true);

			// The restart disables the subgraph, which ends this timer, and its ending stops the graph from under the
			// restart.
			TimerNode subgraph = AddSubgraphTimer(graph, spawn);
			var exit = new ExitNode();
			graph.AddNode(exit);
			Connect(graph, subgraph.OutputPorts[TimerNode.OnDeactivatePort], exit);

			var processor = new GraphProcessor(graph);
			processor.StartGraph();

			processor.Invoking(x => x.UpdateGraph(RetriggerAt)).Should().NotThrow();

			processor.GraphContext.IsActive.Should().BeFalse("the graph stopped");
			parent.GetChildren().Should().ContainSingle("the restart ends with the graph instead of spawning again")
				.Which.IsQueuedForDeletion().Should().BeTrue("stopping the graph freed the instance");
		}
		finally
		{
			parent.Free();
		}
	}

	[TestCase]
	[RequireGodotRuntime]
	public void A_scene_restart_whose_subgraph_starts_the_graph_over_leaves_the_new_run_its_instance()
	{
		var parent = new Node3D();
		Root.AddChild(parent);

		try
		{
			var graph = new ForgeGraph();
			Scene3DNode spawn = AddSpawn(graph, parent, restartOnRetrigger: true);

			// The restart disables the subgraph, which ends this timer, and its ending stops the graph; the graph
			// starts over before the restart returns.
			TimerNode subgraph = AddSubgraphTimer(graph, spawn);
			var exit = new ExitNode();
			graph.AddNode(exit);
			Connect(graph, subgraph.OutputPorts[TimerNode.OnDeactivatePort], exit);

			var processor = new GraphProcessor(graph);
			int completions = 0;
			processor.OnGraphCompleted = () =>
			{
				if (++completions == 1)
				{
					processor.StartGraph();
				}
			};

			processor.StartGraph();
			processor.UpdateGraph(RetriggerAt);

			Node[] instances = [.. parent.GetChildren()];
			instances.Should().HaveCount(2, "the new run spawned its own, and the restart it replaced did not");
			instances[0].IsQueuedForDeletion().Should().BeTrue("stopping the graph freed the first instance");
			instances[1].IsQueuedForDeletion().Should().BeFalse("the new run keeps its instance");
		}
		finally
		{
			parent.Free();
		}
	}

	[TestCase]
	[RequireGodotRuntime]
	public void A_scene_restart_that_spawns_nothing_leaves_no_output_naming_the_freed_instance()
	{
		var parent = new Node3D();
		Root.AddChild(parent);

		try
		{
			var graph = new ForgeGraph();
			AddSpawn(graph, parent, restartOnRetrigger: true);

			var processor = new GraphProcessor(graph);
			processor.StartGraph();

			// The scene can no longer be resolved by the time the retrigger comes.
			processor.GraphContext.GraphVariables.SetObject("scene", null);
			processor.UpdateGraph(RetriggerAt);

			Node[] instances = [.. parent.GetChildren()];
			instances.Should().ContainSingle("nothing replaced the first instance");
			instances[0].IsQueuedForDeletion().Should().BeTrue("the restart freed it");
			processor.GraphContext.GraphVariables.TryGetObject("instance", out Node? output);
			output.Should().BeNull("the output no longer names an instance that is gone");
		}
		finally
		{
			parent.Free();
		}
	}

	[TestCase]
	[RequireGodotRuntime]
	public void A_retriggered_sound_keeps_its_settings_by_default()
	{
		PlaySoundAndRetrigger(restartOnRetrigger: false).Should().BeApproximately(
			-3.0f,
			Tolerance,
			"the sound carries on as it was started.");
	}

	[TestCase]
	[RequireGodotRuntime]
	public void A_restarted_sound_plays_again_with_its_settings_resolved_again()
	{
		PlaySoundAndRetrigger(restartOnRetrigger: true).Should().BeApproximately(
			-9.0f,
			Tolerance,
			"the restart plays the sound again with the volume the graph holds now.");
	}

	private static float PlaySoundAndRetrigger(bool restartOnRetrigger)
	{
		var entity = new TestEntity3D();

		// A second of silence, long enough to still be playing when the retrigger comes.
		var player = new AudioStreamPlayer
		{
			Stream = new AudioStreamWav
			{
				Data = new byte[88200],
				MixRate = 44100,
				Format = AudioStreamWav.FormatEnum.Format16Bits,
			},
		};

		entity.AddChild(player);
		Root.AddChild(entity);

		try
		{
			ForgeGraph graph = GraphWithEntity(entity);
			graph.VariableDefinitions.DefineVariable("volume", -3.0);

			var play = new PlayAudioNode(restartOnRetrigger: restartOnRetrigger);
			play.BindInput(PlayAudioNode.EntityInput, "entity");
			play.BindInput(PlayAudioNode.VolumeDbInput, "volume");
			AddWithRetrigger(graph, play);

			var processor = new GraphProcessor(graph);
			processor.StartGraph();
			processor.GraphContext.GraphVariables.SetVar("volume", -9.0);
			processor.UpdateGraph(RetriggerAt);

			player.Playing.Should().BeTrue("the sound is still playing either way");
			return player.VolumeDb;
		}
		finally
		{
			entity.Free();
		}
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
		AnimationPlayer player = AddSwingPlayer(entity);
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

		try
		{
			var graph = new ForgeGraph();
			Scene3DNode spawn = AddSpawn(graph, parent, restartOnRetrigger);

			// A timer stands in for whatever the subgraph does with the instance, and its elapsed time tells a subgraph
			// that carried on from one that started over.
			TimerNode subgraph = AddSubgraphTimer(graph, spawn);

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

	private static AnimationPlayer AddSwingPlayer(TestEntity3D entity)
	{
		var player = new AnimationPlayer
		{
			CallbackModeProcess = AnimationMixer.AnimationCallbackModeProcess.Manual,
		};

		var library = new AnimationLibrary();
		library.AddAnimation("swing", new Animation { Length = 1.0f });
		player.AddAnimationLibrary(string.Empty, library);
		entity.AddChild(player);

		return player;
	}

	private static Scene3DNode AddSpawn(ForgeGraph graph, Node parent, bool restartOnRetrigger)
	{
		var template = new Node3D();
		var scene = new PackedScene();
		scene.Pack(template);
		template.Free();

		graph.VariableDefinitions.DefineObjectVariable("scene", scene);
		graph.VariableDefinitions.DefineObjectVariable("parent", parent);
		graph.VariableDefinitions.DefineObjectVariable<Node>("instance");

		var spawn = new Scene3DNode(
			InstantiateParentMode.Node,
			passOwnership: false,
			restartOnRetrigger: restartOnRetrigger);
		spawn.BindInput(SceneNodeBase.SceneInput, "scene");
		spawn.BindInput(SceneNodeBase.ParentNodeInput, "parent");
		spawn.BindOutput(SceneNodeBase.InstanceOutput, "instance");
		AddWithRetrigger(graph, spawn);

		return spawn;
	}

	private static TimerNode AddSubgraphTimer(ForgeGraph graph, Scene3DNode spawn)
	{
		graph.VariableDefinitions.DefineVariable("subgraphDuration", 10.0);

		var timer = new TimerNode();
		timer.BindInput(TimerNode.DurationInput, "subgraphDuration");
		graph.AddNode(timer);
		Connect(graph, spawn.OutputPorts[SceneNodeBase.SubgraphPort], timer);

		return timer;
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
