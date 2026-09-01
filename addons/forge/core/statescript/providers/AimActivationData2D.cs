// Copyright © Gamesmiths Guild.

using Godot;

namespace Gamesmiths.Forge.Godot.Core.Statescript.Providers;

/// <summary>
/// The standard aim payload a 2D ability is activated with: where the aim starts, which way it points, and the point it
/// resolved to.
/// </summary>
/// <remarks>
/// <para>This exists so a game does not need its own activation-data type and provider just to tell an ability where
/// the player was aiming, which is what every projectile, blink and ground-targeted skill needs first. Read the members
/// in a graph through the ability activation data resolver.</para>
/// <para>There is deliberately no target entity member, for the same reason its 3D twin has none: activation-data
/// members are read through the value lane, which carries numbers and math structs but not object references, so an
/// entity member would look bindable in the editor and never resolve.</para>
/// <para>Members are Godot vectors because the code that builds this is Godot code. The graph side converts them, which
/// is why the provider declares its inputs as <c>System.Numerics</c> vectors.</para>
/// </remarks>
/// <param name="Origin">Where the aim starts: a muzzle, or the caster's own position.</param>
/// <param name="Direction">A unit vector pointing where the aim goes.</param>
/// <param name="TargetPoint">The point the aim resolved to, whether that is where the cursor sits or the far end of its
/// range.</param>
public readonly record struct AimActivationData2D(Vector2 Origin, Vector2 Direction, Vector2 TargetPoint)
{
	private const float DefaultMaxDistance = 1000.0f;

	private const float DirectionEpsilon = 0.000001f;

	/// <summary>
	/// Builds aim data from where the mouse is pointing, which is what a top-down or twin-stick game wants.
	/// </summary>
	/// <remarks>
	/// Nothing is cast. A 2D cursor already names a world point exactly, so a ray between the caster and it could only
	/// replace where the player pointed with whatever stands in the way, which is the mistake the 3D twin's
	/// plane-intersect mode exists to avoid. The point is clamped to <paramref name="maxDistance"/> instead, so an
	/// ability with a range aims at the edge of it rather than past it.
	/// </remarks>
	/// <param name="source">The entity's spatial node, used as the aim origin and as the viewport to read the cursor
	/// from.</param>
	/// <param name="maxDistance">How far the aim reaches.</param>
	/// <returns>The aim data.</returns>
	public static AimActivationData2D FromMouse(Node2D source, float maxDistance = DefaultMaxDistance)
	{
		Vector2 origin = source.GlobalPosition;

		if (!source.IsInsideTree())
		{
			return new AimActivationData2D(origin, Forward(source), origin);
		}

		Vector2 offset = source.GetGlobalMousePosition() - origin;

		if (offset.LengthSquared() <= DirectionEpsilon)
		{
			return new AimActivationData2D(origin, Forward(source), origin);
		}

		Vector2 direction = offset.Normalized();
		float distance = Mathf.Min(offset.Length(), Mathf.Max(maxDistance, 0.0f));

		return new AimActivationData2D(origin, direction, origin + (direction * distance));
	}

	/// <summary>
	/// Builds aim data from the way the caster is already facing, which is what a game aimed with a stick or with the
	/// character's own heading wants.
	/// </summary>
	/// <param name="source">The entity's spatial node, used as the aim origin and for its facing.</param>
	/// <param name="maxDistance">How far the aim reaches.</param>
	/// <returns>The aim data.</returns>
	public static AimActivationData2D FromFacing(Node2D source, float maxDistance = DefaultMaxDistance)
	{
		Vector2 origin = source.GlobalPosition;
		Vector2 direction = Forward(source);

		return new AimActivationData2D(origin, direction, origin + (direction * Mathf.Max(maxDistance, 0.0f)));
	}

	// Every direction this type hands out is a unit vector, including the fallbacks. A node scaled to zero has no
	// forward at all, in which case +X is the same answer an unrotated node would give.
	private static Vector2 Forward(Node2D source)
	{
		Vector2 forward = source.GlobalTransform.X;

		return forward.LengthSquared() > DirectionEpsilon ? forward.Normalized() : Vector2.Right;
	}
}
