// Copyright © Gamesmiths Guild.

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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

	[ExportGroup("Trigger")]
	[Export]
	public TriggerSource TriggerSource { get; set; }

	[Export]
	public ForgeTag? TriggerTag { get; set; }

	[Export]
	public int Priority { get; set; }

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
			GetTriggerData(),
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

	private AbilityTriggerData? GetTriggerData()
	{
		if (TriggerSource == TriggerSource.None)
		{
			return null;
		}

		Tags.Tag triggerTag = TriggerTag!.GetTag();

		switch (TriggerSource)
		{
			case TriggerSource.Event:
				IAbilityBehavior? behavior = AbilityBehavior?.GetBehavior();

				if (behavior is not null)
				{
					System.Type behaviorType = behavior.GetType();
					System.Type? payloadInterface =
						System.Array.Find(behaviorType.GetInterfaces(), x =>
							x.IsGenericType &&
							x.GetGenericTypeDefinition() == typeof(IAbilityBehavior<>));

					if (payloadInterface is not null)
					{
						System.Type payloadType = payloadInterface.GetGenericArguments()[0];
						MethodInfo method = typeof(AbilityTriggerData)
							.GetMethods(BindingFlags.Public | BindingFlags.Static)
							.First(x => x.Name == nameof(AbilityTriggerData.ForEvent)
								&& x.IsGenericMethodDefinition)
							.MakeGenericMethod(payloadType);

						return (AbilityTriggerData)method.Invoke(null, [triggerTag, Priority])!;
					}
				}

				GD.Print($"call {triggerTag} - {Name}");

				return AbilityTriggerData.ForEvent(triggerTag, Priority);
			case TriggerSource.TagAdded:
				return AbilityTriggerData.ForTagAdded(triggerTag);
			case TriggerSource.TagPresent:
				return AbilityTriggerData.ForTagPresent(triggerTag);
			default:
				return null;
		}
	}
}
