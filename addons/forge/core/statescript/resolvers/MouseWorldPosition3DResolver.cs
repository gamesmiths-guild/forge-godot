// Copyright © Gamesmiths Guild.

using System;
using Gamesmiths.Forge.Godot.Core.Statescript.Physics;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;
using NumericsVector3 = System.Numerics.Vector3;

namespace Gamesmiths.Forge.Godot.Core.Statescript.Resolvers;

/// <summary>
/// Resolves the point in the world the mouse cursor is over.
/// </summary>
/// <remarks>
/// <para>This is the cursor-aimed half of the standard aim payload, for the graphs that want to keep reading it rather
/// than sample it once at activation: a ground-targeted area that follows the cursor while it is being placed, a turret
/// that tracks it. An ability that only needs where the player was aiming when it started should read the payload
/// instead, which is also what a networked game must do - the cursor lives on the client.</para>
/// <para>Both modes end at the far end of the ray when they resolve to nothing, so a cursor pointed at empty sky still
/// yields a usable point in the direction it was pointing rather than the world origin.</para>
/// </remarks>
/// <param name="mode">How the cursor's ray is turned into a point.</param>
/// <param name="maskResolver">Resolves the physics layers the ray can hit under
/// <see cref="MouseWorldMode.PhysicsRay"/>. Zero means every layer.</param>
/// <param name="maxDistanceResolver">Resolves how far the ray reaches.</param>
internal sealed class MouseWorldPosition3DResolver(
	MouseWorldMode mode,
	IPropertyResolver? maskResolver,
	IPropertyResolver maxDistanceResolver) : CameraResolverBase3D
{
	private readonly MouseWorldMode _mode = mode;
	private readonly IPropertyResolver? _maskResolver = maskResolver;
	private readonly IPropertyResolver _maxDistanceResolver = maxDistanceResolver;

	public override Type ValueType => typeof(NumericsVector3);

	protected override Variant128 ResolveFrom(Node3D contextNode, Camera3D camera, GraphContext graphContext)
	{
		Vector2 mousePosition = camera.GetViewport().GetMousePosition();
		Vector3 rayOrigin = camera.ProjectRayOrigin(mousePosition);
		Vector3 rayDirection = camera.ProjectRayNormal(mousePosition);

		float maxDistance = (float)Math.Max(_maxDistanceResolver.Resolve(graphContext).AsDouble(), 0.0);
		Vector3 rayEnd = rayOrigin + (rayDirection * maxDistance);

		Vector3 point = _mode == MouseWorldMode.PlaneIntersect
			? ResolveOnPlane(contextNode, rayOrigin, rayDirection, maxDistance, rayEnd)
			: ResolveAgainstPhysics(contextNode, rayOrigin, rayDirection, maxDistance, graphContext, rayEnd);

		return new Variant128(new NumericsVector3(point.X, point.Y, point.Z));
	}

	// The plane is infinite, so a near-horizontal ray meets it arbitrarily far away. Accepting that would silently
	// ignore the max distance, so a point out of reach is discarded in favour of the ray's own end, which is clamped.
	private static Vector3 ResolveOnPlane(
		Node3D contextNode,
		Vector3 rayOrigin,
		Vector3 rayDirection,
		float maxDistance,
		Vector3 rayEnd)
	{
		Vector3? planePoint =
			new Plane(Vector3.Up, contextNode.GlobalPosition.Y).IntersectsRay(rayOrigin, rayDirection);

		return planePoint.HasValue && planePoint.Value.DistanceSquaredTo(rayOrigin) <= maxDistance * maxDistance
			? planePoint.Value
			: rayEnd;
	}

	private Vector3 ResolveAgainstPhysics(
		Node3D contextNode,
		Vector3 rayOrigin,
		Vector3 rayDirection,
		float maxDistance,
		GraphContext graphContext,
		Vector3 rayEnd)
	{
		World3D? world = contextNode.GetWorld3D();

		if (world is null)
		{
			return rayEnd;
		}

		int mask = _maskResolver is null ? 0 : (int)_maskResolver.Resolve(graphContext).AsDouble();

		return PhysicsQuery3D.TryRaycast(
			world,
			rayOrigin,
			rayDirection,
			maxDistance,
			PhysicsQuery3D.ResolveMask(mask),
			collideWithAreas: false,
			hitFromInside: false,
			exclude: null,
			out RaycastResult3D result)
				? result.Position
				: rayEnd;
	}
}
