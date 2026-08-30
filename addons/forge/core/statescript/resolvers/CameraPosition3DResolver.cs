// Copyright © Gamesmiths Guild.

using System;
using Gamesmiths.Forge.Statescript;
using Godot;
using NumericsVector3 = System.Numerics.Vector3;

namespace Gamesmiths.Forge.Godot.Core.Statescript.Resolvers;

/// <summary>
/// Resolves where the camera a graph is looking through sits in the world.
/// </summary>
/// <remarks>
/// Paired with Camera Forward 3D this is the origin and direction of a shooter's centre-aim ray, which is the reason
/// both exist: a hitscan shot is aimed from the eye, not from the muzzle, or it disagrees with the crosshair.
/// </remarks>
internal sealed class CameraPosition3DResolver : CameraResolverBase3D
{
	public override Type ValueType => typeof(NumericsVector3);

	protected override Variant128 ResolveFrom(Node3D contextNode, Camera3D camera, GraphContext graphContext)
	{
		Vector3 position = camera.GlobalPosition;
		return new Variant128(new NumericsVector3(position.X, position.Y, position.Z));
	}
}
