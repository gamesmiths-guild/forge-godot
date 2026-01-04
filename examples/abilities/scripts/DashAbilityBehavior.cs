// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Abilities;
using Gamesmiths.Forge.Godot.Resources.Abilities;
using Godot;

namespace Gamesmiths.Forge.Example;

[Tool]
[GlobalClass]
public partial class DashAbilityBehavior : ForgeAbilityBehavior
{
	public override IAbilityBehavior GetBehavior()
	{
		return new DashAbilityBehaviorImplementation();
	}
}
