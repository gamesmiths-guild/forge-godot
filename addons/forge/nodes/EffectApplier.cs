// Copyright © 2025 Gamesmiths Guild.

using System.Collections.Generic;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.GameplayEffects;
using Godot;

using ForgeGameplayEffect = Gamesmiths.Forge.GameplayEffects.GameplayEffect;
using ForgeGameplayEffectData = Gamesmiths.Forge.GameplayEffects.GameplayEffectData;
using GameplayEffect = Gamesmiths.Forge.GameplayEffects.Godot.GameplayEffect;

namespace Gamesmiths.Forge.Nodes;

internal class EffectApplier
{
	private readonly List<ForgeGameplayEffectData> _effects = [];

	private readonly Dictionary<IForgeEntity, List<ActiveGameplayEffectHandle>> _effectInstances = [];

	public EffectApplier(Node node)
	{
		foreach (Node child in node.GetChildren())
		{
			if (child is GameplayEffect effectNode && effectNode.GameplayEffectData is not null)
			{
				_effects.Add(effectNode.GameplayEffectData.GetEffectData());
			}
		}
	}

	public void ApplyEffects(Node node, IForgeEntity? effectOwner)
	{
		if (node is IForgeEntity forgeEntity)
		{
			ApplyEffects(forgeEntity, effectOwner, effectOwner);
			return;
		}

		foreach (Node? child in node.GetChildren())
		{
			if (child is IForgeEntity forgeEntityChild)
			{
				ApplyEffects(forgeEntityChild, effectOwner, effectOwner);
				return;
			}
		}
	}

	public void AddEffects(Node node, IForgeEntity? effectOwner)
	{
		if (node is IForgeEntity forgeEntity)
		{
			AddEffects(forgeEntity, effectOwner, effectOwner);
			return;
		}

		foreach (Node? child in node.GetChildren())
		{
			if (child is IForgeEntity forgeEntityChild)
			{
				AddEffects(forgeEntityChild, effectOwner, effectOwner);
				return;
			}
		}
	}

	public void RemoveEffects(Node node)
	{
		if (node is IForgeEntity forgeEntity)
		{
			RemoveEffects(forgeEntity);
			return;
		}

		foreach (Node? child in node.GetChildren())
		{
			if (child is IForgeEntity forgeEntityChild)
			{
				RemoveEffects(forgeEntityChild);
				return;
			}
		}
	}

	private void ApplyEffects(IForgeEntity forgeEntity, IForgeEntity? effectOwner, IForgeEntity? effectCauser)
	{
		foreach (ForgeGameplayEffectData effectData in _effects)
		{
			var effect = new ForgeGameplayEffect(
				effectData,
				new GameplayEffectOwnership(effectOwner, effectCauser));

			forgeEntity.EffectsManager.ApplyEffect(effect);
		}
	}

	private void AddEffects(IForgeEntity forgeEntity, IForgeEntity? effectOwner, IForgeEntity? effectCauser)
	{
		var instanceEffects = new List<ActiveGameplayEffectHandle>();
		if (!_effectInstances.TryAdd(forgeEntity, instanceEffects))
		{
			instanceEffects = _effectInstances[forgeEntity];
		}

		foreach (ForgeGameplayEffectData effectData in _effects)
		{
			var effect = new ForgeGameplayEffect(
				effectData,
				new GameplayEffectOwnership(effectOwner, effectCauser));

			ActiveGameplayEffectHandle? handle = forgeEntity.EffectsManager.ApplyEffect(effect);

			if (handle is null)
			{
				continue;
			}

			instanceEffects.Add(handle);
		}
	}

	private void RemoveEffects(IForgeEntity forgeEntity)
	{
		if (!_effectInstances.TryGetValue(forgeEntity, out List<ActiveGameplayEffectHandle>? value))
		{
			return;
		}

		foreach (ActiveGameplayEffectHandle handle in value)
		{
			forgeEntity.EffectsManager.UnapplyEffect(handle);
		}

		_effectInstances[forgeEntity] = [];
	}
}
