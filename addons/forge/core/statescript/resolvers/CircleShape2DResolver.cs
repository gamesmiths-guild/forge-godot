// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;

namespace Gamesmiths.Forge.Godot.Core.Statescript.Resolvers;

/// <summary>
/// Resolves a circle of a given radius.
/// </summary>
/// <param name="radiusResolver">Resolves the radius.</param>
internal sealed class CircleShape2DResolver(IPropertyResolver radiusResolver) : ShapeResolverBase2D
{
	private readonly IPropertyResolver _radiusResolver = radiusResolver;

	protected override Shape2D CreateShape()
	{
		return new CircleShape2D();
	}

	protected override void UpdateShape(Shape2D shape, GraphContext graphContext)
	{
		((CircleShape2D)shape).Radius = ResolveDimension(_radiusResolver, graphContext);
	}
}
