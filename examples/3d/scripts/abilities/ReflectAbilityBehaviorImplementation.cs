// Copyright © Gamesmiths Guild.

using System;
using Gamesmiths.Forge.Abilities;
using Gamesmiths.Forge.Effects;
using Gamesmiths.Forge.Godot.Core;
using Gamesmiths.Forge.Godot.Nodes;
using Gamesmiths.Forge.Tags;
using Godot;

namespace Gamesmiths.Forge.Example;

public sealed class ReflectAbilityBehaviorImplementation : IAbilityBehavior<DamageType>
{
	private readonly EffectData _effectData;
	private Area3D? _reflectArea;
	private Effect? _damageEffect;

	public ReflectAbilityBehaviorImplementation(EffectData damageEffect)
	{
		_effectData = damageEffect;
	}

	public void OnStarted(AbilityBehaviorContext context, DamageType data)
	{
		if (context.Owner is not ForgeEntity ownerNode || data != DamageType.Physical)
		{
			context.InstanceHandle.End();
			return;
		}

		_reflectArea ??= ownerNode.GetNode<Area3D>("%ReflectArea");

		context.AbilityHandle.CommitAbility();

		foreach (Node3D? body in _reflectArea.GetOverlappingBodies())
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

			var distance = ownerNode.GetParent<Node3D>().GlobalTransform.Origin.DistanceTo(body.GlobalTransform.Origin);
			distance--;
			var damageFalloffMultiplier = Math.Clamp(1f - (distance / 3.5f), 0.1f, 1f);

			forgeEntity.EffectsManager.ApplyEffect(_damageEffect, damageFalloffMultiplier);
		}

		ForgeManagers.Instance.CuesManager.ExecuteCue(
			Tag.RequestTag(ForgeManagers.Instance.TagsManager, "cue.vfx.reflect"),
			ownerNode,
			null);

		context.InstanceHandle.End();
	}

	public void OnEnded(AbilityBehaviorContext context)
	{
	}
}
