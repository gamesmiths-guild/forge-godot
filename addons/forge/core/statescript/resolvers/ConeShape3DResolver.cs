// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Godot.Core.Statescript.Physics;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;

namespace Gamesmiths.Forge.Godot.Core.Statescript.Resolvers;

/// <summary>
/// Resolves a cone of a given aperture and reach, as a convex hull.
/// </summary>
/// <remarks>
/// <para>Godot has no cone collision shape, so this builds one: the convex hull of the apex and a ring of points on
/// the sphere the cone is inscribed in. That makes a cone a real <see cref="Shape3D"/>, which is what lets it be swept
/// by Shapecast, held by an Overlap node, or handed anywhere else a shape goes — none of which Entities In Cone 3D can
/// do, because that resolver's cone exists only for the instant it is tested.</para>
/// <para><b>It is a faceted approximation and the facets sit inside the true cone.</b> The ring is a polygon
/// inscribed in the rim circle, so a target near the rim between two facets is outside this hull and inside the cone
/// Entities In Cone 3D describes. Where the exact answer matters, that resolver — or Is In Cone 3D as a filter — is
/// the one to use; this is for when the cone has to <em>be a shape</em>.</para>
/// <para><b>It points along −Z, not +Y.</b> Godot's own primitives are Y-up, but a gameplay cone is aimed where a
/// character faces, and Godot's forward is −Z — so binding a query's Rotation operand to Entity Rotation 3D aims this
/// correctly with nothing composed in between. The apex sits at the shape's own origin, so binding Position to a
/// caster's position puts the point of the cone on the caster.</para>
/// <para>The aperture is clamped below a half turn. A hull cannot express a reflex cone: past that the rim radius runs
/// away to infinity and the hull swallows the space behind the apex, which is the opposite of what was asked for.
/// </para>
/// </remarks>
/// <param name="angleResolver">Resolves the full aperture, in degrees.</param>
/// <param name="rangeResolver">Resolves how far the cone reaches along its slant.</param>
internal sealed class ConeShape3DResolver(IPropertyResolver angleResolver, IPropertyResolver rangeResolver)
	: ShapeResolverBase3D
{
	// How many points the rim is built from. Enough that the facets are not obvious at gameplay scale, few enough that
	// the hull stays cheap for the physics server to test against.
	private const int RimSegments = 16;

	// Just under a half turn, which is the widest aperture whose hull is still a cone rather than a solid that has
	// closed around behind its own apex.
	private const float MaximumAngleDegrees = 179.0f;

	private readonly IPropertyResolver _angleResolver = angleResolver;
	private readonly IPropertyResolver _rangeResolver = rangeResolver;
	private readonly Vector3[] _points = new Vector3[RimSegments + 1];

	protected override Shape3D CreateShape()
	{
		return new ConvexPolygonShape3D();
	}

	protected override void UpdateShape(Shape3D shape, GraphContext graphContext)
	{
		float range = ResolveDimension(_rangeResolver, graphContext);
		float halfAngle = ConeQuery.HalfAngleRadians(
			Mathf.Clamp((float)_angleResolver.Resolve(graphContext).AsDouble(), 0.0f, MaximumAngleDegrees));

		// The rim sits on the sphere of the given reach, so this hull inscribes exactly the cone Entities In Cone 3D
		// tests analytically and the debug drawing shows.
		float rimRadius = range * Mathf.Sin(halfAngle);
		float rimDepth = range * Mathf.Cos(halfAngle);

		_points[0] = Vector3.Zero;

		for (int i = 0; i < RimSegments; i++)
		{
			float around = Mathf.Tau * i / RimSegments;

			_points[i + 1] = new Vector3(
				Mathf.Cos(around) * rimRadius,
				Mathf.Sin(around) * rimRadius,
				-rimDepth);
		}

		((ConvexPolygonShape3D)shape).Points = _points;
	}
}
