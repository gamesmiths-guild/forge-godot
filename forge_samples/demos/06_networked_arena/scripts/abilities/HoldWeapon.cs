// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Abilities;
using Gamesmiths.Forge.Godot.Resources.Abilities;
using Godot;

namespace Gamesmiths.Forge.Example;

/// <summary>
/// Holds a weapon: a run that never ends by itself, so the ability's activation owned tag says which weapon the
/// player holds for as long as it runs. Taking out another weapon cancels it, through that ability's Cancel Abilities
/// With Tag.
/// </summary>
[Tool]
[GlobalClass]
public partial class HoldWeapon : ForgeAbilityBehavior
{
	/// <inheritdoc/>
	public override IAbilityBehavior GetBehavior()
	{
		return new Behavior();
	}

	private sealed class Behavior : RollbackBehavior<BehaviorContext>
	{
		public override void OnEnded(AbilityBehaviorContext context)
		{
		}
	}
}
