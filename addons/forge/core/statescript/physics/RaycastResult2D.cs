// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;
using Godot;

namespace Gamesmiths.Forge.Godot.Core.Statescript.Physics;

/// <summary>
/// Everything one ray query reported, gathered so the nodes that write it to output variables write all of it at once.
/// </summary>
/// <remarks>
/// This is deliberately not a graph-visible object type. A ray produces several values that must agree with each other,
/// and a graph reads them as separate outputs written in the same step; giving graphs a hit object instead would mean a
/// variable type, a resolver per member, and no gain over five outputs.
/// </remarks>
/// <param name="Position">Where the ray met the surface.</param>
/// <param name="Normal">The surface normal at that point.</param>
/// <param name="Node">The collider that was hit.</param>
/// <param name="Entity">The entity that collider belongs to, when it belongs to one.</param>
/// <param name="Distance">How far along the ray the hit is, in units.</param>
/// <param name="Rid">The physics object that owns the shape, which a caller repeating the query excludes by. Not every
/// owner is a <see cref="CollisionObject2D"/> - a <see cref="TileMapLayer"/> owns its shapes itself - so this is the
/// only identity every hit has.</param>
internal readonly record struct RaycastResult2D(
	Vector2 Position,
	Vector2 Normal,
	Node2D? Node,
	IForgeEntity? Entity,
	float Distance,
	Rid Rid);
