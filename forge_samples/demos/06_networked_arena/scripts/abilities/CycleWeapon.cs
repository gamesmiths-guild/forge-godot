// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Abilities;
using Gamesmiths.Forge.Godot.Resources.Abilities;
using Godot;
using Godot.Collections;

namespace Gamesmiths.Forge.Example;

/// <summary>
/// Takes out the weapon after or before the one held, as the mouse wheel does: it activates that weapon's ability,
/// which puts the held one away, and ends.
/// </summary>
[Tool]
[GlobalClass]
public partial class CycleWeapon : ForgeAbilityBehavior
{
	/// <summary>
	/// Gets or sets the weapons' abilities, in the order the wheel goes through them.
	/// </summary>
	[Export]
	public Array<ForgeAbilityData> Weapons { get; set; } = [];

	/// <summary>
	/// Gets or sets how many weapons on the wheel moves: 1 for the next, -1 for the one before.
	/// </summary>
	[Export]
	public int Step { get; set; } = 1;

	/// <inheritdoc/>
	public override IAbilityBehavior GetBehavior()
	{
		return new Behavior(this);
	}

	private sealed class Behavior(CycleWeapon cycle) : RollbackBehavior<BehaviorContext>
	{
		public override void OnStarted(AbilityBehaviorContext context)
		{
			base.OnStarted(context);

			int count = cycle.Weapons.Count;
			var handles = new AbilityHandle?[count];
			int held = -1;

			for (int i = 0; i < count; i++)
			{
				context.Owner.Abilities.TryGetAbility(cycle.Weapons[i].GetAbilityData(), out handles[i], context.Owner);

				if (handles[i]?.IsActive == true)
				{
					held = i;
				}
			}

			if (held >= 0)
			{
				handles[(((held + cycle.Step) % count) + count) % count]?.TryActivate(out _);
			}

			context.InstanceHandle.End();
		}

		public override void OnEnded(AbilityBehaviorContext context)
		{
		}
	}
}
