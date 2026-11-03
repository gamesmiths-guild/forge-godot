// Copyright © Gamesmiths Guild.

using System;
using System.Runtime.CompilerServices;
using FluentAssertions;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Cues;
using Gamesmiths.Forge.Effects;
using Gamesmiths.Forge.Effects.Duration;
using Gamesmiths.Forge.Godot.Core;
using Gamesmiths.Forge.Godot.Nodes;
using Gamesmiths.Forge.Tags;
using GdUnit4;
using Godot;

// A leak test has to collect to prove something was let go of.
#pragma warning disable S1215

namespace Gamesmiths.Forge.Godot.Tests.Nodes;

/// <summary>
/// A <see cref="ForgeEntity"/> that is freed is torn down: its effects end, their cues with them, and no effect it was
/// given keeps it reachable.
/// </summary>
[TestSuite]
public class ForgeEntityTeardownTests
{
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
