// Copyright © Gamesmiths Guild.

using System.Collections.Generic;
using Gamesmiths.Forge.Effects.Components;
using Godot;

namespace Gamesmiths.Forge.Godot.Resources.Components;

[Tool]
[GlobalClass]
public partial class GrantAbility : ForgeEffectComponent
{
	[Export]
	public ForgeGrantAbilityConfig[] GrantAbilityConfigs { get; set; } = [];

	public override IEffectComponent GetComponent()
	{
		List<GrantAbilityConfig> configs = [];

		foreach (ForgeGrantAbilityConfig config in GrantAbilityConfigs)
		{
			configs.Add(config.GetGrantAbilityConfig());
		}

		return new GrantAbilityEffectComponent([.. configs]);
	}
}
