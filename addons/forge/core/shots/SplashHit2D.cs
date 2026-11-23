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
/// <param name="Scale">The share of the blast's effects it got, which they get as float context data.</param>
/// <param name="Knockback">The velocity the blast added to its body.</param>
public readonly record struct SplashHit2D(
	IForgeEntity Entity,
	Vector2 Position,
	float Distance,
	float Scale,
	Vector2 Knockback);
