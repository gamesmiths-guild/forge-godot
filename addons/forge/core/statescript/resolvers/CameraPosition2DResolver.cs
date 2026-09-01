// Copyright © Gamesmiths Guild.

using System;
using Gamesmiths.Forge.Statescript;
using Godot;
using NumericsVector2 = System.Numerics.Vector2;

namespace Gamesmiths.Forge.Godot.Core.Statescript.Resolvers;

/// <summary>
/// Resolves where the camera a graph is looking through sits in the world.
/// </summary>
/// <remarks>
/// In 2D that is the centre of what the player can see, which is what makes it the anchor for anything measured against
/// the view rather than against a character: an off-screen check, a spawn just outside the edge, a pull towards the
/// middle of the frame.
/// </remarks>
internal sealed class CameraPosition2DResolver : CameraResolverBase2D
{
	public override Type ValueType => typeof(NumericsVector2);

	protected override Variant128 ResolveFrom(Node2D contextNode, Camera2D camera, GraphContext graphContext)
	{
		Vector2 position = camera.GlobalPosition;
		return new Variant128(new NumericsVector2(position.X, position.Y));
	}
}
