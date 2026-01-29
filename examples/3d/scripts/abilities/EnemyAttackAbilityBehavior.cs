// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Abilities;
using Gamesmiths.Forge.Godot.Resources;
using Gamesmiths.Forge.Godot.Resources.Abilities;
using Godot;

namespace Gamesmiths.Forge.Example;

[Tool]
[GlobalClass]
public partial class EnemyAttackAbilityBehavior : ForgeAbilityBehavior
{
	[Export]
	public required ForgeEffectData DamageEffect { get; set; }

	public override IAbilityBehavior GetBehavior()
	{
		return new EnemyAttackBehaviorImplementation(DamageEffect.GetEffectData());
	}
}
