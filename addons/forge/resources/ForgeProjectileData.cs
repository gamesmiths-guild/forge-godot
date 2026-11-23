// Copyright © Gamesmiths Guild.

using System.Collections.Generic;
using Gamesmiths.Forge.Shots;
using Godot;
using Godot.Collections;

namespace Gamesmiths.Forge.Godot.Resources;

/// <summary>
/// A lightweight projectile: a shot that flies along a path every machine works out from its launch, so nothing is
/// sent while it flies.
/// </summary>
/// <remarks>
/// <para>Launch one from a graph's Launch Projectile node or from code (<see cref="Core.Shots.ForgeShots"/>). The
/// entity that launches it flies it in its ticks: the server, and the client that predicts the entity, test each tick's
/// stretch of its path against <see cref="CollisionMask"/>, apply <see cref="Effects"/> to the entity it meets, and
/// blast <see cref="Splash"/> where it ends. Every other client is sent the launch, flies <see cref="Scene"/> along the
/// same path, and is sent where it ended.</para>
/// <para>It goes through colliders as a hitscan's ray does: see <see cref="PierceMask"/> and <see cref="Power"/>.
/// </para>
/// <para>A networked session finds it in the network manifest, as it does effects.</para>
/// </remarks>
[Tool]
[GlobalClass]
public partial class ForgeProjectileData : Resource
{
	private static readonly System.Collections.Generic.Dictionary<ProjectileData, ForgeProjectileData> _byData = [];

	private ProjectileData? _data;

	/// <summary>
	/// Gets or sets the projectile's name.
	/// </summary>
	[Export]
	public string Name { get; set; } = "Projectile";

	/// <summary>
	/// Gets or sets how fast it is launched, in units per second.
	/// </summary>
	[Export(PropertyHint.Range, "0,500,0.1,or_greater")]
	public float Speed { get; set; } = 30;

	/// <summary>
	/// Gets or sets the acceleration it falls by, in units per second squared. A 2D projectile uses X and Y.
	/// </summary>
	[Export]
	public Vector3 Gravity { get; set; }

	/// <summary>
	/// Gets or sets its radius: what it sweeps as it flies, and what a shot that hits projectiles aims at.
	/// </summary>
	[Export(PropertyHint.Range, "0,10,0.01,or_greater")]
	public float Radius { get; set; } = 0.1f;

	/// <summary>
	/// Gets or sets how long it flies before it ends on its own, in seconds.
	/// </summary>
	[Export(PropertyHint.Range, "0.01,60,0.01,or_greater")]
	public float Lifetime { get; set; } = 3;

	/// <summary>
	/// Gets or sets the collision layers it hits.
	/// </summary>
	[Export(PropertyHint.Layers3DPhysics)]
	public uint CollisionMask { get; set; } = 1;

	/// <summary>
	/// Gets or sets the effects applied to an entity it hits.
	/// </summary>
	[Export]
	public Array<ForgeEffectData> Effects { get; set; } = [];

	/// <summary>
	/// Gets or sets the blast where it ends on an impact, or <see langword="null"/> for none.
	/// </summary>
	[Export]
	public ForgeSplashData? Splash { get; set; }

	/// <summary>
	/// Gets or sets a value indicating whether a shot that hits projectiles can hit it in flight.
	/// </summary>
	[Export]
	public bool Shootable { get; set; }

	/// <summary>
	/// Gets or sets the blast where a shot hits it, or <see langword="null"/> for none.
	/// </summary>
	[Export]
	public ForgeSplashData? StruckSplash { get; set; }

	/// <summary>
	/// Gets or sets the layers of the colliders it goes through.
	/// </summary>
	[ExportGroup("Penetration")]
	[Export(PropertyHint.Layers3DPhysics)]
	public uint PierceMask { get; set; }

	/// <summary>
	/// Gets or sets how many colliders it goes through at most. Negative has no limit.
	/// </summary>
	[Export(PropertyHint.Range, "-1,16,1,or_greater")]
	public int MaxPierces { get; set; } = 1;

	/// <summary>
	/// Gets or sets the share of its damage it keeps through each collider it goes through.
	/// </summary>
	[Export(PropertyHint.Range, "0,1,0.01")]
	public float PierceScale { get; set; } = 0.5f;

	/// <summary>
	/// Gets or sets the power it goes through colliders with a <see cref="ForgeShotSurface"/> with. Zero leaves them to
	/// the Pierce Mask.
	/// </summary>
	[Export(PropertyHint.Range, "0,1000,0.1,or_greater")]
	public float Power { get; set; }

	/// <summary>
	/// Gets or sets the scene shown flying along its path: a <see cref="Node3D"/> or a <see cref="Node2D"/>, only
	/// shown.
	/// </summary>
	[ExportGroup("Presentation")]
	[Export]
	public PackedScene? Scene { get; set; }

	/// <summary>
	/// Gets or sets the scene shown where it hits or is struck, only shown.
	/// </summary>
	[Export]
	public PackedScene? ImpactScene { get; set; }

	/// <summary>
	/// Gets the penetration it has.
	/// </summary>
	public Core.Shots.ShotPenetration Penetration => new(PierceMask, MaxPierces, PierceScale, Power);

	/// <summary>
	/// Gives the resource a projectile's data was built from.
	/// </summary>
	/// <param name="data">The data.</param>
	/// <returns>The resource, or <see langword="null"/> for data no resource built.</returns>
	public static ForgeProjectileData? Of(ProjectileData data)
	{
		return _byData.GetValueOrDefault(data);
	}

	/// <summary>
	/// Gives the projectile's data, built once.
	/// </summary>
	/// <returns>The data.</returns>
	public ProjectileData GetProjectileData()
	{
		if (_data is not null)
		{
			return _data;
		}

		_data = new ProjectileData(
			Name,
			new System.Numerics.Vector3(Gravity.X, Gravity.Y, Gravity.Z),
			Radius,
			Lifetime,
			Shootable);

		_byData[_data] = this;
		return _data;
	}
}
