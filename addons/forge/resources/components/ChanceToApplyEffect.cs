// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.GameplayEffects.Components;
using Gamesmiths.Forge.Godot.Core;
using Gamesmiths.Forge.Godot.Resources.Magnitudes;
using Godot;

namespace Gamesmiths.Forge.Godot.Resources.Components;

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
