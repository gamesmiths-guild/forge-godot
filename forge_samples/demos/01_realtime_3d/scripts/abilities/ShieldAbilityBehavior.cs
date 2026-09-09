// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Abilities;
using Gamesmiths.Forge.Godot.Resources;
using Gamesmiths.Forge.Godot.Resources.Abilities;
using Godot;

namespace Gamesmiths.Forge.Example;

[Tool]
[GlobalClass]
public partial class ShieldAbilityBehavior : ForgeAbilityBehavior
{
	[Export]
	public required ForgeEffectData ProtectEffect { get; set; }

	public override IAbilityBehavior GetBehavior()
	{
		return new ShieldAbilityBehaviorImplementation(ProtectEffect.GetEffectData());
	}
}
