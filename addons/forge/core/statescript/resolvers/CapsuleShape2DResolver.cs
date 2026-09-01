// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;

namespace Gamesmiths.Forge.Godot.Core.Statescript.Resolvers;

/// <summary>
/// Resolves an upright capsule of a given radius and height.
/// </summary>
/// <remarks>
/// The height is the capsule's total height including both rounded caps, matching Godot's own
/// <see cref="CapsuleShape2D.Height"/>, and cannot be less than twice the radius - a capsule shorter than that is a
/// circle. Rotating it on its side is the query's business, not the shape's.
/// </remarks>
/// <param name="radiusResolver">Resolves the radius.</param>
/// <param name="heightResolver">Resolves the total height.</param>
internal sealed class CapsuleShape2DResolver(
	IPropertyResolver radiusResolver,
	IPropertyResolver heightResolver) : ShapeResolverBase2D
{
	private readonly IPropertyResolver _radiusResolver = radiusResolver;
	private readonly IPropertyResolver _heightResolver = heightResolver;

	protected override Shape2D CreateShape()
	{
		return new CapsuleShape2D();
	}

	protected override void UpdateShape(Shape2D shape, GraphContext graphContext)
	{
		var capsule = (CapsuleShape2D)shape;
		float radius = ResolveDimension(_radiusResolver, graphContext);

		capsule.Radius = radius;
		capsule.Height = Mathf.Max(ResolveDimension(_heightResolver, graphContext), radius * 2.0f);
	}
}
