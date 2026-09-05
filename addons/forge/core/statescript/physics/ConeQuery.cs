// Copyright © Gamesmiths Guild.

using Godot;

namespace Gamesmiths.Forge.Godot.Core.Statescript.Physics;

/// <summary>
/// The angle test behind the cone queries, in one place so the query and the filter cannot disagree.
/// </summary>
/// <remarks>
/// Entities In Cone runs this over what a sphere query found; Is In Cone runs it over one point inside a Where. If the
/// two carried their own copies, an entity could pass the filter and be absent from the query — the apex rule and the
/// epsilon below are exactly the sort of detail that drifts.
/// </remarks>
internal static class ConeQuery
{
	// Below this the offset has no direction to test, and normalizing it would divide by roughly zero.
	private const float MinimumOffsetSquared = 0.000001f;

	// Saturated rather than wrapped, because past a full turn the cosine comes back down: an aperture typed as 450
	// would test as the 270 degree cone it shares a cosine with, narrowing what the typo asked to widen.
	private const float FullTurnDegrees = 360.0f;

	/// <summary>
	/// Turns an authored aperture into the cosine the tests compare against.
	/// </summary>
	/// <remarks>
	/// The authored figure is the <em>whole</em> aperture in degrees and is halved here, because a 90 degree cleave
	/// means 45 degrees either side of the facing everywhere that phrase is used. Degrees rather than radians is the
	/// one place the layer departs from its own convention: an aperture is a design figure that is typed once, never
	/// lerped, wrapped, or read off a transform.
	/// </remarks>
	/// <param name="degrees">The full aperture, in degrees.</param>
	/// <returns>The cosine of the half angle.</returns>
	public static float ResolveCosHalfAngle(float degrees)
	{
		return Mathf.Cos(HalfAngleRadians(degrees));
	}

	/// <summary>
	/// Gets half an authored aperture in radians, which is what the debug drawing takes.
	/// </summary>
	/// <param name="degrees">The full aperture, in degrees.</param>
	/// <returns>Half the aperture, in radians.</returns>
	public static float HalfAngleRadians(float degrees)
	{
		return Mathf.DegToRad(Mathf.Clamp(degrees, 0.0f, FullTurnDegrees)) * 0.5f;
	}

	/// <summary>
	/// Gets whether an offset from the cone's apex falls inside its aperture.
	/// </summary>
	/// <remarks>
	/// The apex counts as inside. Something standing exactly on the caster has no direction to test, and dropping it
	/// would make point-blank the one range at which a cleave misses.
	/// </remarks>
	/// <param name="offset">The offset from the apex to the point being tested.</param>
	/// <param name="axis">The cone's axis, already normalized.</param>
	/// <param name="cosHalfAngle">The cosine of half the aperture.</param>
	/// <returns><see langword="true"/> if the point is inside the cone's aperture.</returns>
	public static bool IsWithinAngle(Vector3 offset, Vector3 axis, float cosHalfAngle)
	{
		return offset.LengthSquared() <= MinimumOffsetSquared || offset.Normalized().Dot(axis) >= cosHalfAngle;
	}

	/// <summary>
	/// Gets whether an offset from the wedge's apex falls inside its aperture.
	/// </summary>
	/// <remarks>
	/// The apex counts as inside. Something standing exactly on the caster has no direction to test, and dropping it
	/// would make point-blank the one range at which a cleave misses.
	/// </remarks>
	/// <param name="offset">The offset from the apex to the point being tested.</param>
	/// <param name="axis">The wedge's axis, already normalized.</param>
	/// <param name="cosHalfAngle">The cosine of half the aperture.</param>
	/// <returns><see langword="true"/> if the point is inside the wedge's aperture.</returns>
	public static bool IsWithinAngle(Vector2 offset, Vector2 axis, float cosHalfAngle)
	{
		return offset.LengthSquared() <= MinimumOffsetSquared || offset.Normalized().Dot(axis) >= cosHalfAngle;
	}
}
