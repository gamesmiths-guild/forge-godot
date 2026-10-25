// Copyright © Gamesmiths Guild.

using System.Collections.Generic;
using Gamesmiths.Forge.Abilities;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Effects;
using Gamesmiths.Forge.Godot.Core;
using Godot;
using Godot.Collections;

namespace Gamesmiths.Forge.Godot.Resources.Abilities;

[Tool]
[GlobalClass]
[Icon("uid://l04b8et2244r")]
public partial class ForgeAbilityData : Resource
{
	private AbilityData? _data;
	private AbilityInstancingPolicy _instancingPolicy;

	[Export]
	public string Name { get; set; } = "New Ability";

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

	/// <summary>
	/// Gets or sets the input action the ability activates from, one of the project's input actions
	/// (<see cref="ForgeSettings.InputActionsSetting"/>). Empty means the ability is not bound to input.
	/// </summary>
	[ExportGroup("Input")]
	[Export]
	public string InputAction { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the change of <see cref="InputAction"/> that activates the ability.
	/// </summary>
	[Export]
	public InputEdge InputEdge { get; set; }

	/// <summary>
	/// Gets or sets what builds the activation data from the tick's input. Unset activates the ability without data.
	/// </summary>
	[Export]
	public ForgeInputActivationData? InputActivationData { get; set; }

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
			() => AbilityBehavior?.GetBehavior()!,
			GetInputBinding());
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

		// Suggested rather than enforced, so a name typed before its action is added to the settings is kept.
		if (property["name"].AsStringName() == PropertyName.InputAction)
		{
			property["hint"] = (int)PropertyHint.EnumSuggestion;
			property["hint_string"] = string.Join(',', ForgeSettings.GetInputActionSet().Actions);
		}
	}
#endif

	private AbilityInputBinding? GetInputBinding()
	{
		if (string.IsNullOrEmpty(InputAction))
		{
			return null;
		}

		return InputActivationData?.CreateBinding(InputAction, InputEdge)
			?? AbilityInputBinding.ForAction(InputAction, InputEdge);
	}

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
				AbilityTriggerData eventTrigger = AbilityBehavior?.CreateEventTrigger(triggerTag, Priority)
					?? AbilityTriggerData.ForEvent(triggerTag, Priority);
#if TOOLS
				ReportDroppedPayload(eventTrigger, triggerTag);
#endif
				return eventTrigger;
			case TriggerSource.TagAdded:
				return AbilityTriggerData.ForTagAdded(triggerTag);
			case TriggerSource.TagPresent:
				return AbilityTriggerData.ForTagPresent(triggerTag);
			default:
				return null;
		}
	}

#if TOOLS
	// A typed trigger can't be discovered by reflection in AOT builds, so a typed behavior has to name its payload
	// type through CreateEventTrigger. Missing that would only drop the payload, so it is caught here in the editor.
	private void ReportDroppedPayload(AbilityTriggerData eventTrigger, Tags.Tag triggerTag)
	{
		if (AbilityBehavior is null || eventTrigger != AbilityTriggerData.ForEvent(triggerTag, Priority))
		{
			return;
		}

		System.Type? payloadInterface = System.Array.Find(
			AbilityBehavior.GetBehavior().GetType().GetInterfaces(),
			x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IAbilityBehavior<>));

		if (payloadInterface is null)
		{
			return;
		}

		string payloadType = payloadInterface.GetGenericArguments()[0].Name;
		GD.PushError(
			$"Ability '{Name}': {AbilityBehavior.GetType().Name} builds an IAbilityBehavior<{payloadType}> but does " +
			"not override CreateEventTrigger, so the event payload is dropped. Override it to return " +
			$"AbilityTriggerData.ForEvent<{payloadType}>(triggerTag, priority).");
	}
#endif
}
