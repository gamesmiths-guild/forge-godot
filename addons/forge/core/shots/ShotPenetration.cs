// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Godot.Core.Shots;

/// <summary>
/// What a shot goes through.
/// </summary>
/// <remarks>
/// A shot goes through a collider on a layer in <see cref="PierceMask"/>, keeping <see cref="PierceScale"/> of its
/// damage, up to <see cref="MaxPierces"/> of them. With <see cref="Power"/> above zero it also goes through a collider
/// with a <see cref="Resources.ForgeShotSurface"/>, losing the surface's resistance times the thickness it crosses, and
/// keeps the share of its power left as the share of its damage.
/// </remarks>
/// <param name="PierceMask">The layers of the colliders it goes through.</param>
/// <param name="MaxPierces">How many colliders it goes through at most. Negative has no limit.</param>
/// <param name="PierceScale">The share of its damage it keeps through each collider of the Pierce Mask.</param>
/// <param name="Power">The power it goes through surfaces with, or zero.</param>
public readonly record struct ShotPenetration(uint PierceMask, int MaxPierces, float PierceScale, float Power)
{
	/// <summary>
	/// Gets the penetration of a shot that goes through nothing.
	/// </summary>
	public static ShotPenetration None => new(0, 0, 1, 0);

	/// <summary>
	/// Gets a value indicating whether the shot may go through anything.
	/// </summary>
	public bool Pierces => MaxPierces != 0 && (PierceMask != 0 || Power > 0);
}
