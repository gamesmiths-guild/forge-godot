// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;
using NumericsVector2 = System.Numerics.Vector2;

namespace Gamesmiths.Forge.Godot.Core.Statescript.Resolvers;

/// <summary>
/// Resolves a rectangle of a given size.
/// </summary>
/// <remarks>
/// The size is the rectangle's full width and height, matching Godot's own <see cref="RectangleShape2D.Size"/> rather
/// than the half-extents older engines used, so a number typed here means the same thing it means in the inspector.
/// </remarks>
/// <param name="sizeResolver">Resolves the full size.</param>
internal sealed class RectShape2DResolver(IPropertyResolver sizeResolver) : ShapeResolverBase2D
{
	private readonly IPropertyResolver _sizeResolver = sizeResolver;

	protected override Shape2D CreateShape()
	{
		return new RectangleShape2D();
	}

	protected override void UpdateShape(Shape2D shape, GraphContext graphContext)
	{
		NumericsVector2 size = _sizeResolver.Resolve(graphContext).AsVector2();
		((RectangleShape2D)shape).Size = new Vector2(Mathf.Abs(size.X), Mathf.Abs(size.Y));
	}
}
