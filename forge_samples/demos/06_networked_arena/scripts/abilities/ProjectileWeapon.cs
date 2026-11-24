// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Godot.Core.Shots;
using Gamesmiths.Forge.Godot.Nodes;
using Gamesmiths.Forge.Godot.Resources;
using Godot;

namespace Gamesmiths.Forge.Example;

/// <summary>
/// A weapon that launches a lightweight projectile: every machine flies it from its launch, and its impact applies its
/// effects and its blast.
/// </summary>
[Tool]
[GlobalClass]
public partial class ProjectileWeapon : WeaponFire
{
	private const float Skin = 0.1f;

	/// <summary>
	/// Gets or sets the projectile.
	/// </summary>
	[Export]
	public ForgeProjectileData? Projectile { get; set; }

	/// <summary>
	/// Gets or sets how far ahead of the eye the projectile starts, so it clears the shooter's hands.
	/// </summary>
	[Export]
	public float LaunchAhead { get; set; } = 0.3f;

	/// <summary>
	/// Gets or sets the layers a projectile mustn't start inside: it starts short of them instead, so a shot fired
	/// against a wall meets it rather than going through.
	/// </summary>
	[Export(PropertyHint.Layers3DPhysics)]
	public uint WallMask { get; set; } = 1;

	/// <inheritdoc/>
	protected override void Fire(ForgeEntity owner, Vector3 eye, Vector3 aim)
	{
		if (Projectile is null)
		{
			return;
		}

		// A sweep leaves out what it starts inside or touching, so the projectile starts only as far as its sphere is
		// clear, with a skin to spare.
		var query = new PhysicsShapeQueryParameters3D
		{
			Shape = new SphereShape3D { Radius = Projectile.Radius + Skin },
			Transform = new Transform3D(Basis.Identity, eye),
			Motion = aim * LaunchAhead,
			CollisionMask = WallMask,
		};
		float[] clear = ((Node3D)owner.GetParent()).GetWorld3D().DirectSpaceState.CastMotion(query);

		ForgeShots3D.Launch(owner, Projectile, eye + (aim * LaunchAhead * clear[0]), aim);
	}
}
