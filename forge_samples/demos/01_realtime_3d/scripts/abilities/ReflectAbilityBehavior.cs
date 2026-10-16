// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Abilities;
using Gamesmiths.Forge.Godot.Resources;
using Gamesmiths.Forge.Godot.Resources.Abilities;
using Gamesmiths.Forge.Tags;
using Godot;

namespace Gamesmiths.Forge.Example;

[Tool]
[GlobalClass]
public partial class ReflectAbilityBehavior : ForgeAbilityBehavior
{
	[Export]
	public required ForgeEffectData DamageEffect { get; set; }

	public override IAbilityBehavior GetBehavior()
	{
		return new ReflectAbilityBehaviorImplementation(DamageEffect.GetEffectData());
	}

	public override AbilityTriggerData CreateEventTrigger(Tag triggerTag, int priority)
	{
		return AbilityTriggerData.ForEvent<DamageType>(triggerTag, priority);
	}
}
