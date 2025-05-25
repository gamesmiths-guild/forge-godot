// Copyright © Gamesmiths Guild.

using System.Collections.Generic;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Effects;
using Gamesmiths.Forge.Godot.Nodes;
using Godot;

namespace Gamesmiths.Forge.Godot.Core;

internal sealed class EffectApplier
{
	private readonly List<EffectData> _effects = [];

	private readonly Dictionary<IForgeEntity, List<ActiveEffectHandle>> _effectInstances = [];

	public EffectApplier(Node node)
	{
		foreach (Node child in node.GetChildren())
		{
			if (child is ForgeEffect effectNode && effectNode.EffectData is not null)
			{
				_effects.Add(effectNode.EffectData.GetEffectData());
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
		foreach (EffectData effectData in _effects)
		{
			var effect = new Effect(
				effectData,
				new EffectOwnership(effectOwner, effectCauser));

			forgeEntity.EffectsManager.ApplyEffect(effect);
		}
	}

	private void AddEffects(IForgeEntity forgeEntity, IForgeEntity? effectOwner, IForgeEntity? effectCauser)
	{
		var instanceEffects = new List<ActiveEffectHandle>();
		if (!_effectInstances.TryAdd(forgeEntity, instanceEffects))
		{
			instanceEffects = _effectInstances[forgeEntity];
		}

		foreach (EffectData effectData in _effects)
		{
			var effect = new Effect(
				effectData,
				new EffectOwnership(effectOwner, effectCauser));

			ActiveEffectHandle? handle = forgeEntity.EffectsManager.ApplyEffect(effect);

			if (handle is null)
			{
				continue;
			}

			instanceEffects.Add(handle);
		}
	}

	private void RemoveEffects(IForgeEntity forgeEntity)
	{
		if (!_effectInstances.TryGetValue(forgeEntity, out List<ActiveEffectHandle>? value))
		{
			return;
		}

		foreach (ActiveEffectHandle handle in value)
		{
			forgeEntity.EffectsManager.UnapplyEffect(handle);
		}

		_effectInstances[forgeEntity] = [];
	}
}
