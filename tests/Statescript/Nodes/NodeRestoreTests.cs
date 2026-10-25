// Copyright © Gamesmiths Guild.

using FluentAssertions;
using Gamesmiths.Forge.Abilities;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Godot.Core.Statescript.Interop;
using Gamesmiths.Forge.Godot.Core.Statescript.Nodes.State;
using Gamesmiths.Forge.Godot.Tests.Helpers;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Nodes;
using Gamesmiths.Forge.Statescript.Properties;
using GdUnit4;
using Godot;
using ForgeGraph = Gamesmiths.Forge.Statescript.Graph;
using ForgeNode = Gamesmiths.Forge.Statescript.Node;
using Node = Godot.Node;
using NumericsVector3 = System.Numerics.Vector3;

namespace Gamesmiths.Forge.Godot.Tests.Statescript.Nodes;

/// <summary>
/// The plugin's nodes that hold something in the engine while they run - a signal connection, an override, a group, a
/// debug marker - let go of it when a graph snapshot replaces their activation, and take hold of it again when one
/// brings it back, so the engine always matches the restored run.
/// </summary>
[TestSuite]
public class NodeRestoreTests
{
	private static SceneTree Tree => (SceneTree)Engine.GetMainLoop();

	[TestCase]
	[RequireGodotRuntime]
	public void A_signal_listener_is_connected_exactly_while_the_restored_run_listens()
	{
		var emitter = new Node();
		emitter.AddUserSignal("pinged");
		Tree.Root.AddChild(emitter);

		try
		{
			// One shot, so the listener ends on the signal it hears and lets go of its connection.
			var graph = new ForgeGraph();
			graph.VariableDefinitions.DefineObjectVariable("target", emitter);
			var listener = new SignalListenerNode("pinged", oneShot: true);
			listener.BindInput(SignalListenerNode.NodeInput, "target");
			AddFromEntry(graph, listener);

			GraphProcessor processor = StartWithSnapshot(graph, out GraphSnapshot idle);
			GraphSnapshot listening = processor.CaptureSnapshot();
			listener.InputPorts[StateNode<SignalListenerNodeContext>.AbortPort].ReceiveMessage(processor.GraphContext);

			processor.RestoreSnapshot(listening);
			processor.RestoreSnapshot(listening);
			emitter.GetSignalConnectionList("pinged").Should().ContainSingle("however often it was restored");

			processor.RestoreSnapshot(idle);
			emitter.GetSignalConnectionList("pinged").Should().BeEmpty();

			processor.RestoreSnapshot(listening);
			emitter.EmitSignal("pinged");

			processor.GraphContext.IsActive.Should().BeFalse("the signal reached the restored listener");
			emitter.GetSignalConnectionList("pinged").Should().BeEmpty();
		}
		finally
		{
			emitter.Free();
		}
	}

	[TestCase]
	[RequireGodotRuntime]
	public void A_collision_override_holds_its_bits_exactly_while_the_restored_run_does()
	{
		var entity = new TestEntity3D();
		var body = new StaticBody3D { Name = "Body", CollisionLayer = 0b11 };
		entity.AddChild(body);
		Tree.Root.AddChild(entity);

		try
		{
			var graph = new ForgeGraph();
			graph.VariableDefinitions.DefineObjectVariable<IForgeEntity>("entity", entity);
			graph.VariableDefinitions.DefineVariable("bits", 0b10);
			var node = new CollisionOverride3DNode(nodePath: "Body");
			node.BindInput(CollisionOverride3DNode.EntityInput, "entity");
			node.BindInput(CollisionOverride3DNode.BitsInput, "bits");
			AddFromEntry(graph, node);

			GraphProcessor processor = StartWithSnapshot(graph, out GraphSnapshot idle);
			body.CollisionLayer.Should().Be(0b01u);
			GraphSnapshot overriding = processor.CaptureSnapshot();

			processor.RestoreSnapshot(idle);
			body.CollisionLayer.Should().Be(0b11u);

			processor.RestoreSnapshot(overriding);
			processor.RestoreSnapshot(overriding);
			body.CollisionLayer.Should().Be(0b01u);

			processor.StopGraph();
			body.CollisionLayer.Should().Be(0b11u);
		}
		finally
		{
			entity.Free();
		}
	}

