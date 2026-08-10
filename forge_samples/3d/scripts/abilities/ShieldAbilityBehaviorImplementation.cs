// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Abilities;
using Gamesmiths.Forge.Effects;
using Gamesmiths.Forge.Events;
using Gamesmiths.Forge.Godot.Core;
using Gamesmiths.Forge.Godot.Nodes;
using Gamesmiths.Forge.Tags;

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

		if (!context.AbilityHandle.TryCommitAbility())
		{
			context.InstanceHandle.End();
			return;
		}

		_protectEffect ??= new Effect(_effectData, new EffectOwnership(context.Owner, context.Source));

		_activeEffectHandle = ownerNode.EffectsManager.ApplyEffect(_protectEffect);

		ownerNode.Attributes["CharacterAttributes.Mana"].OnValueChanged += Mana_OnValueChanged;

		ForgeManagers.Instance.CuesManager.ApplyCue(
			Tag.RequestTag(ForgeManagers.Instance.TagsManager, "cue.vfx.shield"),
			ownerNode,
			null);

		_damageTakenSubscriptionToken = ownerNode.Events.Subscribe<DamageType>(
			Tag.RequestTag(ForgeManagers.Instance.TagsManager, "event.damage.taken"),
			_ => context.AbilityHandle.TryCommitCost());
	}

	public void OnEnded(AbilityBehaviorContext context)
	{
		if (context.Owner is not ForgeEntity ownerNode)
		{
			return;
		}

		ForgeManagers.Instance.CuesManager.RemoveCue(
			Tag.RequestTag(ForgeManagers.Instance.TagsManager, "cue.vfx.shield"), ownerNode, false);

		if (_activeEffectHandle is not null)
		{
			ownerNode.EffectsManager.RemoveEffect(_activeEffectHandle);
			_activeEffectHandle = null;
		}

		ownerNode.Attributes["CharacterAttributes.Mana"].OnValueChanged -= Mana_OnValueChanged;

		if (_damageTakenSubscriptionToken is not null)
		{
			ownerNode.Events.Unsubscribe(_damageTakenSubscriptionToken.Value);
			_damageTakenSubscriptionToken = null;
		}
	}

	private void Mana_OnValueChanged(Attributes.EntityAttribute attribute, int change)
	{
		if (attribute.CurrentValue < 10)
		{
			_abilityInstanceHandle?.End();
		}
	}
}
