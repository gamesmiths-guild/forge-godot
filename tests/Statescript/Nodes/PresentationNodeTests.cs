// Copyright © Gamesmiths Guild.

using FluentAssertions;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Godot.Core;
using Gamesmiths.Forge.Godot.Core.Statescript.Interop;
using Gamesmiths.Forge.Godot.Core.Statescript.Nodes;
using Gamesmiths.Forge.Godot.Core.Statescript.Nodes.Action;
using Gamesmiths.Forge.Godot.Core.Statescript.Nodes.State;
using Gamesmiths.Forge.Godot.Tests.Helpers;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Nodes;
using GdUnit4;
using Godot;
using ForgeGraph = Gamesmiths.Forge.Statescript.Graph;
using ForgeNode = Gamesmiths.Forge.Statescript.Node;
using Node = Godot.Node;

namespace Gamesmiths.Forge.Godot.Tests.Statescript.Nodes;

/// <summary>
/// The plugin's presentation nodes: what they show starts and ends in the presentation pass, and how long they last is
/// counted in fixed steps rather than read from engine playback.
/// </summary>
[TestSuite]
public class PresentationNodeTests
{
	private static Window Root => ((SceneTree)Engine.GetMainLoop()).Root;

	[TestCase]
	[RequireGodotRuntime]
	public void Nodes_that_only_show_something_are_presentation_nodes()
	{
		ForgeNode[] presentation =
		[
			new PlayAnimationNode(),
			new PlayAudioNode(),
			new PlayAnimationOneShotNode(),
			new PlayAudioOneShotNode(),
			new NodePropertyOverrideNode(),
			new SetNodePropertyNode(),
			new NodeEnabledOverrideNode(NodeEnabledAspect.Visible),
			new SetNodeEnabledNode(NodeEnabledAspect.Visible),
		];

		ForgeNode[] simulation =
		[
			new NodeEnabledOverrideNode(NodeEnabledAspect.Monitoring),
			new SetNodeEnabledNode(NodeEnabledAspect.Processing),
			new CallMethodNode(),
			new EmitSignalNode(),
			new Scene3DNode(),
			new InstantiateScene3DNode(),
			new MoveBody3DNode(),
		];

		foreach (ForgeNode node in presentation)
		{
			node.NetworkKind.Should().Be(NodeNetworkKind.Presentation, node.GetType().Name);
		}

		foreach (ForgeNode node in simulation)
		{
			node.NetworkKind.Should().Be(NodeNetworkKind.Simulation, node.GetType().Name);
		}
	}

	[TestCase]
	[RequireGodotRuntime]
	public void An_animation_starts_in_the_presentation_pass_not_when_the_node_activates()
	{
		var entity = new TestEntity3D();
		AnimationPlayer player = AddPlayer(entity, length: 1.0f);
		Root.AddChild(entity);

		try
		{
			GraphProcessor processor = Start(entity, PlayNode());

			player.IsPlaying().Should().BeFalse("nothing is shown before the pass");

			processor.Present();

			player.CurrentAnimation.ToString().Should().Be("swing");
		}
		finally
		{
			entity.Free();
		}
	}

	[TestCase]
	[RequireGodotRuntime]
	public void An_animation_that_starts_and_ends_within_a_frame_never_plays()
	{
		var entity = new TestEntity3D();
		AnimationPlayer player = AddPlayer(entity, length: 1.0f);
		Root.AddChild(entity);

		try
		{
			GraphProcessor processor = Start(entity, PlayNode());

			processor.StopGraph();
			processor.Present();

			player.IsPlaying().Should().BeFalse();
			player.AssignedAnimation.ToString().Should().BeEmpty("the animation was never started");
		}
		finally
		{
			entity.Free();
		}
	}