	[TestCase]
	[RequireGodotRuntime]
	public void A_node_is_in_the_group_exactly_while_the_restored_run_holds_it_there()
	{
		var target = new Node();
		Tree.Root.AddChild(target);

		try
		{
			var graph = new ForgeGraph();
			graph.VariableDefinitions.DefineObjectVariable("target", target);
			var node = new NodeGroupNode("marked");
			node.BindInput(NodeGroupNode.NodeInput, "target");
			AddFromEntry(graph, node);

			GraphProcessor processor = StartWithSnapshot(graph, out GraphSnapshot idle);
			GraphSnapshot holding = processor.CaptureSnapshot();

			processor.RestoreSnapshot(idle);
			target.IsInGroup("marked").Should().BeFalse();

			processor.RestoreSnapshot(holding);
			target.IsInGroup("marked").Should().BeTrue();

			processor.StopGraph();
			target.IsInGroup("marked").Should().BeFalse();
		}
		finally
		{
			target.Free();
		}
	}

	[TestCase]
	[RequireGodotRuntime]
	public void A_simulation_switch_is_held_exactly_while_the_restored_run_holds_it()
	{
		var target = new Node();
		Tree.Root.AddChild(target);
		target.SetProcess(true);

		try
		{
			var graph = new ForgeGraph();
			graph.VariableDefinitions.DefineObjectVariable("target", target);
			graph.VariableDefinitions.DefineVariable("enabled", false);
			var node = new NodeEnabledOverrideNode(NodeEnabledAspect.Processing);
			node.BindInput(NodeEnabledOverrideNode.NodeInput, "target");
			node.BindInput(NodeEnabledOverrideNode.EnabledInput, "enabled");
			AddFromEntry(graph, node);

			GraphProcessor processor = StartWithSnapshot(graph, out GraphSnapshot idle);
			target.IsProcessing().Should().BeFalse();
			GraphSnapshot holding = processor.CaptureSnapshot();

			processor.RestoreSnapshot(idle);
			target.IsProcessing().Should().BeTrue();

			processor.RestoreSnapshot(holding);
			target.IsProcessing().Should().BeFalse();

			processor.StopGraph();
			target.IsProcessing().Should().BeTrue();
		}
		finally
		{
			target.Free();
		}
	}

	[TestCase]
	[RequireGodotRuntime]
	public void A_force_override_and_its_debug_arrow_follow_the_restored_run()
	{
		var world = new Node3D();
		var entity = new TestAbilityEntity();
		var body = new RigidBody3D { Name = "Body", ConstantForce = new Vector3(0, 1, 0) };
		world.AddChild(entity);
		world.AddChild(body);
		Tree.Root.AddChild(world);
		bool drewCollisions = Tree.DebugCollisionsHint;
		Tree.DebugCollisionsHint = true;
		GraphAbilityBehavior? behavior = null;

		try
		{
			var graph = new ForgeGraph();

			// Shared, so a restore leaves it as it is now rather than as it was.
			entity.SharedVariables.DefineVariable("force", new NumericsVector3(5, 0, 0));
			graph.VariableDefinitions.DefineProperty(
				"force",
				new VariableResolver("force", typeof(NumericsVector3), VariableScope.Shared));
			var push = new ForceOverride3DNode("Body");
			push.BindInput(ForceOverride3DNode.ForceInput, "force");
			AddFromEntry(graph, push);

			behavior = new GraphAbilityBehavior(graph);
			AbilityHandle ability = entity.Abilities.GrantAbilityPermanently(
				new AbilityData("Push", behaviorFactory: () => behavior!),
				1,
				LevelComparison.None,
				sourceEntity: null);

			GraphSnapshot idle = behavior.Processor.CaptureSnapshot();
			ability.TryActivate(out _).Should().BeTrue();
			MeshInstance3D? arrow = Context(behavior.Processor, push).DebugMarker;
			arrow.Should().NotBeNull();
			GraphSnapshot pushing = behavior.Processor.CaptureSnapshot();
			entity.SharedVariables.SetVar("force", new NumericsVector3(9, 0, 0));

			behavior.Processor.RestoreSnapshot(idle);
			body.ConstantForce.Should().Be(new Vector3(0, 1, 0));
			arrow!.IsQueuedForDeletion().Should().BeTrue("the arrow went with the activation it was drawn for");

			behavior.Processor.RestoreSnapshot(pushing);
			body.ConstantForce.Should().Be(
				new Vector3(5, 0, 0),
				"a restore puts back what the run held; only the next fixed update resolves the input again");
			Context(behavior.Processor, push).DebugMarker.Should().NotBeNull().And.NotBeSameAs(arrow);
		}
		finally
		{
			// Stopping releases the arrow, which lives in the viewport rather than under the world.
			behavior?.Processor.StopGraph();
			Tree.DebugCollisionsHint = drewCollisions;
			world.Free();
		}
	}

