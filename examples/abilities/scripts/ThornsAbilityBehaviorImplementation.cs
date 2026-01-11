// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Abilities;
using Gamesmiths.Forge.Effects;
using Gamesmiths.Forge.Godot.Core;
using Gamesmiths.Forge.Godot.Nodes;
using Gamesmiths.Forge.Tags;
using Godot;

namespace Gamesmiths.Forge.Example;

public sealed class ThornsAbilityBehaviorImplementation : IAbilityBehavior<DamageType>
{
	private readonly EffectData _effectData;
	private Area3D? _thornsArea;
	private Effect? _damageEffect;

	public ThornsAbilityBehaviorImplementation(EffectData damageEffect)
	{
		_effectData = damageEffect;
	}

	public void OnStarted(AbilityBehaviorContext context, DamageType data)
	{
		if (context.Owner is not ForgeEntity ownerNode || data == DamageType.Magical)
		{
			return;
		}

		_thornsArea ??= ownerNode.GetNode<Area3D>("%ThornsArea");

		context.AbilityHandle.CommitAbility();

		foreach (Node3D? body in _thornsArea.GetOverlappingBodies())
		{
			ForgeEntity? forgeEntity = body.GetNodeOrNull<ForgeEntity>("%Forge Entity");
			if (forgeEntity is null || forgeEntity == context.Owner)
			{
				continue;
			}

			_damageEffect ??= new Effect(_effectData, new EffectOwnership(context.Owner, context.Source));

			_damageEffect.SetSetByCallerMagnitude(
				Tag.RequestTag(ForgeManagers.Instance.TagsManager, "set_by_caller.damage"),
				context.Magnitude * 2);

			forgeEntity.EffectsManager.ApplyEffect(_damageEffect);
		}

		ForgeManagers.Instance.CuesManager.ExecuteCue(
			Tag.RequestTag(ForgeManagers.Instance.TagsManager, "cue.vfx.thorns"),
			ownerNode,
			null);

		context.InstanceHandle.End();
	}

	public void OnEnded(AbilityBehaviorContext context)
	{
	}
}
