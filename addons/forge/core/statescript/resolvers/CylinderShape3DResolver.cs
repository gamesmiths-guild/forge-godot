// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;

namespace Gamesmiths.Forge.Godot.Core.Statescript.Resolvers;

/// <summary>
/// Resolves an upright cylinder of a given radius and height.
/// </summary>
/// <param name="radiusResolver">Resolves the radius.</param>
/// <param name="heightResolver">Resolves the total height.</param>
internal sealed class CylinderShape3DResolver(
	IPropertyResolver radiusResolver,
	IPropertyResolver heightResolver) : ShapeResolverBase3D
{
	private readonly IPropertyResolver _radiusResolver = radiusResolver;
	private readonly IPropertyResolver _heightResolver = heightResolver;

	protected override Shape3D CreateShape()
	{
		return new CylinderShape3D();
	}

	protected override void UpdateShape(Shape3D shape, GraphContext graphContext)
	{
		var cylinder = (CylinderShape3D)shape;
		cylinder.Radius = ResolveDimension(_radiusResolver, graphContext);
		cylinder.Height = ResolveDimension(_heightResolver, graphContext);
	}
}