	[TestCase]
	[RequireGodotRuntime]
	public void A_walk_listens_to_its_agent_exactly_while_the_restored_run_walks()
	{
		var entity = new TestEntity3D();
		var agent = new NavigationAgent3D { Name = "Agent", AvoidanceEnabled = true };
		entity.AddChild(agent);
		Tree.Root.AddChild(entity);

		try
		{
			var graph = new ForgeGraph();
			graph.VariableDefinitions.DefineObjectVariable<IForgeEntity>("entity", entity);
			var walk = new NavMoveTo3DNode("Agent", useSafeVelocity: true);
			walk.BindInput(NavMoveTo3DNode.EntityInput, "entity");
			AddFromEntry(graph, walk);

			GraphProcessor processor = StartWithSnapshot(graph, out GraphSnapshot idle);
			GraphSnapshot walking = processor.CaptureSnapshot();
			agent.GetSignalConnectionList(NavigationAgent3D.SignalName.VelocityComputed).Should().ContainSingle();

			processor.RestoreSnapshot(idle);
			agent.GetSignalConnectionList(NavigationAgent3D.SignalName.VelocityComputed).Should().BeEmpty();

			processor.RestoreSnapshot(walking);
			processor.RestoreSnapshot(walking);
			agent.GetSignalConnectionList(NavigationAgent3D.SignalName.VelocityComputed).Should().ContainSingle();

			processor.StopGraph();
			agent.GetSignalConnectionList(NavigationAgent3D.SignalName.VelocityComputed).Should().BeEmpty();
		}
		finally
		{
			entity.Free();
		}
	}

	[TestCase]
	[RequireGodotRuntime]
	public void A_restored_animation_that_is_running_again_has_not_played_out()
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
		Tree.Root.AddChild(entity);

		try
		{
			var graph = new ForgeGraph();
			graph.VariableDefinitions.DefineObjectVariable<IForgeEntity>("entity", entity);
			var play = new PlayAnimationNode(animation: "swing");
			play.BindInput(PlayAnimationNode.EntityInput, "entity");
			AddFromEntry(graph, play);

			var processor = new GraphProcessor(graph);
			processor.StartGraph();

			// Taken before the pass starts playback, so it holds the activation's handle before the pass has used it.
			GraphSnapshot playing = processor.CaptureSnapshot();
			processor.Present();
			PresentationPlayback playback = Context<PlayAnimationNodeContext>(processor, play).Playback;
			playback.Player.Should().BeSameAs(player);

			processor.FixedUpdateGraph(1.0);
			playback.Finished.Should().BeTrue();

			processor.RestoreSnapshot(playing);

			Context<PlayAnimationNodeContext>(processor, play).Playback.Should().BeSameAs(playback);
			playback.Finished
				.Should().BeFalse("the restored activation runs again, so stopping it stops the animation");
		}
		finally
		{
			entity.Free();
		}
	}

	private static GraphProcessor StartWithSnapshot(ForgeGraph graph, out GraphSnapshot idle)
	{
		var processor = new GraphProcessor(graph);
		idle = processor.CaptureSnapshot();
		processor.StartGraph();
		return processor;
	}

	private static void AddFromEntry(ForgeGraph graph, ForgeNode node)
	{
		graph.AddNode(node);
		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			node.InputPorts[StateNode<StateNodeContext>.InputPort]));
	}

	private static ForceOverride3DNodeContext Context(GraphProcessor processor, ForgeNode node)
	{
		return Context<ForceOverride3DNodeContext>(processor, node);
	}

	private static T Context<T>(GraphProcessor processor, ForgeNode node)
		where T : StateNodeContext, new()
	{
		return processor.GraphContext.GetNodeContext<T>(node.NodeID);
	}
}
