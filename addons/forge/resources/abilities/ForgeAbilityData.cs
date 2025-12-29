// Copyright © Gamesmiths Guild.

using System.Collections.Generic;
using Gamesmiths.Forge.Abilities;
using Gamesmiths.Forge.Effects;
using Godot;
using Godot.Collections;

namespace Gamesmiths.Forge.Godot.Resources.Abilities;

[Tool]
[GlobalClass]
public partial class ForgeAbilityData : Resource
{
	private AbilityData? _data;
	private AbilityInstancingPolicy _instancingPolicy;

	[Export]
	public string Name { get; set; } = string.Empty;

	[Export]
	public AbilityInstancingPolicy InstancingPolicy
	{
		get => _instancingPolicy;

		set
		{
			_instancingPolicy = value;
			NotifyPropertyListChanged();
		}
	}

	[Export]
	public bool RetriggerInstancedAbility { get; set; }

	[Export]
	public ForgeEffectData[]? CooldownEffects { get; set; }

	[Export]
	public ForgeEffectData? CostEffect { get; set; }

	[Export]
	public ForgeAbilityBehavior? AbilityBehavior { get; set; }

	[ExportGroup("Tags")]
	[Export]
	public ForgeTagContainer? AbilityTags { get; set; }

	[Export]
	public ForgeTagContainer? CancelAbilitiesWithTag { get; set; }

	[Export]
	public ForgeTagContainer? BlockAbilitiesWithTag { get; set; }

	[Export]
	public ForgeTagContainer? ActivationOwnedTags { get; set; }

	[Export]
	public ForgeTagContainer? ActivationRequiredTags { get; set; }

	[Export]
	public ForgeTagContainer? ActivationBlockedTags { get; set; }

	[Export]
	public ForgeTagContainer? SourceRequiredTags { get; set; }

	[Export]
	public ForgeTagContainer? SourceBlockedTags { get; set; }

	[Export]
	public ForgeTagContainer? TargetRequiredTags { get; set; }

	[Export]
	public ForgeTagContainer? TargetBlockedTags { get; set; }

	public AbilityData GetAbilityData()
	{
		if (_data.HasValue)
		{
			return _data.Value;
		}

		List<EffectData> cooldownEffects = [];
		foreach (ForgeEffectData cooldownEffect in CooldownEffects ?? [])
		{
			cooldownEffects.Add(cooldownEffect.GetEffectData());
		}

		_data = new AbilityData(
			Name,
			CostEffect?.GetEffectData(),
			[.. cooldownEffects],
			AbilityTags?.GetTagContainer(),
			InstancingPolicy,
			RetriggerInstancedAbility,
			null,
			CancelAbilitiesWithTag?.GetTagContainer(),
			BlockAbilitiesWithTag?.GetTagContainer(),
			ActivationOwnedTags?.GetTagContainer(),
			ActivationRequiredTags?.GetTagContainer(),
			ActivationBlockedTags?.GetTagContainer(),
			SourceRequiredTags?.GetTagContainer(),
			SourceBlockedTags?.GetTagContainer(),
			TargetRequiredTags?.GetTagContainer(),
			TargetBlockedTags?.GetTagContainer(),
			() => AbilityBehavior?.GetBehavior()!);
		return _data.Value;
	}

#if TOOLS
	public override void _ValidateProperty(Dictionary property)
	{
		if (property["name"].AsStringName() == PropertyName.RetriggerInstancedAbility
			&& InstancingPolicy != AbilityInstancingPolicy.PerEntity)
		{
			property["usage"] = (int)PropertyUsageFlags.NoEditor;
		}
	}
#endif
}
