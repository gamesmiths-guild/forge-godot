// Copyright © Gamesmiths Guild.

using System;
using Gamesmiths.Forge.Cues;
using Gamesmiths.Forge.Godot.Core.Shots;
using Gamesmiths.Forge.Godot.Nodes;
using Gamesmiths.Forge.Godot.Resources;
using Godot;

namespace Gamesmiths.Forge.Example;

/// <summary>
/// A hitscan weapon: a beam, or pellets spread in a cone from the shooter's seeded random, each one hitscan that
/// applies its effects to what it hits. Each pellet executes a tracer cue to where it stopped, and an impact cue on
/// the surface it stopped on, both carrying the point, so every screen draws them where the server's hit.
/// </summary>
[Tool]
[GlobalClass]
public partial class HitscanWeapon : WeaponFire
{
	/// <summary>
	/// Gets or sets the hitscan each pellet fires, one ray with no spread of its own.
	/// </summary>
	[Export]
	public ForgeHitscanData? Shot { get; set; }

	/// <summary>
	/// Gets or sets how many pellets a shot fires.
	/// </summary>
	[Export]
	public int Pellets { get; set; } = 1;

	/// <summary>
	/// Gets or sets the half angle of the cone the pellets spread over, in degrees.
	/// </summary>
	[Export]
	public float SpreadDegrees { get; set; }

	/// <summary>
	/// Gets or sets the cue drawn from the shooter to where a pellet stopped.
	/// </summary>
	[Export]
	public ForgeTag? TracerCue { get; set; }

	/// <summary>
	/// Gets or sets the cue shown where a pellet hit a surface, facing it.
	/// </summary>
	[Export]
	public ForgeTag? ImpactCue { get; set; }

	/// <inheritdoc/>
	protected override void Fire(ForgeEntity owner, Vector3 eye, Vector3 aim)
	{
		if (Shot is null)
		{
			return;
		}

		Span<System.Numerics.Vector3> directions = stackalloc System.Numerics.Vector3[Math.Max(1, Pellets)];
		ForgeShots3D.Spread(owner, aim, SpreadDegrees, directions);

		foreach (System.Numerics.Vector3 pellet in directions)
		{
			var direction = new Vector3(pellet.X, pellet.Y, pellet.Z);
			ShotHit3D? stop = ForgeShots3D.Hitscan(owner, eye, direction, Shot);
			Vector3 end = stop?.Position ?? (eye + (direction * Shot.Range));

			Execute(owner, TracerCue, new CueParameters(0, 0, null, ToNumerics(end)));

			if (stop is { Flight: null } hit)
			{
				Execute(owner, ImpactCue, new CueParameters(0, 0, null, ToNumerics(end), ToNumerics(hit.Normal)));
			}
		}
	}

	private static System.Numerics.Vector3 ToNumerics(Vector3 value)
	{
		return new System.Numerics.Vector3(value.X, value.Y, value.Z);
	}
}
