// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;

namespace Gamesmiths.Forge.Godot.Core.Statescript.Resolvers;

/// <summary>
/// Base for the resolvers that build a query shape from numbers.
/// </summary>
/// <remarks>
/// <para>The shape is created once and resized on every resolve rather than rebuilt, because these are read by queries
/// that run every tick and a shape is a Godot resource. The instance is therefore shared by everything that reads the
/// same resolver in the same frame, which is safe because a resolved shape is consumed by the query that asked for it
/// before anything else runs.</para>
/// <para>Dimensions are resolvers rather than fixed numbers, which is the whole reason shapes are built here instead of
/// exported: a radius that cannot scale with an attribute or an ability level is not much use.</para>
/// </remarks>
internal abstract class ShapeResolverBase3D : ObjectResolver<Shape3D>
{
	private Shape3D? _shape;

	/// <summary>
	/// Creates this resolver's shape, once.
	/// </summary>
	/// <returns>The new shape.</returns>
	protected abstract Shape3D CreateShape();

	/// <summary>
	/// Applies this resolver's current dimensions to the shape.
	/// </summary>
	/// <param name="shape">The shape to resize.</param>
	/// <param name="graphContext">The graph execution context, for resolving the dimensions.</param>
	protected abstract void UpdateShape(Shape3D shape, GraphContext graphContext);

#pragma warning disable SA1202 // Elements should be ordered by access
	public sealed override Shape3D Resolve(GraphContext graphContext)
	{
		if (_shape is null || !GodotObject.IsInstanceValid(_shape))
		{
			_shape = CreateShape();
		}

		UpdateShape(_shape, graphContext);
		return _shape;
	}
#pragma warning restore SA1202 // Elements should be ordered by access

	/// <summary>
	/// Resolves a dimension, clamped to a usable size.
	/// </summary>
	/// <remarks>
	/// A negative extent is a mistake rather than an intent, and Godot's shapes reject one; a zero one is left alone,
	/// since a query with no size finding nothing is the honest result of authoring no size.
	/// </remarks>
	/// <param name="resolver">The resolver providing the dimension.</param>
	/// <param name="graphContext">The graph execution context.</param>
	/// <returns>The dimension.</returns>
	protected static float ResolveDimension(IPropertyResolver resolver, GraphContext graphContext)
	{
		return Mathf.Max((float)resolver.Resolve(graphContext).AsDouble(), 0.0f);
	}
}