	[TestCase]
	[RequireGodotRuntime]
	public void An_animation_lasts_its_length_at_its_speed_counted_in_fixed_steps()
	{
		var entity = new TestEntity3D();
		AddPlayer(entity, length: 1.0f);
		Root.AddChild(entity);

		try
		{
			PlayAnimationNode play = PlayNode();
			GraphProcessor processor = Start(entity, play, speed: 2.0);

			processor.UpdateGraph(10.0);
			processor.GraphContext.IsActive.Should().BeTrue("frames do not count toward the length");

			processor.FixedUpdateGraph(0.25);
			processor.GraphContext.IsActive.Should().BeTrue("a one second clip at double speed lasts half a second");

			processor.FixedUpdateGraph(0.25);
			processor.GraphContext.IsActive.Should().BeFalse();
		}
		finally
		{
			entity.Free();
		}
	}

	[TestCase]
	[RequireGodotRuntime]
	public void A_looping_animation_never_finishes_on_its_own()
	{
		var entity = new TestEntity3D();
		AddPlayer(entity, length: 1.0f, Animation.LoopModeEnum.Linear);
		Root.AddChild(entity);

		try
		{
			GraphProcessor processor = Start(entity, PlayNode());

			processor.FixedUpdateGraph(100.0);

			processor.GraphContext.IsActive.Should().BeTrue();
		}
		finally
		{
			entity.Free();
		}
	}

	[TestCase]
	[RequireGodotRuntime]
	public void An_animation_ended_early_is_stopped_by_the_next_pass()
	{
		var entity = new TestEntity3D();
		AnimationPlayer player = AddPlayer(entity, length: 1.0f);
		Root.AddChild(entity);

		try
		{
			GraphProcessor processor = Start(entity, PlayNode());
			processor.Present();

			processor.StopGraph();
			player.IsPlaying().Should().BeTrue("the pass has not run since the node ended");

			processor.Present();

			player.IsPlaying().Should().BeFalse();
		}
		finally
		{
			entity.Free();
		}
	}

	[TestCase]
	[RequireGodotRuntime]
	public void An_animation_plays_at_the_speed_resolved_when_it_activated()
	{
		var entity = new TestEntity3D();
		AnimationPlayer player = AddPlayer(entity, length: 1.0f);
		Root.AddChild(entity);

		try
		{
			GraphProcessor processor = Start(entity, PlayNode(), speed: 2.0);

			processor.GraphContext.GraphVariables.SetVar("speed", 1.0);
			processor.Present();

			player.GetPlayingSpeed().Should().Be(2.0f, "the length counted from activation is at speed 2");
		}
		finally
		{
			entity.Free();
		}
	}

	[TestCase]
	[RequireGodotRuntime]
	public void An_animation_whose_restart_stop_ends_the_node_is_not_played()
	{
		var entity = new TestEntity3D();
		AnimationPlayer player = AddPlayer(entity, length: 1.0f);
		Root.AddChild(entity);

		try
		{
			// Already playing, so the pass stops it before playing it again, and a listener on that stop ends the graph.
			player.Play("swing");

			var graph = new ForgeGraph();
			graph.VariableDefinitions.DefineObjectVariable<IForgeEntity>("entity", entity);
			graph.VariableDefinitions.DefineObjectVariable<Node>("player", player);
			graph.VariableDefinitions.DefineVariable("speed", 1.0);

			var listener = new SignalListenerNode("current_animation_changed");
			listener.BindInput(SignalListenerNode.NodeInput, "player");
			var exit = new ExitNode();
			graph.AddNode(listener);
			graph.AddNode(exit);
			graph.AddConnection(new Connection(
				graph.EntryNode.OutputPorts[EntryNode.OutputPort],
				listener.InputPorts[StateNode<StateNodeContext>.InputPort]));
			graph.AddConnection(new Connection(
				listener.OutputPorts[SignalListenerNode.OnSignalPort],
				exit.InputPorts[ExitNode.InputPort]));
			GraphProcessor processor = Start(graph, PlayNode());

			processor.Present();

			processor.GraphContext.IsActive.Should().BeFalse("stopping the copy already playing ended the graph");
			player.IsPlaying().Should().BeFalse("what the ended node would play is not started");
		}
		finally
		{
			entity.Free();
		}
	}

