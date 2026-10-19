// Copyright © Gamesmiths Guild.

using FluentAssertions;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Effects;
using Gamesmiths.Forge.Effects.Duration;
using Gamesmiths.Forge.Effects.Magnitudes;
using Gamesmiths.Forge.Godot.Nodes;
using GdUnit4;
using Godot;

namespace Gamesmiths.Forge.Godot.Tests.Nodes;

/// <summary>
/// How a <see cref="ForgeEntity"/> drives its simulation: effects advance on the physics tick through
/// <see cref="EntitySimulation"/>, never on the frame, and every entity draws from its own seeded random.
/// </summary>
[TestSuite]
public class ForgeEntityTickTests
{
	// A power of two, so the float duration below is exactly one step.
	private const double Step = 1.0 / 64.0;

	private static Window Root => ((SceneTree)Engine.GetMainLoop()).Root;

	[TestCase]
	[RequireGodotRuntime]
	public void Every_entity_gets_its_own_simulation_random_when_ready()
	{
		var first = new ForgeEntity();
		var second = new ForgeEntity();
		Root.AddChild(first);
		Root.AddChild(second);

		try
		{
			first.Random.Should().NotBeNull();
			((IForgeEntity)first).Random.Should().BeSameAs(first.Random);
			first.Random.Seed.Should().NotBe(second.Random.Seed);
		}
		finally
		{
			first.Free();
			second.Free();
		}
	}

	[TestCase]
	[RequireGodotRuntime]
	public void Effects_advance_on_the_physics_tick_and_not_on_the_frame()
	{
		var entity = new ForgeEntity();
		Root.AddChild(entity);

		try
		{
			var effectData = new EffectData(
				"One step",
				new DurationData(
					DurationType.HasDuration,
					new ModifierMagnitude(MagnitudeCalculationType.ScalableFloat, new ScalableFloat((float)Step))));
			entity.EffectsManager.ApplyEffect(new Effect(effectData, new EffectOwnership(entity, entity)));

			entity._Process(1.0);
			entity.EffectsManager.GetActiveEffects().Should().ContainSingle("a frame no longer counts down effects");

			entity._PhysicsProcess(Step);
			entity.EffectsManager.GetActiveEffects().Should().BeEmpty("one physics tick uses up the one-step effect");
		}
		finally
		{
			entity.Free();
		}
	}
}
