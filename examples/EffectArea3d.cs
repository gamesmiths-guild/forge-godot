// Copyright © 2025 Gamesmiths Guild.

using System.Collections.Generic;
using System.Diagnostics;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.GameplayEffects;
using Godot;
using ForgeGameplayEffect = Gamesmiths.Forge.GameplayEffects.GameplayEffect;
using ForgeGameplayEffectData = Gamesmiths.Forge.GameplayEffects.GameplayEffectData;
using GameplayEffect = Gamesmiths.Forge.GameplayEffects.Godot.GameplayEffect;

namespace Gamesmiths.Forge.Example;

public enum EffectTriggerMode
{
	/// <summary>
	/// Add effects when entering the area.
	/// </summary>
	OnEnter = 0,

	/// <summary>
	/// Add effects when exiting the area.
	/// </summary>
	OnExit = 1,

	/// <summary>
	/// Add effects when entering the area and removes when exiting it.
	/// </summary>
	OnStay = 2,
}

public partial class EffectArea3D : Area3D
{
	private readonly List<ForgeGameplayEffectData> _effects = [];

	private readonly Dictionary<IForgeEntity, List<ActiveGameplayEffectHandle>> _effectInstances = [];

	[Export]
	public Node? AreaOwner { get; set; }

	[Export]
	public EffectTriggerMode TriggerMode { get; set; }

	private IForgeEntity? ForgeEntity => AreaOwner as IForgeEntity;

	public override void _Ready()
	{
		if (AreaOwner is not null && AreaOwner is not IForgeEntity)
		{
			GD.PushError($"{nameof(AreaOwner)} must implement {nameof(IForgeEntity)}.");
		}

		base._Ready();

		foreach (Node node in GetChildren())
		{
			if (node is GameplayEffect effectNode && effectNode.GameplayEffectData is not null)
			{
				_effects.Add(effectNode.GameplayEffectData.GetEffectData());
			}
		}

		switch (TriggerMode)
		{
			case EffectTriggerMode.OnEnter:
				BodyEntered += ApplyEffects;
				AreaEntered += ApplyEffects;
				break;

			case EffectTriggerMode.OnExit:
				BodyExited += ApplyEffects;
				AreaExited += ApplyEffects;
				break;

			case EffectTriggerMode.OnStay:
				BodyEntered += AddEffects;
				AreaEntered += AddEffects;
				BodyExited += RemoveEffects;
				AreaExited += RemoveEffects;
				break;
		}
	}

	private void ApplyEffects(Node3D node)
	{
		foreach (Node? child in node.GetChildren())
		{
			if (child is IForgeEntity forgeEntity)
			{
				AreaOwner ??= child;

				Debug.Assert(ForgeEntity is not null, $"{nameof(ForgeEntity)} should never be null here.");

				foreach (ForgeGameplayEffectData effectData in _effects)
				{
					var effect = new ForgeGameplayEffect(
						effectData,
						new GameplayEffectOwnership(ForgeEntity, ForgeEntity));

					forgeEntity.EffectsManager.ApplyEffect(effect);
				}
			}
		}
	}

	private void AddEffects(Node3D node)
	{
		foreach (Node? child in node.GetChildren())
		{
			if (child is IForgeEntity forgeEntity)
			{
				AreaOwner ??= child;

				Debug.Assert(ForgeEntity is not null, $"{nameof(ForgeEntity)} should never be null here.");

				var instanceEffects = new List<ActiveGameplayEffectHandle>();
				if (!_effectInstances.TryAdd(forgeEntity, instanceEffects))
				{
					instanceEffects = _effectInstances[forgeEntity];
				}

				foreach (ForgeGameplayEffectData effectData in _effects)
				{
					var effect = new ForgeGameplayEffect(
						effectData,
						new GameplayEffectOwnership(ForgeEntity, ForgeEntity));

					ActiveGameplayEffectHandle? handle = forgeEntity.EffectsManager.ApplyEffect(effect);

					if (handle is null)
					{
						continue;
					}

					instanceEffects.Add(handle);
				}
			}
		}
	}

	private void RemoveEffects(Node3D node)
	{
		foreach (Node? child in node.GetChildren())
		{
			if (child is IForgeEntity forgeEntity)
			{
				if (!_effectInstances.ContainsKey(forgeEntity))
				{
					continue;
				}

				foreach (ActiveGameplayEffectHandle handle in _effectInstances[forgeEntity])
				{
					forgeEntity.EffectsManager.UnapplyEffect(handle);
				}

				_effectInstances[forgeEntity] = [];
			}
		}
	}
}
