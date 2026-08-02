// Copyright © Gamesmiths Guild.

using System.Collections.Generic;
using Gamesmiths.Forge.Effects.Components;
using Godot;

namespace Gamesmiths.Forge.Godot.Resources.Components;

[Tool]
[GlobalClass]
public partial class StackThreshold : ForgeEffectComponent
{
	[Export(PropertyHint.Range, "2,10,1,or_greater")]
	public int Threshold { get; set; } = 2;

	[Export]
	public bool CopyDataFromOriginalEffect { get; set; }

	[Export]
	public ForgeConditionalEffect[] ThresholdEffects { get; set; } = [];

	public override IEffectComponent GetComponent()
	{
		return new StackThresholdEffectComponent(Threshold, Convert(ThresholdEffects), CopyDataFromOriginalEffect);
	}

	private static ConditionalEffect[] Convert(ForgeConditionalEffect[] conditionalEffects)
	{
		List<ConditionalEffect> converted = [];

		foreach (ForgeConditionalEffect conditionalEffect in conditionalEffects)
		{
			if (conditionalEffect is null)
			{
				continue;
			}

			// A conditional with no effect to apply has nothing to contribute, and reaching into it would fail on the
			// missing reference rather than say what is wrong.
			if (conditionalEffect.EffectData is null)
			{
				GD.PushError($"{nameof(StackThreshold)}: a threshold effect is missing its effect data.");
				continue;
			}

			converted.Add(conditionalEffect.GetConditionalEffect());
		}

		return [.. converted];
	}
}
