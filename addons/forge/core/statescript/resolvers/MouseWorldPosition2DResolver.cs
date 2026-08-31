// Copyright © Gamesmiths Guild.

using System;
using Gamesmiths.Forge.Godot.Core.Statescript.Physics;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;
using NumericsVector2 = System.Numerics.Vector2;

namespace Gamesmiths.Forge.Godot.Core.Statescript.Resolvers;

/// <summary>
/// Resolves the point in the world the mouse cursor is over.
/// </summary>
/// <remarks>
/// <para>This is the cursor-aimed half of the standard aim payload, for the graphs that want to keep reading it rather
/// than sample it once at activation: a ground-targeted area that follows the cursor while it is being placed, a turret
/// that tracks it. An ability that only needs where the player was aiming when it started should read the payload
/// instead, which is also what a networked game must do - the cursor lives on the client.</para>
/// <para>Unlike its 3D twin this has no mode, no mask and no reach. A 2D world is the plane the cursor is already on,
/// so undoing the canvas transform <em>is</em> the answer, and there is nothing between the camera and that point to
/// hit or to miss. It is also not a camera resolver: a 2D game without a <see cref="Camera2D"/> still has a viewport
/// with a canvas transform, and reading it through the owner's own node works whether or not a camera is driving it.
/// </para>
/// </remarks>
internal sealed class MouseWorldPosition2DResolver : IPropertyResolver
{
	private bool _reportedMissingOwner;

	public Type ValueType => typeof(NumericsVector2);

	public Variant128 Resolve(GraphContext graphContext)
	{
		if (!PhysicsQuery2D.TryResolveContextNode(graphContext, out Node2D? contextNode)
			|| !contextNode.IsInsideTree())
		{
			ReportMissingOwnerOnce();
			return default;
		}

		Vector2 point = contextNode.GetGlobalMousePosition();
		return new Variant128(new NumericsVector2(point.X, point.Y));
	}

	// Resolvers run every tick, so a warning left unsuppressed would repeat every frame.
	private void ReportMissingOwnerOnce()
	{
		if (_reportedMissingOwner)
		{
			return;
		}

		_reportedMissingOwner = true;

		GD.PushWarning(
			"Statescript: MouseWorldPosition2DResolver has no owner in the scene to read a viewport from." +
			" Resolving to a default value.");
	}
}
