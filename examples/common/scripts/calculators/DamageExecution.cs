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
	private readonly EffectData _fireEffectData;

	// Define attributes to capture and modify
	public AttributeCaptureDefinition TargetHealth { get; }

	public AttributeCaptureDefinition TargetMana { get; }

	public AttributeCaptureDefinition TargetIncomingDamage { get; }

	public DamageExecution(EffectData fireEffectData)
	{
		// Capture target mana and magic resistance
		TargetHealth = new AttributeCaptureDefinition(
			"CharacterAttributes.Health",
			AttributeCaptureSource.Target);

		TargetMana = new AttributeCaptureDefinition(
			"CharacterAttributes.Mana",
			AttributeCaptureSource.Target);

		TargetIncomingDamage = new AttributeCaptureDefinition(
			"MetaAttributes.IncomingDamage",
			AttributeCaptureSource.Target);

		// Register attributes for capture
		AttributesToCapture.Add(TargetHealth);
		AttributesToCapture.Add(TargetMana);
		AttributesToCapture.Add(TargetIncomingDamage);
		_fireEffectData = fireEffectData;
	}

	public override ModifierEvaluatedData[] EvaluateExecution(
		Effect effect, IForgeEntity target, EffectEvaluatedData? effectEvaluatedData)
	{
		var results = new List<ModifierEvaluatedData>();

		var targetMana = CaptureAttributeMagnitude(
			TargetMana,
			effect,
			target,
			effectEvaluatedData);

		float targetIncomingDamage = CaptureAttributeMagnitude(
			TargetIncomingDamage,
			effect,
			target,
			effectEvaluatedData);

		if (target.Tags.CombinedTags.HasTag(Tag.RequestTag(ForgeManagers.Instance.TagsManager, "effect.mana_shield")))
		{
			if (targetMana > 10)
			{
				targetIncomingDamage *= 0.2f;

				// Apply mana reduction to source if attribute exists
				if (TargetMana.TryGetAttribute(target, out EntityAttribute? targetManaAttribute))
				{
					results.Add(new ModifierEvaluatedData(
						targetManaAttribute,
						ModifierOperation.FlatBonus,
						-10));
				}
			}
			else
			{
				target.Events.Raise(new EventData
				{
					EventTags =
						Tag.RequestTag(ForgeManagers.Instance.TagsManager, "event.shield.not_enough_mana")
							.GetSingleTagContainer()!,
					Source = target,
					Target = target,
				});
			}
		}

		// Apply health reduction to target if attribute exists
		if (TargetHealth.TryGetAttribute(target, out EntityAttribute? targetHealthAttribute))
		{
			results.Add(new ModifierEvaluatedData(
				targetHealthAttribute,
				ModifierOperation.FlatBonus,
				-targetIncomingDamage)); // Negative for damage
		}

		if (effect.Ownership.Source!.Tags.CombinedTags.HasTag(Tag.RequestTag(ForgeManagers.Instance.TagsManager, "effect.fire")))
		{
			// Apply fire effect to target
			var fireEffect = new Effect(_fireEffectData, new EffectOwnership(effect.Ownership.Owner, effect.Ownership.Source));
			target.EffectsManager.ApplyEffect(fireEffect);
		}

		return [.. results];
	}
}
