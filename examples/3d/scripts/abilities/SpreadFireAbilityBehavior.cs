// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Abilities;
using Gamesmiths.Forge.Godot.Resources.Abilities;
using Godot;

namespace Gamesmiths.Forge.Example;

[Tool]
[GlobalClass]
public partial class SpreadFireAbilityBehavior : ForgeAbilityBehavior
{
	// Using UID to reference the effect to avoid circular references between abilities and effects.
	[Export]
	public required string FireEffectUID { get; set; }

	public override IAbilityBehavior GetBehavior()
	{
		return new SpreadFireAbilityBehaviorImplementation(FireEffectUID);
	}
}
