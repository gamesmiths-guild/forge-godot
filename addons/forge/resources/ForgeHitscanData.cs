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
/// a layer in <see cref="PierceMask"/>, up to <see cref="MaxPierces"/>, each keeping <see cref="PierceScale"/> of its
/// strength; with <see cref="Power"/> above zero, it goes through one with a <see cref="ForgeShotSurface"/> too, losing
/// the surface's resistance times the thickness it crosses. Each entity on the line gets the effects once, with the
/// strength left, times <see cref="Falloff"/> at the distance it was hit at, as float context data, which a custom
/// calculator or execution reads.</para>
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
	/// Gets or sets how the strength falls off with distance from where the shot starts: sampled at the distance, in
	/// units over the curve's domain, holding its last value past the end. Without one, distance changes nothing.
	/// </summary>
	[Export]
	public Curve? Falloff { get; set; }

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
	/// Gets or sets the share of its strength a ray keeps through each collider it goes through.
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

	/// <summary>
	/// Gives the share of its strength a hit keeps at a distance from where the shot started.
	/// </summary>
	/// <param name="distance">The distance, in units.</param>
	/// <returns><see cref="Falloff"/> at the distance, or 1 without one.</returns>
	public float FalloffAt(float distance)
	{
		return Falloff?.Sample(distance) ?? 1;
	}
}
