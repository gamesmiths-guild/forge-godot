// Copyright © Gamesmiths Guild.

using Godot;
using GodotDictionary = Godot.Collections.Dictionary;

namespace Gamesmiths.Forge.Godot.Core.Statescript.Providers;

/// <summary>
/// The standard aim payload an ability is activated with: where the aim starts, which way it points, and the point it
/// resolved to.
/// </summary>
/// <remarks>
/// <para>This exists so a game does not need its own activation-data type and provider just to tell an ability where
/// the player was aiming, which is what every projectile, blink and ground-targeted skill needs first. Read the members
/// in a graph through the ability activation data resolver.</para>
/// <para>There is deliberately no target entity member. Activation-data members are read through the value lane, which
/// carries numbers and math structs but not object references, so an entity member would look bindable in the editor
/// and never resolve. Targeting a specific entity already has a home - the ability's own target - and graphs that need
/// to find one use a physics query instead.</para>
/// <para>Members are Godot vectors because the code that builds this is Godot code. The graph side converts them, which
/// is why the provider declares its inputs as <c>System.Numerics</c> vectors.</para>
/// </remarks>
/// <param name="Origin">Where the aim starts: an eye, a muzzle, or the caster's own position.</param>
/// <param name="Direction">A unit vector pointing where the aim goes.</param>
/// <param name="TargetPoint">The point the aim resolved to, whether that is a surface it hit or the far end of its
/// range.</param>
public readonly record struct AimActivationData(Vector3 Origin, Vector3 Direction, Vector3 TargetPoint)
{
	private const float DefaultMaxDistance = 1000.0f;

	/// <summary>
	/// Builds aim data from where a camera is looking, which is what a first or third person shooter wants.
	/// </summary>
	/// <param name="camera">The camera to aim from.</param>
	/// <param name="collisionMask">The physics layers the aim ray can hit.</param>
	/// <param name="maxDistance">How far the aim reaches when it hits nothing.</param>
	/// <returns>The aim data.</returns>
	public static AimActivationData FromCamera(
		Camera3D camera,
		uint collisionMask = uint.MaxValue,
		float maxDistance = DefaultMaxDistance)
	{
		Vector3 origin = camera.GlobalPosition;
		Vector3 direction = -camera.GlobalBasis.Z.Normalized();

		return new AimActivationData(
			origin,
			direction,
			CastForPoint(camera, origin, origin + (direction * maxDistance), collisionMask));
	}

	/// <summary>
	/// Builds aim data from where the mouse is pointing on the ground, which is what a top-down or isometric game
	/// wants.
	/// </summary>
	/// <remarks>
	/// The aim resolves against physics first and falls back to the horizontal plane through <paramref name="source"/>,
	/// so pointing at empty sky still yields a usable point rather than nothing. The direction is flattened, because a
	/// character aiming at the ground should turn rather than tilt.
	/// </remarks>
	/// <param name="source">The entity's spatial node, used as the aim origin and as the plane's height.</param>
	/// <param name="collisionMask">The physics layers the aim ray can hit.</param>
	/// <param name="maxDistance">How far the aim reaches when it hits nothing.</param>
	/// <returns>The aim data.</returns>
	public static AimActivationData FromMouseGround(
		Node3D source,
		uint collisionMask = uint.MaxValue,
		float maxDistance = DefaultMaxDistance)
	{
		Vector3 origin = source.GlobalPosition;
		Viewport? viewport = source.GetViewport();
		Camera3D? camera = viewport?.GetCamera3D();

		if (viewport is null || camera is null)
		{
			return new AimActivationData(origin, -source.GlobalBasis.Z.Normalized(), origin);
		}

		Vector2 mousePosition = viewport.GetMousePosition();
		Vector3 rayOrigin = camera.ProjectRayOrigin(mousePosition);
		Vector3 rayDirection = camera.ProjectRayNormal(mousePosition);

		Vector3 targetPoint = CastForPoint(
			source,
			rayOrigin,
			rayOrigin + (rayDirection * maxDistance),
			collisionMask,
			out bool hit);

		if (!hit)
		{
			Vector3? planePoint = new Plane(Vector3.Up, origin.Y).IntersectsRay(rayOrigin, rayDirection);
			targetPoint = planePoint ?? origin;
		}

		Vector3 flattened = targetPoint - origin;
		flattened.Y = 0;

		Vector3 direction = flattened.LengthSquared() > 0.000001f
			? flattened.Normalized()
			: -source.GlobalBasis.Z.Normalized();

		return new AimActivationData(origin, direction, targetPoint);
	}

	private static Vector3 CastForPoint(Node3D context, Vector3 from, Vector3 to, uint collisionMask)
	{
		return CastForPoint(context, from, to, collisionMask, out _);
	}

	private static Vector3 CastForPoint(Node3D context, Vector3 from, Vector3 to, uint collisionMask, out bool hit)
	{
		hit = false;
		World3D? world = context.GetWorld3D();

		if (world is null)
		{
			return to;
		}

		var query = PhysicsRayQueryParameters3D.Create(from, to, collisionMask);
		GodotDictionary result = world.DirectSpaceState.IntersectRay(query);

		if (result.Count == 0)
		{
			return to;
		}

		hit = true;
		return result["position"].AsVector3();
	}
}
