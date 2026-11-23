// Copyright © Gamesmiths Guild.

using Godot;
using Godot.Collections;

namespace Gamesmiths.Forge.Godot.Resources;

/// <summary>
/// A blast: effects and a push on every entity within a radius, scaled by how close each one is to the center.
/// </summary>
/// <remarks>
/// <para>Each entity's scale is <see cref="Falloff"/> sampled at its distance over <see cref="Radius"/>, or falls
/// linearly to <see cref="EdgeScale"/> without a curve. The effects get it as float context data, which a custom
/// calculator or execution reads to scale its magnitude. The push goes to an entity whose body is moved by a
/// <see cref="Nodes.ForgeCharacterMovement3D"/> or <see cref="Nodes.ForgeCharacterMovement2D"/>, away from the center.
/// </para>
/// <para>On the server, a client's blast meets the other entities where that client was shown them, as its shots do.
/// </para>
/// </remarks>
[Tool]
[GlobalClass]
public partial class ForgeSplashData : Resource
{
	/// <summary>
	/// Gets or sets how far the blast reaches, in units.
	/// </summary>
	[Export(PropertyHint.Range, "0,100,0.01,or_greater")]
	public float Radius { get; set; } = 3;

	/// <summary>
	/// Gets or sets the collision layers the blast finds entities on.
	/// </summary>
	[Export(PropertyHint.Layers3DPhysics)]
	public uint CollisionMask { get; set; } = 1;

	/// <summary>
	/// Gets or sets the curve the scale follows from the center (0) to the edge (1). Without one the scale falls
	/// linearly from 1 to <see cref="EdgeScale"/>.
	/// </summary>
	[Export]
	public Curve? Falloff { get; set; }

	/// <summary>
	/// Gets or sets the scale at the edge, when there's no <see cref="Falloff"/>.
	/// </summary>
	[Export(PropertyHint.Range, "0,1,0.01")]
	public float EdgeScale { get; set; } = 0.25f;

	/// <summary>
	/// Gets or sets the effects applied to each entity in the blast.
	/// </summary>
	[Export]
	public Array<ForgeEffectData> Effects { get; set; } = [];

	/// <summary>
	/// Gets or sets the push at the center, in units per second added to a body's velocity, scaled like the effects.
	/// </summary>
	[Export(PropertyHint.Range, "0,100,0.01,or_greater")]
	public float Knockback { get; set; }

	/// <summary>
	/// Gets or sets how much of the push points up, whatever the direction from the center: 0 pushes straight away, 1
	/// straight up. A little lifts a body off the ground, where friction would stop it.
	/// </summary>
	[Export(PropertyHint.Range, "0,1,0.01")]
	public float KnockbackLift { get; set; } = 0.2f;

	/// <summary>
	/// Gets or sets a value indicating whether the blast reaches the entity that caused it: its effects and its push,
	/// which a rocket jump needs.
	/// </summary>
	[Export]
	public bool AffectsInstigator { get; set; } = true;

	/// <summary>
	/// Gets or sets the scale the instigator's effects get on top of its distance's, so a rocket jump costs less than a
	/// direct hit.
	/// </summary>
	[Export(PropertyHint.Range, "0,1,0.01")]
	public float InstigatorScale { get; set; } = 0.5f;

	/// <summary>
	/// Gets or sets a value indicating whether a wall between the center and an entity shields it.
	/// </summary>
	[Export]
	public bool BlockedByWalls { get; set; } = true;

	/// <summary>
	/// Gives the scale an entity a distance from the center gets.
	/// </summary>
	/// <param name="distance">The distance.</param>
	/// <returns>The scale, from 0 to 1.</returns>
	public float ScaleAt(float distance)
	{
		if (Radius <= 0)
		{
			return 1;
		}

		float t = Mathf.Clamp(distance / Radius, 0, 1);
		return Falloff is null ? Mathf.Lerp(1, EdgeScale, t) : Falloff.Sample(t);
	}
}
