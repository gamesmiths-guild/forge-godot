// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Godot.Core.Statescript.Physics;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;

namespace Gamesmiths.Forge.Godot.Core.Statescript.Resolvers;

/// <summary>
/// Resolves a wedge of a given aperture and reach, as a convex polygon.
/// </summary>
/// <remarks>
/// <para>The 2D reading of Cone Shape 3D, and named for what it is: a plane's cone is a wedge. Godot has no wedge
/// collision shape, so this builds one from the apex and an arc of points at the given reach. That makes it a real
/// <see cref="Shape2D"/>, which is what lets it be swept by Shapecast, held by an Overlap node, or handed anywhere
/// else a shape goes — none of which Entities In Cone 2D can do, because that resolver's wedge exists only for the
/// instant it is tested.</para>
/// <para><b>It is a faceted approximation and the facets sit inside the true wedge.</b> The arc is a polyline
/// inscribed in the reach circle, so a target near the arc between two points is outside this polygon and inside the
/// wedge Entities In Cone 2D describes. Where the exact answer matters, that resolver — or Is In Cone 2D as a filter —
/// is the one to use; this is for when the wedge has to <em>be a shape</em>.</para>
/// <para><b>It opens along +X</b>, which is Godot's 2D forward, so binding a query's Rotation operand to Entity
/// Rotation 2D aims it correctly with nothing composed in between. The apex sits at the shape's own origin, so binding
/// Position to a caster's position puts the point of the wedge on the caster.</para>
/// <para>The aperture is clamped below a half turn, because a convex polygon cannot express a reflex wedge: past that
/// the hull closes around behind its own apex, which is the opposite of what was asked for.</para>
/// </remarks>
/// <param name="angleResolver">Resolves the full aperture, in degrees.</param>
/// <param name="rangeResolver">Resolves how far the wedge reaches.</param>
internal sealed class WedgeShape2DResolver(IPropertyResolver angleResolver, IPropertyResolver rangeResolver)
	: ShapeResolverBase2D
{
	// How many points the arc is built from. Enough that the facets are not obvious at gameplay scale, few enough that
	// the polygon stays cheap for the physics server to test against.
	private const int ArcSegments = 12;

	// Just under a half turn, which is the widest aperture whose hull is still a wedge rather than a polygon that has
	// closed around behind its own apex.
	private const float MaximumAngleDegrees = 179.0f;

	private readonly IPropertyResolver _angleResolver = angleResolver;
	private readonly IPropertyResolver _rangeResolver = rangeResolver;
	private readonly Vector2[] _points = new Vector2[ArcSegments + 2];

	protected override Shape2D CreateShape()
	{
		return new ConvexPolygonShape2D();
	}

	protected override void UpdateShape(Shape2D shape, GraphContext graphContext)
	{
		float range = ResolveDimension(_rangeResolver, graphContext);
		float halfAngle = ConeQuery.HalfAngleRadians(
			Mathf.Clamp((float)_angleResolver.Resolve(graphContext).AsDouble(), 0.0f, MaximumAngleDegrees));

		_points[0] = Vector2.Zero;

		// One more point than segments, so both edges of the wedge are real vertices rather than the ends of the arc
		// falling short of the aperture the author asked for.
		for (int i = 0; i <= ArcSegments; i++)
		{
			float around = -halfAngle + (2 * halfAngle * i / ArcSegments);
			_points[i + 1] = Vector2.FromAngle(around) * range;
		}

		((ConvexPolygonShape2D)shape).Points = _points;
	}
}
