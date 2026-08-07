// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Abilities;
using Gamesmiths.Forge.Effects;

namespace Gamesmiths.Forge.Example;

public sealed class EnemyAttackBehaviorImplementation : IAbilityBehavior
{
	private readonly EffectData _effectData;
	private Effect? _damageEffect;

	public EnemyAttackBehaviorImplementation(EffectData damageEffect)
	{
		_effectData = damageEffect;
	}

	public void OnStarted(AbilityBehaviorContext context)
	{
		_damageEffect ??= new Effect(_effectData, new EffectOwnership(context.Owner, context.Source));

		context.AbilityHandle.CommitCooldown();

		context.Target!.EffectsManager.ApplyEffect(_damageEffect);

		context.InstanceHandle.End();
	}

	public void OnEnded(AbilityBehaviorContext context)
	{
	}
}
