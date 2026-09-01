// Copyright © Gamesmiths Guild.

using Godot;

namespace Gamesmiths.Forge.Godot.Core.Statescript.Physics;

/// <summary>
/// The stretch of world a ray query actually covered: from its origin to the hit, or to its full reach when it missed.
/// </summary>
/// <remarks>
/// Reported alongside the hit so debug drawing shows the query that ran rather than the inputs that went into it — a
/// ray whose direction resolved to zero draws nothing, which is exactly what it did.
/// </remarks>
/// <param name="From">Where the ray started.</param>
/// <param name="To">Where the ray stopped.</param>
internal readonly record struct RaySegment2D(Vector2 From, Vector2 To);
