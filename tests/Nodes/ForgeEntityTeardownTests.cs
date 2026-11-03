// Copyright © Gamesmiths Guild.

using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using FluentAssertions;
using Gamesmiths.Forge.Abilities;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Cues;
using Gamesmiths.Forge.Effects;
using Gamesmiths.Forge.Effects.Duration;
using Gamesmiths.Forge.Godot.Core;
using Gamesmiths.Forge.Godot.Core.Statescript.Interop;
using Gamesmiths.Forge.Godot.Core.Statescript.Nodes.State;
using Gamesmiths.Forge.Godot.Nodes;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Nodes;
using Gamesmiths.Forge.Tags;
using GdUnit4;
using Godot;
using ForgeGraph = Gamesmiths.Forge.Statescript.Graph;
using Node = Godot.Node;

// A leak test has to collect to prove something was let go of.
#pragma warning disable S1215

namespace Gamesmiths.Forge.Godot.Tests.Nodes;

/// <summary>
/// A <see cref="ForgeEntity"/> that is freed is torn down: its effects end, their cues with them, what its abilities
/// showed ends, and no effect it was given keeps it reachable.
/// </summary>
[TestSuite]
public class ForgeEntityTeardownTests
{
	private const double Step = 1.0 / 60.0;

	private static Window Root => ((SceneTree)Engine.GetMainLoop()).Root;

	[TestCase]
	[RequireGodotRuntime]
	public void An_entity_freed_mid_effect_is_collected_and_its_cues_end()
	{
		CuesManager cues = ForgeManagers.Instance.CuesManager;
		var cue = Tag.RequestTag(ForgeManagers.Instance.TagsManager, "cue");
		var handler = new RemovalCountingCueHandler();
		cues.RegisterCue(cue, handler);
		var source = new ForgeEntity();
		Root.AddChild(source);

		try
		{
			var effect = new Effect(Shown(cue), new EffectOwnership(source, source));

			WeakReference freed = ApplyAndFree(effect);
			cues.PresentCues();

			GC.Collect();
			GC.WaitForPendingFinalizers();
			GC.Collect();

			freed.IsAlive.Should().BeFalse("the shared effect it was given must not keep it reachable");
			handler.Removals.Should().Be(1);
			handler.LastInterrupted.Should().BeTrue();
			GC.KeepAlive(effect);
		}
		finally
		{
			cues.UnregisterCue(cue, handler);
			source.Free();
		}
	}

	[TestCase]
	[RequireGodotRuntime]
	public async Task An_entity_freed_from_inside_its_own_change_is_torn_down_once_it_unwinds()
	{
		CuesManager cues = ForgeManagers.Instance.CuesManager;
		var cue = Tag.RequestTag(ForgeManagers.Instance.TagsManager, "cue");
		var handler = new RemovalCountingCueHandler();
		cues.RegisterCue(cue, handler);
		var entity = new ForgeEntity();
		Root.AddChild(entity);

		try
		{
			entity.EffectsManager.ApplyEffect(new Effect(Shown(cue), new EffectOwnership(entity, entity)));
			cues.PresentCues();
			entity.EffectsManager.OnEffectApplied += _ => entity.Free();

			entity.EffectsManager.ApplyEffect(new Effect(Shown(cue), new EffectOwnership(entity, entity)));
			handler.Removals.Should().Be(0, "the teardown waits for the change to unwind");

			// Deferred calls flush at the end of a frame, which process_frame can resume this before.
			for (int frame = 0; frame < 5 && entity.EffectsManager.GetActiveEffects().Any(); frame++)
			{
				await Root.ToSignal(Root.GetTree(), SceneTree.SignalName.ProcessFrame);
			}

			cues.PresentCues();

			entity.EffectsManager.GetActiveEffects().Should().BeEmpty();
			handler.Removals.Should().Be(1);
		}
		finally
		{
			cues.UnregisterCue(cue, handler);
		}
	}

	[TestCase]
	[RequireGodotRuntime]
	public void What_a_freed_entity_s_abilities_showed_ends_with_it()
	{
		var entity = new ForgeEntity();
		var light = new OmniLight3D { LightEnergy = 1.0f };
		Root.AddChild(entity);
		Root.AddChild(light);
		ForgeBootstrap bootstrap = Root.GetNode<ForgeBootstrap>("Forge Bootstrap");

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

			entity.Abilities.GrantAbilityPermanently(
				new AbilityData("Glow", behaviorFactory: () => new GraphAbilityBehavior(graph)),
				1,
				LevelComparison.Higher,
				null).TryActivate(out _).Should().BeTrue();
			bootstrap._Process(Step);
			light.LightEnergy.Should().Be(5.0f);

			entity.Free();

			light.LightEnergy.Should().Be(1.0f, "the bootstrap no longer presents a freed entity");
		}
		finally
		{
			light.Free();
		}
	}

	// Kept out of line, so the entity never lives in the caller's frame, where the JIT could keep it reachable.
	[MethodImpl(MethodImplOptions.NoInlining)]
	private static WeakReference ApplyAndFree(Effect effect)
	{
		var entity = new ForgeEntity();
		Root.AddChild(entity);
		entity.EffectsManager.ApplyEffect(effect);
		ForgeManagers.Instance.CuesManager.PresentCues();
		entity.Free();

		return new WeakReference(entity);
	}

	private static EffectData Shown(Tag cue)
	{
		return new EffectData(
			"Shown",
			new DurationData(DurationType.Infinite),
			cues:
			[
				new CueData(
					new TagContainer(ForgeManagers.Instance.TagsManager, [cue]),
					0,
					1,
					CueMagnitudeType.EffectLevel)
			]);
	}

	private sealed class RemovalCountingCueHandler : ICueHandler
	{
		public int Removals { get; private set; }

		public bool LastInterrupted { get; private set; }

		public void OnExecute(IForgeEntity? target, CueParameters? parameters)
		{
		}

		public void OnApply(IForgeEntity? target, CueParameters? parameters)
		{
		}

		public void OnRemove(IForgeEntity? target, bool interrupted)
		{
			Removals++;
			LastInterrupted = interrupted;
		}

		public void OnUpdate(IForgeEntity? target, CueParameters? parameters)
		{
		}
	}
}
