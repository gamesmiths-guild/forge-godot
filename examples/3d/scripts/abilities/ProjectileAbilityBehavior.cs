// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Abilities;
using Gamesmiths.Forge.Godot.Resources.Abilities;
using Godot;

namespace Gamesmiths.Forge.Example;

[Tool]
[GlobalClass]
public partial class ProjectileAbilityBehavior : ForgeAbilityBehavior
{
	[Export]
	public required PackedScene Projectile { get; set; }

	public override IAbilityBehavior GetBehavior()
	{
		return new ProjectileAbilityBehaviorImplementation(Projectile);
	}
}
