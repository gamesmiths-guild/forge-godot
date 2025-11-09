// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Abilities;
using Godot;

namespace Gamesmiths.Forge.Godot.Resources.Abilities;

[Tool]
[GlobalClass]
[Icon("uid://bcx7anhepqfmd")]
public abstract partial class ForgeAbilityBehavior : Resource
{
	public abstract IAbilityBehavior GetBehavior();
}
