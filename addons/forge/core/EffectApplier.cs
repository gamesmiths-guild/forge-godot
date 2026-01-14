// Copyright © Gamesmiths Guild.

using System.Collections.Generic;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Effects;
using Gamesmiths.Forge.Godot.Nodes;
using Godot;

namespace Gamesmiths.Forge.Godot.Core;

internal sealed class EffectApplier
{
	private record struct EffectKey(EffectData EffectData, EffectOwnership EffectOwnership, int Level);

	private readonly List<EffectData> _effects = [];

	private readonly Dictionary<IForgeEntity, List<ActiveEffectHandle>> _effectInstances = [];
	private readonly Dictionary<EffectKey, Effect> _effectsCache = [];

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

	public void ApplyEffects(Node node, IForgeEntity? effectOwner, IForgeEntity? effectSource, int level = 1)
	{
		if (node is IForgeEntity forgeEntity)
		{
			ApplyEffects(forgeEntity, effectOwner, effectSource, level);
			return;
		}

		foreach (Node? child in node.GetChildren())
		{
			if (child is IForgeEntity forgeEntityChild)
			{
				ApplyEffects(forgeEntityChild, effectOwner, effectSource, level);
				return;
			}
		}
	}

	public void ApplyEffects<TData>(
		Node node,
		TData contextData,
		IForgeEntity? effectOwner,
		IForgeEntity? effectSource,
		int level = 1)
	{
		if (node is IForgeEntity forgeEntity)
		{
			ApplyEffects(forgeEntity, contextData, effectOwner, effectSource, level);
			return;
		}

		foreach (Node? child in node.GetChildren())
		{
			if (child is IForgeEntity forgeEntityChild)
			{
				ApplyEffects(forgeEntityChild, contextData, effectOwner, effectSource, level);
				return;
			}
		}
	}

	public void AddEffects(Node node, IForgeEntity? effectOwner, IForgeEntity? effectSource, int level)
	{
		if (node is IForgeEntity forgeEntity)
		{
			AddEffects(forgeEntity, effectOwner, effectSource, level);
			return;
		}

		foreach (Node? child in node.GetChildren())
		{
			if (child is IForgeEntity forgeEntityChild)
			{
				AddEffects(forgeEntityChild, effectOwner, effectSource, level);
				return;
			}
		}
	}

	public void AddEffects<TData>(
		Node node,
		TData contextData,
		IForgeEntity? effectOwner,
		IForgeEntity? effectSource,
		int level)
	{
		if (node is IForgeEntity forgeEntity)
		{
			AddEffects(forgeEntity, contextData, effectOwner, effectSource, level);
			return;
		}

		foreach (Node? child in node.GetChildren())
		{
			if (child is IForgeEntity forgeEntityChild)
			{
				AddEffects(forgeEntityChild, contextData, effectOwner, effectSource, level);
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

	private void ApplyEffects(
		IForgeEntity forgeEntity,
		IForgeEntity? effectOwner,
		IForgeEntity? effectSource,
		int level)
	{
		var effectOwnership = new EffectOwnership(effectOwner, effectSource);

		foreach (EffectData effectData in _effects)
		{
			var key = new EffectKey(effectData, effectOwnership, level);

			if (_effectsCache.TryGetValue(key, out Effect? cachedEffect))
			{
				forgeEntity.EffectsManager.ApplyEffect(cachedEffect);
				continue;
			}

			var effect = new Effect(
				effectData,
				new EffectOwnership(effectOwner, effectSource),
				level);

			_effectsCache[key] = effect;

			forgeEntity.EffectsManager.ApplyEffect(effect);
		}
	}

	private void ApplyEffects<TData>(
		IForgeEntity forgeEntity,
		TData contextData,
		IForgeEntity? effectOwner,
		IForgeEntity? effectSource,
		int level)
	{
		var effectOwnership = new EffectOwnership(effectOwner, effectSource);

		foreach (EffectData effectData in _effects)
		{
			var key = new EffectKey(effectData, effectOwnership, level);

			if (_effectsCache.TryGetValue(key, out Effect? cachedEffect))
			{
				forgeEntity.EffectsManager.ApplyEffect(cachedEffect);
				continue;
			}

			var effect = new Effect(
				effectData,
				new EffectOwnership(effectOwner, effectSource),
				level);

			_effectsCache[key] = effect;

			forgeEntity.EffectsManager.ApplyEffect(effect, contextData);
		}
	}

	private void AddEffects(IForgeEntity forgeEntity, IForgeEntity? effectOwner, IForgeEntity? effectSource, int level)
	{
		var instanceEffects = new List<ActiveEffectHandle>();
		if (!_effectInstances.TryAdd(forgeEntity, instanceEffects))
		{
			instanceEffects = _effectInstances[forgeEntity];
		}

		var effectOwnership = new EffectOwnership(effectOwner, effectSource);

		foreach (EffectData effectData in _effects)
		{
			var key = new EffectKey(effectData, effectOwnership, level);

			ActiveEffectHandle? handle;
			if (_effectsCache.TryGetValue(key, out Effect? cachedEffect))
			{
				handle = forgeEntity.EffectsManager.ApplyEffect(cachedEffect);

				if (handle is null)
				{
					continue;
				}

				instanceEffects.Add(handle);

				continue;
			}

			var effect = new Effect(
				effectData,
				new EffectOwnership(effectOwner, effectSource),
				level);

			handle = forgeEntity.EffectsManager.ApplyEffect(effect);

			if (handle is null)
			{
				continue;
			}

			instanceEffects.Add(handle);
		}
	}

	private void AddEffects<TData>(
		IForgeEntity forgeEntity,
		TData contextData,
		IForgeEntity? effectOwner,
		IForgeEntity? effectSource,
		int level)
	{
		var instanceEffects = new List<ActiveEffectHandle>();
		if (!_effectInstances.TryAdd(forgeEntity, instanceEffects))
		{
			instanceEffects = _effectInstances[forgeEntity];
		}

		var effectOwnership = new EffectOwnership(effectOwner, effectSource);

		foreach (EffectData effectData in _effects)
		{
			var key = new EffectKey(effectData, effectOwnership, level);

			ActiveEffectHandle? handle;
			if (_effectsCache.TryGetValue(key, out Effect? cachedEffect))
			{
				handle = forgeEntity.EffectsManager.ApplyEffect(cachedEffect);

				if (handle is null)
				{
					continue;
				}

				instanceEffects.Add(handle);

				continue;
			}

			var effect = new Effect(
				effectData,
				new EffectOwnership(effectOwner, effectSource),
				level);

			handle = forgeEntity.EffectsManager.ApplyEffect(effect, contextData);

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
			forgeEntity.EffectsManager.RemoveEffect(handle);
		}

		_effectInstances[forgeEntity] = [];
	}
}
