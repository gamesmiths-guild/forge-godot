// Copyright © Gamesmiths Guild.

using Godot;
using Godot.Collections;

namespace Gamesmiths.Forge.Godot.Resources;

/// <summary>
/// An instant shot: one ray, or pellets spread in a cone, that hits what is on its line at once, tested on the server
/// against the world as its shooter saw it.
/// </summary>
/// <remarks>
/// <para>Pellets spread from the shooter's seeded random, so the server and the client that predicts the shot spread
/// them alike.</para>
/// <para>A ray stops at the first collider in <see cref="CollisionMask"/> it may not go through. It goes through one on
/// a layer in <see cref="PierceMask"/>, up to <see cref="MaxPierces"/>, each costing <see cref="PierceScale"/> of its
/// damage; with <see cref="Power"/> above zero, it goes through one with a <see cref="ForgeShotSurface"/> too, losing
/// the surface's resistance times the thickness it crosses. Each entity on the line gets the effects once, with the
/// scale left as float context data, which a custom calculator or execution reads.</para>
/// </remarks>
[Tool]
[GlobalClass]
public partial class ForgeHitscanData : Resource
{
	/// <summary>
	/// Gets or sets how far the shot reaches, in units.
	/// </summary>
	[Export(PropertyHint.Range, "0,1000,0.1,or_greater")]
	public float Range { get; set; } = 100;

	/// <summary>
	/// Gets or sets the collision layers the shot hits.
	/// </summary>
	[Export(PropertyHint.Layers3DPhysics)]
	public uint CollisionMask { get; set; } = 1;

	/// <summary>
	/// Gets or sets how many rays the shot fires.
	/// </summary>
	[Export(PropertyHint.Range, "1,64,1")]
	public int Pellets { get; set; } = 1;

	/// <summary>
	/// Gets or sets the half angle of the cone the rays spread over, in degrees. Zero fires them all along the aim.
	/// </summary>
	[Export(PropertyHint.Range, "0,45,0.1")]
	public float SpreadDegrees { get; set; }

	/// <summary>
	/// Gets or sets the effects applied to each entity the shot hits.
	/// </summary>
	[Export]
	public Array<ForgeEffectData> Effects { get; set; } = [];

	/// <summary>
	/// Gets or sets a value indicating whether the shot hits lightweight projectiles that can be shot, where its
	/// shooter saw them, and ends them as struck.
	/// </summary>
	[Export]
	public bool HitsProjectiles { get; set; }

	/// <summary>
	/// Gets or sets the layers of the colliders a ray goes through.
	/// </summary>
	[ExportGroup("Penetration")]
	[Export(PropertyHint.Layers3DPhysics)]
	public uint PierceMask { get; set; }

	/// <summary>
	/// Gets or sets how many colliders a ray goes through at most. Negative has no limit.
	/// </summary>
	[Export(PropertyHint.Range, "-1,16,1,or_greater")]
	public int MaxPierces { get; set; } = 1;

	/// <summary>
	/// Gets or sets the share of its damage a ray keeps through each collider it goes through.
	/// </summary>
	[Export(PropertyHint.Range, "0,1,0.01")]
	public float PierceScale { get; set; } = 0.5f;

	/// <summary>
	/// Gets or sets the power a ray goes through colliders with a <see cref="ForgeShotSurface"/> with. Zero leaves them
	/// to the Pierce Mask.
	/// </summary>
	[Export(PropertyHint.Range, "0,1000,0.1,or_greater")]
	public float Power { get; set; }

	/// <summary>
	/// Gets the penetration the shot's rays have.
	/// </summary>
	public Core.Shots.ShotPenetration Penetration => new(PierceMask, MaxPierces, PierceScale, Power);
}
