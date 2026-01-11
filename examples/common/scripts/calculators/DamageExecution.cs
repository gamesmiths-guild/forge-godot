// Copyright © Gamesmiths Guild.

using System.Collections.Generic;
using Gamesmiths.Forge.Attributes;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Effects;
using Gamesmiths.Forge.Effects.Calculator;
using Gamesmiths.Forge.Effects.Magnitudes;
using Gamesmiths.Forge.Effects.Modifiers;
using Gamesmiths.Forge.Events;
using Gamesmiths.Forge.Godot.Core;
using Gamesmiths.Forge.Tags;

namespace Gamesmiths.Forge.Example;

public class DamageExecution : CustomExecution
{
	private readonly DamageType _damageType;

	// Define attributes to capture and modify
	public AttributeCaptureDefinition TargetHealth { get; }

	public AttributeCaptureDefinition TargetIncomingDamage { get; }

	public DamageExecution(DamageType damageType)
	{
		// Capture target mana and magic resistance
		TargetHealth = new AttributeCaptureDefinition(
			"CharacterAttributes.Health",
			AttributeCaptureSource.Target,
			false);

		TargetIncomingDamage = new AttributeCaptureDefinition(
			"MetaAttributes.IncomingDamage",
			AttributeCaptureSource.Target);

		// Register attributes for capture
		AttributesToCapture.Add(TargetHealth);
		AttributesToCapture.Add(TargetIncomingDamage);
		_damageType = damageType;
	}

	public override ModifierEvaluatedData[] EvaluateExecution(
		Effect effect, IForgeEntity target, EffectEvaluatedData? effectEvaluatedData)
	{
		var results = new List<ModifierEvaluatedData>();

		float targetIncomingDamage = CaptureAttributeMagnitude(
			TargetIncomingDamage,
			effect,
			target,
			effectEvaluatedData);

		if (targetIncomingDamage <= 0)
		{
			return [.. results];
		}

		// Apply health reduction to target if attribute exists
		if (TargetHealth.TryGetAttribute(target, out EntityAttribute? targetHealthAttribute))
		{
			results.Add(new ModifierEvaluatedData(
				targetHealthAttribute,
				ModifierOperation.FlatBonus,
				-targetIncomingDamage)); // Negative for damage
		}

		target.Events.Raise(new EventData<DamageType>
		{
			EventTags =
				Tag.RequestTag(ForgeManagers.Instance.TagsManager, "event.damage.taken").GetSingleTagContainer()!,
			Source = effect.Ownership.Owner,
			Target = target,
			EventMagnitude = targetIncomingDamage,
			Payload = _damageType,
		});

		if (effect.Ownership.Source is null)
		{
			return [.. results];
		}

		effect.Ownership.Source.Events.Raise(new EventData<DamageType>
		{
			EventTags =
				Tag.RequestTag(ForgeManagers.Instance.TagsManager, "event.damage.dealt").GetSingleTagContainer()!,
			Source = effect.Ownership.Owner,
			Target = target,
			EventMagnitude = targetIncomingDamage,
			Payload = _damageType,
		});

		return [.. results];
	}
}
