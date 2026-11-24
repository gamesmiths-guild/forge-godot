// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;
using Godot;

namespace Gamesmiths.Forge.Godot.Core.Shots;

/// <summary>
/// An entity a 2D blast reached.
/// </summary>
/// <param name="Entity">The entity.</param>
/// <param name="Position">The center of its body, where the blast saw it.</param>
/// <param name="Distance">How far its body's center was from the blast's.</param>
/// <param name="Strength">The share of the blast's strength it got, which its effects get as float context data.
/// </param>
/// <param name="Knockback">The push the blast gave it, which its body takes as velocity if it has a character
/// movement.</param>
public readonly record struct SplashHit2D(
	IForgeEntity Entity,
	Vector2 Position,
	float Distance,
	float Strength,
	Vector2 Knockback);