	[TestCase]
	[RequireGodotRuntime]
	public void An_animation_that_finishes_naturally_is_left_to_play_out()
	{
		var entity = new TestEntity3D();
		AnimationPlayer player = AddPlayer(entity, length: 1.0f);
		Root.AddChild(entity);

		try
		{
			GraphProcessor processor = Start(entity, PlayNode());
			processor.Present();

			processor.FixedUpdateGraph(1.0);
			processor.GraphContext.IsActive.Should().BeFalse();
			processor.Present();

			player.IsPlaying().Should().BeTrue("playback trails the steps, and stopping it would snap it to its start");
		}
		finally
		{
			entity.Free();
		}
	}

	[TestCase]
	[RequireGodotRuntime]
	public void A_missing_animation_finishes_on_the_next_step()
	{
		var entity = new TestEntity3D();
		AddPlayer(entity, length: 1.0f);
		Root.AddChild(entity);

		try
		{
			GraphProcessor processor = Start(entity, new PlayAnimationNode(animation: "missing"));

			processor.FixedUpdateGraph(1.0 / 60.0);

			processor.GraphContext.IsActive.Should().BeFalse("a misconfigured node must not stall its graph");
		}
		finally
		{
			entity.Free();
		}
	}

	[TestCase]
	[RequireGodotRuntime]
	public void A_sound_lasts_its_stream_length_at_its_pitch_counted_in_fixed_steps()
	{
		var entity = new TestEntity3D();
		AudioStreamPlayer player = AddSound(entity, seconds: 0.5);
		Root.AddChild(entity);

		try
		{
			var play = new PlayAudioNode();
			play.BindInput(PlayAudioNode.EntityInput, "entity");
			play.BindInput(PlayAudioNode.PitchInput, "pitch");
			GraphProcessor processor = Start(entity, play, pitch: 2.0);

			processor.Present();
			player.Playing.Should().BeTrue();

			processor.FixedUpdateGraph(0.125);
			processor.GraphContext.IsActive.Should().BeTrue("half a second at double pitch lasts a quarter of one");

			processor.FixedUpdateGraph(0.125);
			processor.GraphContext.IsActive.Should().BeFalse();
		}
		finally
		{
			entity.Free();
		}
	}

	[TestCase]
	[RequireGodotRuntime]
	public void A_looping_sound_never_finishes_on_its_own()
	{
		var entity = new TestEntity3D();
		AudioStreamPlayer player = AddSound(entity, seconds: 0.5);
		((AudioStreamWav)player.Stream).LoopMode = AudioStreamWav.LoopModeEnum.Forward;
		Root.AddChild(entity);

		try
		{
			var play = new PlayAudioNode();
			play.BindInput(PlayAudioNode.EntityInput, "entity");
			GraphProcessor processor = Start(entity, play);

			processor.FixedUpdateGraph(100.0);

			processor.GraphContext.IsActive.Should().BeTrue();
		}
		finally
		{
			entity.Free();
		}
	}

	[TestCase]
	[RequireGodotRuntime]
	public void A_looping_sound_restores_into_its_loop_rather_than_from_the_stream_s_start()
	{
		var entity = new TestEntity3D();
		AudioStreamPlayer player = AddSound(entity, seconds: 1);
		var stream = (AudioStreamWav)player.Stream;
		stream.LoopMode = AudioStreamWav.LoopModeEnum.Forward;
		stream.LoopBegin = 44100 / 5;
		stream.LoopEnd = 44100;

		try
		{
			AudioPlayers.StreamPosition(player, 1, 0.5).Should().BeApproximately(0.5, 1e-6);
			AudioPlayers.StreamPosition(player, 1, 1.5).Should().BeApproximately(0.7, 1e-6, "the loop starts at 0.2 s");

			stream.LoopMode = AudioStreamWav.LoopModeEnum.Pingpong;

			AudioPlayers.StreamPosition(player, 1, 1.3)
				.Should().BeApproximately(0.7, 1e-6, "it plays back from the end");
		}
		finally
		{
			entity.Free();
		}
	}

