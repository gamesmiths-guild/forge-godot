// Copyright © 2025 Gamesmiths Guild.

using System.Collections.Generic;
using Gamesmiths.Forge.Core.Godot;
using Gamesmiths.Forge.GameplayEffects;
using Godot;
using ForgeGameplayEffect = Gamesmiths.Forge.GameplayEffects.GameplayEffect;
using ForgeGameplayEffectData = Gamesmiths.Forge.GameplayEffects.GameplayEffectData;
using GameplayEffect = Gamesmiths.Forge.GameplayEffects.Godot.GameplayEffect;

public partial class EffectArea3d : Area3D
{
	private readonly List<ForgeGameplayEffectData> _effects = [];

	public override void _Ready()
	{
		base._Ready();

		foreach (Node node in GetChildren())
		{
			if (node is GameplayEffect effectNode && effectNode.GameplayEffectData is not null)
			{
				_effects.Add(effectNode.GameplayEffectData.GetEffectData());
			}
		}

		BodyEntered += EffectArea3d_BodyEntered;
		AreaEntered += EffectArea3d_AreaEntered;
	}

	private void EffectArea3d_AreaEntered(Area3D area)
	{
		GD.Print($"{area.Name} area entered.");
	}

	private void EffectArea3d_BodyEntered(Node3D body)
	{
		GD.Print($"{body.Name} body entered.");

		foreach (Node? child in (Godot.Collections.Array<Node>)body.GetChildren())
		{
			GD.Print($"{child.Name} body entered.");

			if (child is ForgeEntity forgeEntity)
			{
				GD.Print("Is forge entity.");

				foreach (ForgeGameplayEffectData effectData in _effects)
				{
					GD.Print($"{effectData.Name} effect.");

					var effect = new ForgeGameplayEffect(
						effectData,
						new GameplayEffectOwnership(forgeEntity, forgeEntity));

					forgeEntity.EffectsManager.ApplyEffect(effect);
				}
			}
		}
	}
}
