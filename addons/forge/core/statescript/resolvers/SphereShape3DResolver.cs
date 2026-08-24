// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;

namespace Gamesmiths.Forge.Godot.Core.Statescript.Resolvers;

/// <summary>
/// Resolves a sphere of a given radius.
/// </summary>
/// <param name="radiusResolver">Resolves the radius.</param>
internal sealed class SphereShape3DResolver(IPropertyResolver radiusResolver) : ShapeResolverBase3D
{
	private readonly IPropertyResolver _radiusResolver = radiusResolver;

	protected override Shape3D CreateShape()
	{
		return new SphereShape3D();
	}

	protected override void UpdateShape(Shape3D shape, GraphContext graphContext)
	{
		((SphereShape3D)shape).Radius = ResolveDimension(_radiusResolver, graphContext);
	}
}
