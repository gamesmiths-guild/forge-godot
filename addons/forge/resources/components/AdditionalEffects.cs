// Copyright © Gamesmiths Guild.

using System.Collections.Generic;
using Gamesmiths.Forge.Effects.Components;
using Godot;

namespace Gamesmiths.Forge.Godot.Resources.Components;

[Tool]
[GlobalClass]
public partial class AdditionalEffects : ForgeEffectComponent
{
	[Export]
	public bool CopyDataFromOriginalEffect { get; set; }

	[ExportGroup("On Application")]
	[Export]
	public ForgeConditionalEffect[] OnApplication { get; set; } = [];

	[ExportGroup("On Complete")]
	[Export]
	public ForgeConditionalEffect[] OnCompleteAlways { get; set; } = [];

	[Export]
	public ForgeConditionalEffect[] OnCompleteNormal { get; set; } = [];

	[Export]
	public ForgeConditionalEffect[] OnCompletePrematurely { get; set; } = [];

	public override IEffectComponent GetComponent()
	{
		return new AdditionalEffectsEffectComponent(
			Convert(OnApplication),
			Convert(OnCompleteAlways),
			Convert(OnCompleteNormal),
			Convert(OnCompletePrematurely),
			CopyDataFromOriginalEffect);
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
				GD.PushError($"{nameof(AdditionalEffects)}: a conditional effect is missing its effect data.");
				continue;
			}

			converted.Add(conditionalEffect.GetConditionalEffect());
		}

		return [.. converted];
	}
}
