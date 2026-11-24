// Copyright © Gamesmiths Guild.

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

/// <summary>
/// The arena's damage: the effect's incoming damage, times the strength a shot hit with, off the target's health. A
/// hit that takes the last of it raises <c>event.arena.frag</c> on the target, naming who dealt it, which the target's
/// Die ability answers.
/// </summary>
public class ArenaDamageExecution : CustomExecution
{
	/// <summary>
	/// Gets the target's health.
	/// </summary>
	public AttributeCaptureDefinition TargetHealth { get; }

	/// <summary>
	/// Gets the damage the effect brings.
	/// </summary>
	public AttributeCaptureDefinition TargetIncomingDamage { get; }

	/// <summary>
	/// Initializes a new instance of the <see cref="ArenaDamageExecution"/> class.
	/// </summary>
	public ArenaDamageExecution()
	{
		TargetHealth = new AttributeCaptureDefinition(
			"CharacterAttributes.Health",
			AttributeCaptureSource.Target,
			false);
		TargetIncomingDamage = new AttributeCaptureDefinition(
			"MetaAttributes.IncomingDamage",
			AttributeCaptureSource.Target);

		AttributesToCapture.Add(TargetHealth);
		AttributesToCapture.Add(TargetIncomingDamage);
	}

	/// <inheritdoc/>
	public override ModifierEvaluatedData[] EvaluateExecution(
		Effect effect,
		IForgeEntity target,
		EffectEvaluatedData? effectEvaluatedData)
	{
		float damage = CaptureAttributeMagnitude(TargetIncomingDamage, effect, target, effectEvaluatedData);

		if (effectEvaluatedData?.TryGetContextData(out float strength) == true)
		{
			damage *= strength;
		}

		if (damage <= 0 || !TargetHealth.TryGetAttribute(target, out EntityAttribute? health))
		{
			return [];
		}

		if (health.CurrentValue > health.Min && health.CurrentValue - damage <= health.Min)
		{
			target.Events.Raise(new EventData<Frag>
			{
				EventTags =
					Tag.RequestTag(ForgeManagers.Instance.TagsManager, "event.arena.frag").GetSingleTagContainer()!,
				Source = effect.Ownership.Owner,
				Target = target,
				Payload = new Frag(effect.Ownership.Owner),
			});
		}

		return [new ModifierEvaluatedData(health, ModifierOperation.FlatBonus, -damage)];
	}
}
