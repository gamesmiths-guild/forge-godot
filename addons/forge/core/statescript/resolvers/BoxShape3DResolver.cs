// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;
using NumericsVector3 = System.Numerics.Vector3;

namespace Gamesmiths.Forge.Godot.Core.Statescript.Resolvers;

/// <summary>
/// Resolves a box of a given size.
/// </summary>
/// <remarks>
/// The size is the box's full width, height and depth, matching Godot's own <see cref="BoxShape3D.Size"/> rather than
/// the half-extents older engines used, so a number typed here means the same thing it means in the inspector.
/// </remarks>
/// <param name="sizeResolver">Resolves the full size.</param>
internal sealed class BoxShape3DResolver(IPropertyResolver sizeResolver) : ShapeResolverBase3D
{
	private readonly IPropertyResolver _sizeResolver = sizeResolver;

	protected override Shape3D CreateShape()
	{
		return new BoxShape3D();
	}

	protected override void UpdateShape(Shape3D shape, GraphContext graphContext)
	{
		NumericsVector3 size = _sizeResolver.Resolve(graphContext).AsVector3();
		((BoxShape3D)shape).Size = new Vector3(Mathf.Abs(size.X), Mathf.Abs(size.Y), Mathf.Abs(size.Z));
	}
}