	[TestCase]
	[RequireGodotRuntime]
	public void A_property_override_writes_in_one_pass_and_restores_in_the_pass_after_it_ends()
	{
		var light = new OmniLight3D { LightEnergy = 1.0f };
		Root.AddChild(light);

		try
		{
			var graph = new ForgeGraph();
			graph.VariableDefinitions.DefineObjectVariable<Node>("light", light);
			graph.VariableDefinitions.DefineVariable("energy", 5.0);

			var hold = new NodePropertyOverrideNode("light_energy", InteropValueType.Float);
			hold.BindInput(NodePropertyOverrideNode.NodeInput, "light");
			hold.BindInput(NodePropertyOverrideNode.ValueInput, "energy");
			GraphProcessor processor = Start(graph, hold);

			light.LightEnergy.Should().Be(1.0f, "nothing is written before the pass");

			processor.Present();
			light.LightEnergy.Should().Be(5.0f);

			processor.StopGraph();
			light.LightEnergy.Should().Be(5.0f, "the pass has not run since the node ended");

			processor.Present();
			light.LightEnergy.Should().Be(1.0f);
		}
		finally
		{
			light.Free();
		}
	}

	[TestCase]
	[RequireGodotRuntime]
	public void A_visibility_override_waits_for_the_pass_while_a_monitoring_override_switches_at_once()
	{
		var hidden = new Node3D();
		var area = new Area3D { Monitoring = true };
		Root.AddChild(hidden);
		Root.AddChild(area);

		try
		{
			var graph = new ForgeGraph();
			graph.VariableDefinitions.DefineObjectVariable<Node>("hidden", hidden);
			graph.VariableDefinitions.DefineObjectVariable<Node>("area", area);
			graph.VariableDefinitions.DefineVariable("enabled", false);

			var hide = new NodeEnabledOverrideNode(NodeEnabledAspect.Visible);
			hide.BindInput(NodeEnabledOverrideNode.NodeInput, "hidden");
			hide.BindInput(NodeEnabledOverrideNode.EnabledInput, "enabled");

			var disarm = new NodeEnabledOverrideNode(NodeEnabledAspect.Monitoring);
			disarm.BindInput(NodeEnabledOverrideNode.NodeInput, "area");
			disarm.BindInput(NodeEnabledOverrideNode.EnabledInput, "enabled");

			GraphProcessor processor = Start(graph, hide, disarm);

			hidden.Visible.Should().BeTrue("visibility only changes in the pass");
			area.Monitoring.Should().BeFalse("monitoring changes gameplay, so it switches at once");

			processor.Present();
			hidden.Visible.Should().BeFalse();

			processor.StopGraph();
			area.Monitoring.Should().BeTrue();
			hidden.Visible.Should().BeFalse();

			processor.Present();
			hidden.Visible.Should().BeTrue();
		}
		finally
		{
			hidden.Free();
			area.Free();
		}
	}

	[TestCase]
	[RequireGodotRuntime]
	public void A_sound_without_a_pitch_plays_at_the_player_s_own_pitch_after_an_override()
	{
		var entity = new TestEntity3D();
		AudioStreamPlayer player = AddSound(entity, seconds: 0.5);
		Root.AddChild(entity);

		try
		{
			var raised = new PlayAudioNode();
			raised.BindInput(PlayAudioNode.EntityInput, "entity");
			raised.BindInput(PlayAudioNode.PitchInput, "pitch");
			Start(entity, raised, pitch: 2.0).Present();
			player.PitchScale.Should().Be(2.0f);

			var plain = new PlayAudioNode();
			plain.BindInput(PlayAudioNode.EntityInput, "entity");
			GraphProcessor processor = Start(entity, plain);
			processor.Present();

			player.PitchScale.Should().Be(1.0f, "an unbound pitch is the player's own, not the last override");

			processor.FixedUpdateGraph(0.25);
			processor.GraphContext.IsActive.Should().BeTrue(
				"half a second at the player's own pitch lasts half a second, presented or not");
		}
		finally
		{
			entity.Free();
		}
	}

