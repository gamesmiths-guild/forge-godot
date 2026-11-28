// Copyright © Gamesmiths Guild.

using System.Linq;
using Gamesmiths.Forge.Abilities;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Cues;
using Gamesmiths.Forge.Effects;
using Gamesmiths.Forge.Godot.Resources;
using Gamesmiths.Forge.Godot.Resources.Abilities;
using Gamesmiths.Forge.Tags;
using Godot;
using Godot.Collections;

namespace Gamesmiths.Forge.Example;

/// <summary>
/// What a lethal hit sets off, answering the <see cref="Frag"/> event the arena's damage raises: the frag's cue with
/// who dealt it, a death for the fragged and a kill for the fragger, a wait out of the fight, then a respawn at full
/// health. The ability's activation owned tags keep the entity dead for the run.
/// </summary>
/// <remarks>
/// The ability is Server Only: a frag is the server's to call, and every client hears of it through the cue, the
/// tags and the attributes.
/// </remarks>
[Tool]
[GlobalClass]
public partial class Die : ForgeAbilityBehavior
{
	/// <summary>
	/// Gets or sets how long the entity is out of the fight, in seconds.
	/// </summary>
	[Export]
	public float RespawnSeconds { get; set; } = 2.5f;

	/// <summary>
	/// Gets or sets a value indicating whether the entity waits in <see cref="Limbo"/> and respawns at the spawn point
	/// furthest from the other players, rather than where it fell.
	/// </summary>
	[Export]
	public bool Relocate { get; set; } = true;

	/// <summary>
	/// Gets or sets where a relocated entity waits, out of everyone's way.
	/// </summary>
	[Export]
	public Vector3 Limbo { get; set; } = new(0, -40, 0);

	/// <summary>
	/// Gets or sets the cue executed on the fragged entity where it fell, with the fragger as its source and the
	/// fragger's weapon, counted from 1, as its magnitude.
	/// </summary>
	[Export]
	public ForgeTag? FragCue { get; set; }

	/// <summary>
	/// Gets or sets the weapons' abilities, whose order numbers the weapon the cue names.
	/// </summary>
	[Export]
	public Array<ForgeAbilityData> Weapons { get; set; } = [];

	/// <summary>
	/// Gets or sets the effect that counts a death on the fragged entity.
	/// </summary>
	[Export]
	public ForgeEffectData? DeathEffect { get; set; }

	/// <summary>
	/// Gets or sets the effect that counts a kill for the fragger, when it's another player.
	/// </summary>
	[Export]
	public ForgeEffectData? FragEffect { get; set; }

	/// <summary>
	/// Gets or sets the effect that brings the entity back to full health.
	/// </summary>
	[Export]
	public ForgeEffectData? RespawnEffect { get; set; }

	/// <inheritdoc/>
	public override IAbilityBehavior GetBehavior()
	{
		return new Behavior(this);
	}

	/// <inheritdoc/>
	public override AbilityTriggerData CreateEventTrigger(Tag triggerTag, int priority)
	{
		return AbilityTriggerData.ForEvent<Frag>(triggerTag, priority);
	}

	private static void Apply(ForgeEffectData? data, IForgeEntity target, IForgeEntity owner)
	{
		if (data is not null)
		{
			target.EffectsManager.ApplyEffect(new Effect(data.GetEffectData(), new EffectOwnership(owner, owner)));
		}
	}

	/// <summary>
	/// A death's run: the time left out of the fight.
	/// </summary>
	public sealed class DieContext : BehaviorContext
	{
		/// <summary>
		/// Gets or sets the time to the respawn, in seconds.
		/// </summary>
		public double Wait { get; set; }
	}

	private sealed class Behavior(Die die) : RollbackBehavior<DieContext>, IAbilityBehavior<Frag>
	{
		public void OnStarted(AbilityBehaviorContext context, Frag data)
		{
			OnStarted(context);

			IForgeEntity owner = context.Owner;
			var body = (CharacterBody3D)((Node)owner).GetParent();
			int weapon = 0;

			for (int i = 0; data.Fragger is not null && i < die.Weapons.Count; i++)
			{
				if (data.Fragger.Abilities.TryGetAbility(die.Weapons[i].GetAbilityData(), out AbilityHandle? handle)
					&& handle.IsActive)
				{
					weapon = i + 1;
				}
			}

			Vector3 fell = body.GlobalPosition + Vector3.Up;

			if (die.FragCue is not null)
			{
				owner.CuesManager.ExecuteCue(
					die.FragCue.GetTag(),
					owner,
					new CueParameters(weapon, 0, data.Fragger, new System.Numerics.Vector3(fell.X, fell.Y, fell.Z)));
			}

			Apply(die.DeathEffect, owner, owner);

			if (data.Fragger is not null && data.Fragger != owner && IsPlayer(data.Fragger))
			{
				Apply(die.FragEffect, data.Fragger, data.Fragger);
			}

			if (die.Relocate)
			{
				body.GlobalPosition = die.Limbo;
				body.Velocity = Vector3.Zero;
			}

			Context.Wait = die.RespawnSeconds;
		}

		public override void OnFixedUpdate(double deltaTime)
		{
			Context.Wait -= deltaTime;

			if (Context.Wait > 1e-6)
			{
				return;
			}

			IForgeEntity owner = Context.Ability!.Owner;
			var body = (CharacterBody3D)((Node)owner).GetParent();

			if (die.Relocate)
			{
				body.GlobalPosition = FurthestSpawn(body);
				body.Velocity = Vector3.Zero;
			}

			Apply(die.RespawnEffect, owner, owner);
			Context.Ability.InstanceHandle.End();
		}

		public override void OnEnded(AbilityBehaviorContext context)
		{
		}

		private static bool IsPlayer(IForgeEntity entity)
		{
			return entity is Node node && node.IsInsideTree() && node.GetParent().IsInGroup(ArenaGame.PlayersGroup);
		}

		// The spawn point whose nearest other player is furthest from it, so nobody respawns in someone's sights.
		private static Vector3 FurthestSpawn(Node3D body)
		{
			Vector3[] others = [.. body.GetTree().GetNodesInGroup(ArenaGame.PlayersGroup)
				.OfType<Node3D>()
				.Where(x => x != body)
				.Select(x => x.GlobalPosition)];

			return body.GetTree().GetNodesInGroup(ArenaGame.SpawnsGroup)
				.OfType<Node3D>()
				.Select(x => x.GlobalPosition)
				.DefaultIfEmpty(Vector3.Zero)
				.MaxBy(x => others.Select(y => y.DistanceTo(x)).DefaultIfEmpty(float.MaxValue).Min());
		}
	}
}
