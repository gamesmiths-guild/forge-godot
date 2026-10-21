// Copyright © Gamesmiths Guild.

using FluentAssertions;
using Gamesmiths.Forge.Abilities;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Cues;
using Gamesmiths.Forge.Godot.Core;
using Gamesmiths.Forge.Godot.Core.Statescript.Interop;
using Gamesmiths.Forge.Godot.Core.Statescript.Nodes.State;
using Gamesmiths.Forge.Godot.Core.Statescript.Providers;
using Gamesmiths.Forge.Godot.Nodes;
using Gamesmiths.Forge.Godot.Resources.Abilities;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Nodes;
using Gamesmiths.Forge.Tags;
using GdUnit4;
using Godot;
using ForgeGraph = Gamesmiths.Forge.Statescript.Graph;
using Node = Godot.Node;

namespace Gamesmiths.Forge.Godot.Tests.Nodes;

/// <summary>
/// Where the presentation pass runs in a Godot game: each <see cref="ForgeEntity"/> presents its abilities at the end
/// of its frame, and the Forge bootstrap dispatches the frame's cues once everything else has processed.
/// </summary>
[TestSuite]
public class ForgeEntityPresentationTests
{
	private const double Step = 1.0 / 60.0;

	private static Window Root => ((SceneTree)Engine.GetMainLoop()).Root;

	[TestCase]
	[RequireGodotRuntime]
	public void An_entity_presents_its_abilities_at_the_end_of_its_frame()
	{
		var entity = new ForgeEntity();
		var light = new OmniLight3D { LightEnergy = 1.0f };
		Root.AddChild(entity);
		Root.AddChild(light);

		try
		{
			var graph = new ForgeGraph();
			graph.VariableDefinitions.DefineObjectVariable<Node>("light", light);
			graph.VariableDefinitions.DefineVariable("energy", 5.0);

			var hold = new NodePropertyOverrideNode("light_energy", InteropValueType.Float);
			hold.BindInput(NodePropertyOverrideNode.NodeInput, "light");
			hold.BindInput(NodePropertyOverrideNode.ValueInput, "energy");
			graph.AddNode(hold);
			graph.AddConnection(new Connection(
				graph.EntryNode.OutputPorts[EntryNode.OutputPort],
				hold.InputPorts[StateNode<StateNodeContext>.InputPort]));

			AbilityHandle handle = entity.Abilities.GrantAbilityPermanently(
				new AbilityData("Glow", behaviorFactory: () => new GraphAbilityBehavior(graph)),
				1,
				LevelComparison.Higher,
				null);

			handle.TryActivate(out _).Should().BeTrue();
			light.LightEnergy.Should().Be(1.0f, "nothing is shown before the frame ends");

			entity._Process(Step);
			light.LightEnergy.Should().Be(5.0f);

			handle.Cancel();
			entity._Process(Step);
			light.LightEnergy.Should().Be(1.0f, "the ended ability's graph is presented once more");
		}
		finally
		{
			entity.Free();
			light.Free();
		}
	}

	[TestCase]
	[RequireGodotRuntime]
	public void Cue_handlers_wait_for_the_bootstrap_at_the_end_of_the_frame()
	{
		CuesManager cues = ForgeManagers.Instance.CuesManager;
		ForgeBootstrap bootstrap = Root.GetNode<ForgeBootstrap>("Forge Bootstrap");
		var cue = Tag.RequestTag(ForgeManagers.Instance.TagsManager, "cue");
		var handler = new CountingCueHandler();
		cues.RegisterCue(cue, handler);

		try
		{
			cues.DeferDispatch.Should().BeTrue();
			bootstrap.ProcessPriority.Should().Be(int.MaxValue, "the bootstrap processes after everything else");

			cues.ExecuteCue(cue, null, null);
			handler.Executions.Should().Be(0);

			bootstrap._Process(Step);

			handler.Executions.Should().Be(1);
		}
		finally
		{
			cues.UnregisterCue(cue, handler);
		}
	}

	[TestCase]
	[RequireGodotRuntime]
	public void The_sample_dash_lasts_exactly_one_roll_whatever_the_animation_player_does()
	{
		CharacterBody3D body = GD.Load<PackedScene>("res://forge_samples/demos/03_statescript/scenes/player.tscn")
			.Instantiate<CharacterBody3D>();
		Root.AddChild(body);

		try
		{
			ForgeEntity entity = body.GetNode<ForgeEntity>("Forge Entity");
			var dash = GD.Load<ForgeAbilityData>("res://forge_samples/demos/03_statescript/abilities/player/dash.tres");
			entity.Abilities.TryGetAbility(dash.GetAbilityData(), out AbilityHandle? handle, entity).Should().BeTrue();
			double roll = body.GetNode<AnimationPlayer>("AnimationPlayer").GetAnimation("roll").Length;

			// The steps it takes for a roll's length to run, counted the way the node counts them.
			int steps = 0;

			for (double elapsed = 0; elapsed < roll; elapsed += Step)
			{
				steps++;
			}

			handle!.TryActivate(
				new AimActivationData(body.GlobalPosition, Vector3.Forward, body.GlobalPosition + Vector3.Forward),
				out _).Should().BeTrue();

			// No frame runs here, so the animation player never advances: only the physics steps time the dash.
			for (int i = 1; i < steps; i++)
			{
				entity._PhysicsProcess(Step);
				handle.IsActive.Should().BeTrue($"the roll is still under way after step {i}");
			}

			entity._PhysicsProcess(Step);

			handle.IsActive.Should().BeFalse("the dash ends on the step its roll's length has run");
		}
		finally
		{
			body.Free();
		}
	}

	private sealed class CountingCueHandler : ICueHandler
	{
		public int Executions { get; private set; }

		public void OnExecute(IForgeEntity? target, CueParameters? parameters)
		{
			Executions++;
		}

		public void OnApply(IForgeEntity? target, CueParameters? parameters)
		{
		}

		public void OnRemove(IForgeEntity? target, bool interrupted)
		{
		}

		public void OnUpdate(IForgeEntity? target, CueParameters? parameters)
		{
		}
	}
}
