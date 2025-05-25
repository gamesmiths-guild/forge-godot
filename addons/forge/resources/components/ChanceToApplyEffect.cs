// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Effects.Components;
using Gamesmiths.Forge.Godot.Core;
using Gamesmiths.Forge.Godot.Resources.Magnitudes;
using Godot;

namespace Gamesmiths.Forge.Godot.Resources.Components;

[Tool]
[GlobalClass]
public partial class ChanceToApplyEffect : ForgeEffectComponent
{
	[Export]
	public ForgeScalableFloat Chance { get; set; } = new(1);

	public override IEffectComponent GetComponent()
	{
		return new ChanceToApplyEffectComponent(new ForgeRandom(), Chance.GetScalableFloat());
	}
}
