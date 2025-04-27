// Copyright © 2025 Gamesmiths Guild.

using Gamesmiths.Forge.Core.Godot;
using Gamesmiths.Forge.GameplayEffects.Magnitudes.Godot;
using Godot;

namespace Gamesmiths.Forge.GameplayEffects.Components.Godot;

[Tool]
[GlobalClass]
public partial class ChanceToApplyEffect : EffectComponent
{
	[Export]
	public ScalableFloat Chance { get; set; } = new(1);

	public override IGameplayEffectComponent GetComponent()
	{
		return new ChanceToApplyEffectComponent(new ForgeRandom(), Chance.GetScalableFloat());
	}
}