	[TestCase]
	[RequireGodotRuntime]
	public void An_override_holds_the_value_resolved_when_it_activated()
	{
		var hidden = new Node3D();
		var light = new OmniLight3D { LightEnergy = 1.0f };
		Root.AddChild(hidden);
		Root.AddChild(light);

		try
		{
			var graph = new ForgeGraph();
			graph.VariableDefinitions.DefineObjectVariable<Node>("hidden", hidden);
			graph.VariableDefinitions.DefineObjectVariable<Node>("light", light);
			graph.VariableDefinitions.DefineVariable("enabled", false);
			graph.VariableDefinitions.DefineVariable("energy", 5.0);

			var hide = new NodeEnabledOverrideNode(NodeEnabledAspect.Visible);
			hide.BindInput(NodeEnabledOverrideNode.NodeInput, "hidden");
			hide.BindInput(NodeEnabledOverrideNode.EnabledInput, "enabled");

			var glow = new NodePropertyOverrideNode("light_energy", InteropValueType.Float);
			glow.BindInput(NodePropertyOverrideNode.NodeInput, "light");
			glow.BindInput(NodePropertyOverrideNode.ValueInput, "energy");

			GraphProcessor processor = Start(graph, hide, glow);
			processor.GraphContext.GraphVariables.SetVar("enabled", true);
			processor.GraphContext.GraphVariables.SetVar("energy", 2.0);

			processor.Present();

			hidden.Visible.Should().BeFalse();
			light.LightEnergy.Should().Be(5.0f);
		}
		finally
		{
			hidden.Free();
			light.Free();
		}
	}

	[TestCase]
	[RequireGodotRuntime]
	public void A_scene_lifetime_counts_fixed_steps()
	{
		var parent = new Node3D();
		Root.AddChild(parent);

		try
		{
			var template = new Node3D();
			var scene = new PackedScene();
			scene.Pack(template);
			template.Free();

			var graph = new ForgeGraph();
			graph.VariableDefinitions.DefineObjectVariable("scene", scene);
			graph.VariableDefinitions.DefineObjectVariable<Node>("parent", parent);
			graph.VariableDefinitions.DefineVariable("lifetime", 0.5);

			var spawn = new Scene3DNode(InstantiateParentMode.Node, passOwnership: false);
			spawn.BindInput(SceneNodeBase.SceneInput, "scene");
			spawn.BindInput(SceneNodeBase.ParentNodeInput, "parent");
			spawn.BindInput(SceneNodeBase.LifetimeInput, "lifetime");
			GraphProcessor processor = Start(graph, spawn);

			processor.UpdateGraph(10.0);
			processor.GraphContext.IsActive.Should().BeTrue("frames do not count toward the lifetime");

			processor.FixedUpdateGraph(0.5);

			processor.GraphContext.IsActive.Should().BeFalse();
			parent.GetChild(0).IsQueuedForDeletion().Should().BeTrue();
		}
		finally
		{
			parent.Free();
		}
	}

	[TestCase]
	[RequireGodotRuntime]
	public void An_animation_presented_part_way_through_shows_the_pose_it_is_at()
	{
		var entity = new TestEntity3D();
		AnimationPlayer player = AddPlayer(entity, length: 1.0f);
		Root.AddChild(entity);

		try
		{
			GraphProcessor processor = Start(entity, PlayNode(), speed: 2.0);
			processor.FixedUpdateGraph(0.125);
			processor.Present();

			player.CurrentAnimationPosition.Should().BeApproximately(0.25, 1e-6, "an eighth of a second at double speed");
		}
		finally
		{
			entity.Free();
		}
	}

	[TestCase]
	[RequireGodotRuntime]
	public void An_animation_a_restore_kept_running_seeks_to_where_the_activation_is()
	{
		var entity = new TestEntity3D();
		AnimationPlayer player = AddPlayer(entity, length: 1.0f);
		Root.AddChild(entity);

		try
		{
			GraphProcessor processor = Start(entity, PlayNode());
			processor.Present();
			processor.FixedUpdateGraph(0.25);
			GraphSnapshot quarter = processor.CaptureSnapshot();
			processor.FixedUpdateGraph(0.5);

			processor.RestoreSnapshot(quarter);
			processor.Present();

			player.CurrentAnimationPosition.Should().BeApproximately(0.25, 1e-6);
			player.CurrentAnimation.ToString().Should().Be("swing", "it's the same activation, not played again");
		}
		finally
		{
			entity.Free();
		}
	}

