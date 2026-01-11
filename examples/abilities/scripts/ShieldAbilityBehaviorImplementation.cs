// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Abilities;
using Gamesmiths.Forge.Effects;
using Gamesmiths.Forge.Events;
using Gamesmiths.Forge.Godot.Core;
using Gamesmiths.Forge.Godot.Nodes;
using Gamesmiths.Forge.Tags;
using Godot;

namespace Gamesmiths.Forge.Example;

public sealed class ShieldAbilityBehaviorImplementation : IAbilityBehavior
{
	private readonly EffectData _effectData;
	private Effect? _protectEffect;
	private ActiveEffectHandle? _activeEffectHandle;
	private AbilityInstanceHandle? _abilityInstanceHandle;
	private EventSubscriptionToken? _damageTakenSubscriptionToken;

	public ShieldAbilityBehaviorImplementation(EffectData effectData)
	{
		_effectData = effectData;
	}

	public void OnStarted(AbilityBehaviorContext context)
	{
		if (context.Owner is not ForgeEntity ownerNode)
		{
			context.InstanceHandle.End();
			return;
		}

		_abilityInstanceHandle = context.InstanceHandle;

		context.AbilityHandle.CommitAbility();

		_protectEffect ??= new Effect(_effectData, new EffectOwnership(context.Owner, context.Source));

		GD.Print(_effectData.Modifiers[0].Magnitude);

		_activeEffectHandle = ownerNode.EffectsManager.ApplyEffect(_protectEffect);

		ownerNode.Attributes["CharacterAttributes.Mana"].OnValueChanged += Mana_OnValueChanged;

		ForgeManagers.Instance.CuesManager.ApplyCue(
			Tag.RequestTag(ForgeManagers.Instance.TagsManager, "cue.vfx.shield"),
			ownerNode,
			null);

		_damageTakenSubscriptionToken = ownerNode.Events.Subscribe<DamageType>(
			Tag.RequestTag(ForgeManagers.Instance.TagsManager, "event.damage.taken"),
			_ => context.AbilityHandle.CommitCost());
	}

	public void OnEnded(AbilityBehaviorContext context)
	{
		if (context.Owner is not ForgeEntity ownerNode)
		{
			return;
		}

		ForgeManagers.Instance.CuesManager.RemoveCue(
			Tag.RequestTag(ForgeManagers.Instance.TagsManager, "cue.vfx.shield"), ownerNode, false);

		ownerNode.EffectsManager.UnapplyEffect(_activeEffectHandle!);

		ownerNode.Attributes["CharacterAttributes.Mana"].OnValueChanged -= Mana_OnValueChanged;

		ownerNode.Events.Unsubscribe(_damageTakenSubscriptionToken!.Value);
	}

	private void Mana_OnValueChanged(Attributes.EntityAttribute attribute, int change)
	{
		if (attribute.CurrentValue < 10)
		{
			_abilityInstanceHandle?.End();
		}
	}
}
