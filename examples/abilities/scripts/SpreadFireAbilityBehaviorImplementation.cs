// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Abilities;
using Gamesmiths.Forge.Effects;
using Gamesmiths.Forge.Godot.Resources;
using Godot;

namespace Gamesmiths.Forge.Example;

public sealed class SpreadFireAbilityBehaviorImplementation : IAbilityBehavior
{
	private readonly string _fireEffectDataUID;
	private Effect? _fireEffect;

	public SpreadFireAbilityBehaviorImplementation(string fireEffectDataUID)
	{
		_fireEffectDataUID = fireEffectDataUID;
	}

	public void OnStarted(AbilityBehaviorContext context)
	{
		if (_fireEffect is null)
		{
			ForgeEffectData res = ResourceLoader.Load<ForgeEffectData>(_fireEffectDataUID);
			_fireEffect = new Effect(res.GetEffectData(), new EffectOwnership(context.Owner, context.Source));
		}

		context.Target?.EffectsManager.ApplyEffect(_fireEffect);
	}

	public void OnEnded(AbilityBehaviorContext context)
	{
	}
}