	[TestCase]
	[RequireGodotRuntime]
	public void A_sound_started_on_a_player_that_cannot_process_waits_paused()
	{
		var entity = new TestEntity3D();
		AudioStreamPlayer player = AddSound(entity, seconds: 1.0);
		player.ProcessMode = Node.ProcessModeEnum.Disabled;
		Root.AddChild(entity);

		try
		{
			var play = new PlayAudioNode();
			play.BindInput(PlayAudioNode.EntityInput, "entity");
			GraphProcessor processor = Start(entity, play);
			processor.Present();

			player.StreamPaused.Should().BeTrue("it waits for the game to go on");
		}
		finally
		{
			entity.Free();
		}
	}

	[TestCase]
	[RequireGodotRuntime]
	public void A_sound_a_restore_kept_running_catches_up_once_its_player_can_play()
	{
		var entity = new TestEntity3D();
		AudioStreamPlayer player = AddSound(entity, seconds: 1.0);
		Root.AddChild(entity);

		try
		{
			var play = new PlayAudioNode();
			play.BindInput(PlayAudioNode.EntityInput, "entity");
			GraphProcessor processor = Start(entity, play);
			processor.Present();
			processor.FixedUpdateGraph(0.5);
			GraphSnapshot half = processor.CaptureSnapshot();
			processor.FixedUpdateGraph(0.25);

			player.ProcessMode = Node.ProcessModeEnum.Disabled;
			processor.RestoreSnapshot(half);
			processor.Present();

			player.GetPlaybackPosition().Should().BeLessThan(0.1f, "a sound is left where it is while its player can't play");

			player.ProcessMode = Node.ProcessModeEnum.Inherit;
			processor.Present();

			player.GetPlaybackPosition().Should().BeApproximately(0.5f, 0.05f, "it caught up once it could play");
		}
		finally
		{
			entity.Free();
		}
	}

	private static PlayAnimationNode PlayNode()
	{
		var play = new PlayAnimationNode(animation: "swing");
		play.BindInput(PlayAnimationNode.EntityInput, "entity");
		play.BindInput(PlayAnimationNode.SpeedInput, "speed");
		return play;
	}

	private static GraphProcessor Start(TestEntity3D entity, ForgeNode node, double speed = 1.0, double pitch = 1.0)
	{
		var graph = new ForgeGraph();
		graph.VariableDefinitions.DefineObjectVariable<IForgeEntity>("entity", entity);
		graph.VariableDefinitions.DefineVariable("speed", speed);
		graph.VariableDefinitions.DefineVariable("pitch", pitch);

		return Start(graph, node);
	}

	private static GraphProcessor Start(ForgeGraph graph, params ForgeNode[] nodes)
	{
		foreach (ForgeNode node in nodes)
		{
			graph.AddNode(node);
			graph.AddConnection(new Connection(
				graph.EntryNode.OutputPorts[EntryNode.OutputPort],
				node.InputPorts[StateNode<StateNodeContext>.InputPort]));
		}

		var processor = new GraphProcessor(graph);
		processor.StartGraph();
		return processor;
	}

	private static AnimationPlayer AddPlayer(
		TestEntity3D entity,
		float length,
		Animation.LoopModeEnum loopMode = Animation.LoopModeEnum.None)
	{
		var player = new AnimationPlayer
		{
			CallbackModeProcess = AnimationMixer.AnimationCallbackModeProcess.Manual,
		};

		var library = new AnimationLibrary();
		library.AddAnimation("swing", new Animation { Length = length, LoopMode = loopMode });
		player.AddAnimationLibrary(string.Empty, library);
		entity.AddChild(player);

		return player;
	}

	private static AudioStreamPlayer AddSound(TestEntity3D entity, double seconds)
	{
		var player = new AudioStreamPlayer
		{
			Stream = new AudioStreamWav
			{
				Data = new byte[(int)(seconds * 44100) * 2],
				MixRate = 44100,
				Format = AudioStreamWav.FormatEnum.Format16Bits,
			},
		};

		entity.AddChild(player);
		return player;
	}
}
