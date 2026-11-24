// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Abilities;
using Gamesmiths.Forge.Cues;
using Gamesmiths.Forge.Godot.Nodes;
using Gamesmiths.Forge.Godot.Resources;
using Gamesmiths.Forge.Godot.Resources.Abilities;
using Godot;

namespace Gamesmiths.Forge.Example;

/// <summary>
/// A weapon's trigger: it fires a burst of <see cref="Burst"/> shots as the ability starts, and another every
/// <see cref="RefireSeconds"/> while its action is held or was pressed again, then ends. Its whole run lives in a
/// <see cref="FireContext"/>, so a client predicts it and a correction runs it again exactly.
/// </summary>
[Tool]
[GlobalClass]
public abstract partial class WeaponFire : ForgeAbilityBehavior
{
	/// <summary>
	/// Fires one shot.
	/// </summary>
	/// <param name="owner">The shooter.</param>
	/// <param name="eye">Where the shot leaves from.</param>
	/// <param name="aim">Where it aims.</param>
	protected abstract void Fire(ForgeEntity owner, Vector3 eye, Vector3 aim);

	/// <summary>
	/// Gets or sets the input action that holds the trigger, the same as the ability's.
	/// </summary>
	[Export]
	public string Action { get; set; } = "fire";

	/// <summary>
	/// Gets or sets the time from a burst's last shot to the next burst, in seconds.
	/// </summary>
	[Export]
	public float RefireSeconds { get; set; } = 0.5f;

	/// <summary>
	/// Gets or sets how many shots a pull of the trigger fires.
	/// </summary>
	[Export]
	public int Burst { get; set; } = 1;

	/// <summary>
	/// Gets or sets the time between a burst's shots, in seconds.
	/// </summary>
	[Export]
	public float BurstSeconds { get; set; } = 0.1f;

	/// <summary>
	/// Gets or sets where shots leave from, against the body's origin: the eye of a first-person player.
	/// </summary>
	[Export]
	public Vector3 Eye { get; set; } = new(0, 1.6f, 0);

	/// <summary>
	/// Gets or sets the cue each shot executes on the shooter, as its sound.
	/// </summary>
	[Export]
	public ForgeTag? ShotCue { get; set; }

	/// <summary>
	/// Gets or sets the cue each shot executes on the shooter to kick its view. The shooter's client shows it at once,
	/// and the kicked view aims the burst's next shots, which the server fires where the client's input says.
	/// </summary>
	[Export]
	public ForgeTag? RecoilCue { get; set; }

	/// <inheritdoc/>
	public override IAbilityBehavior GetBehavior()
	{
		return new Behavior(this);
	}

	/// <summary>
	/// Executes a cue on the shooter, which its client shows at once and the server's word shows everyone else.
	/// </summary>
	/// <param name="owner">The shooter.</param>
	/// <param name="cue">The cue, or <see langword="null"/> for none.</param>
	/// <param name="parameters">What the cue is sent with.</param>
	protected static void Execute(ForgeEntity owner, ForgeTag? cue, CueParameters? parameters = null)
	{
		if (cue is not null)
		{
			owner.CuesManager.ExecuteCue(cue.GetTag(), owner, parameters);
		}
	}

	/// <summary>
	/// A trigger's run: the time to its next shot, the shots left in its burst and a press waiting for it.
	/// </summary>
	public sealed class FireContext : BehaviorContext
	{
		/// <summary>
		/// Gets or sets the time to the next shot, in seconds.
		/// </summary>
		public double Wait { get; set; }

		/// <summary>
		/// Gets or sets how many shots the burst has left.
		/// </summary>
		public int BurstLeft { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether the action was pressed again since the last shot.
		/// </summary>
		public bool Queued { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether the run is past the tick it started on, whose press started it.
		/// </summary>
		public bool Begun { get; set; }
	}

	private sealed class Behavior(WeaponFire weapon) : RollbackBehavior<FireContext>
	{
		public override void OnStarted(AbilityBehaviorContext context)
		{
			base.OnStarted(context);
			Context.BurstLeft = weapon.Burst;
			Shoot();
		}

		public override void OnFixedUpdate(double deltaTime)
		{
			var owner = (ForgeEntity)Context.Ability!.Owner;

			if (Context.Begun && owner.Input?.WasPressed(weapon.Action) == true)
			{
				Context.Queued = true;
			}

			Context.Begun = true;
			Context.Wait -= deltaTime;

			if (Context.Wait > 1e-6)
			{
				return;
			}

			if (Context.BurstLeft > 0)
			{
				Shoot();
				return;
			}

			if (Context.Queued || owner.Input?.IsHeld(weapon.Action) == true)
			{
				Context.Queued = false;
				Context.BurstLeft = weapon.Burst;
				Shoot();
				return;
			}

			Context.Ability.InstanceHandle.End();
		}

		public override void OnEnded(AbilityBehaviorContext context)
		{
		}

		private void Shoot()
		{
			var owner = (ForgeEntity)Context.Ability!.Owner;
			var body = (Node3D)owner.GetParent();
			System.Numerics.Vector3 input = owner.Input?.Current.AimDirection ?? default;
			Vector3 aim = input == default ? -body.GlobalBasis.Z : new Vector3(input.X, input.Y, input.Z);

			Execute(owner, weapon.ShotCue);
			Execute(owner, weapon.RecoilCue);
			weapon.Fire(owner, body.GlobalPosition + weapon.Eye, aim.Normalized());
			Context.BurstLeft--;
			Context.Wait += Context.BurstLeft > 0 ? weapon.BurstSeconds : weapon.RefireSeconds;
		}
	}
}
