// Copyright © Gamesmiths Guild.

using System;
using Gamesmiths.Forge.Statescript;
using Godot;
using NumericsVector3 = System.Numerics.Vector3;

namespace Gamesmiths.Forge.Godot.Core.Statescript.Resolvers;

/// <summary>
/// Resolves the direction the camera a graph is looking through faces, as a unit vector.
/// </summary>
/// <remarks>
/// Unpaired: a 2D camera has no forward. Godot's forward is −Z, which this already accounts for, so a ray cast along it
/// goes where the crosshair points rather than behind the player.
/// </remarks>
internal sealed class CameraForward3DResolver : CameraResolverBase3D
{
	public override Type ValueType => typeof(NumericsVector3);

	protected override Variant128 ResolveFrom(Node3D contextNode, Camera3D camera, GraphContext graphContext)
	{
		Vector3 forward = -camera.GlobalBasis.Z.Normalized();
		return new Variant128(new NumericsVector3(forward.X, forward.Y, forward.Z));
	}
}
