// Copyright © Gamesmiths Guild.

using System.Diagnostics;
using Gamesmiths.Forge.Abilities;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Effects.Components;
using Gamesmiths.Forge.Godot.Resources;
using Gamesmiths.Forge.Godot.Resources.Magnitudes;
using Godot;

namespace ForgeGodot.Addons.Forge.Resources.Components;

[Tool]
[GlobalClass]
public partial class ForgeGrantAbilityConfig : Resource
{
	[Export]
	public ForgeAbilityData? AbilityData { get; set; }

	[Export]
	public ForgeScalableInt AbilityLevel { get; set; } = new(1);

	[Export]
	public AbilityDeactivationPolicy RemovalPolicy { get; set; } = AbilityDeactivationPolicy.CancelImmediately;

	[Export]
	public AbilityDeactivationPolicy InhibitionPolicy { get; set; } = AbilityDeactivationPolicy.CancelImmediately;

	[Export]
	public LevelComparison LevelOverridePolicy { get; set; } = LevelComparison.None;

	public GrantAbilityConfig GetGrantAbilityConfig()
	{
		Debug.Assert(AbilityData is not null, $"{nameof(AbilityData)} reference is missing.");

		return new GrantAbilityConfig(
			AbilityData.GetAbilityData(),
			AbilityLevel.GetScalableInt(),
			RemovalPolicy,
			InhibitionPolicy,
			LevelOverridePolicy);
	}
}
