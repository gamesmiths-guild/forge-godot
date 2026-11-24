// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Godot.Nodes;
using Gamesmiths.Forge.Shots;
using Godot;

namespace Gamesmiths.Forge.Godot.Core.Shots;

/// <summary>
/// Something a 2D shot hit.
/// </summary>
/// <param name="Position">Where it hit.</param>
/// <param name="Normal">The normal of the surface it hit, or zero for a projectile.</param>
/// <param name="Distance">How far along the shot it hit.</param>
/// <param name="Collider">The collider it hit, or <see langword="null"/> for a projectile.</param>
/// <param name="Entity">The entity it hit, or <see langword="null"/> for a collider with none.</param>
/// <param name="Hitbox">The hitbox it hit, or <see langword="null"/>.</param>
/// <param name="Strength">The share of its strength the shot had left as it hit, times a hitbox's Strength Scale and
/// the shot's falloff at that distance: what its effects get as float context data.</param>
/// <param name="Flight">The projectile it hit in flight, if it hit one.</param>
public readonly record struct ShotHit2D(
	Vector2 Position,
	Vector2 Normal,
	float Distance,
	Node2D? Collider,
	IForgeEntity? Entity,
	ForgeHitbox2D? Hitbox,
	float Strength,
	ProjectileFlight? Flight = null);
