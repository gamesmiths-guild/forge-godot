// Copyright © 2025 Gamesmiths Guild.

using Gamesmiths.Forge.Core.Godot;
using Gamesmiths.Forge.GameplayEffects.Magnitudes;
using Godot;

namespace Gamesmiths.Forge.GameplayEffects.Components.Godot;

[Tool]
[GlobalClass]
public partial class ChanceToApplyEffect : EffectComponent
{
	[Export]
	public float Chance { get; set; }

	public override IGameplayEffectComponent GetComponent()
	{
		return new ChanceToApplyEffectComponent(new ForgeRandom(), new ScalableFloat(Chance));
	}
}
