// Copyright © Gamesmiths Guild.

using System;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;
using NumericsVector2 = System.Numerics.Vector2;

namespace Gamesmiths.Forge.Godot.Core.Statescript.Resolvers;

/// <summary>
/// Converts a point between an entity's local space and world space.
/// </summary>
/// <remarks>
/// This is the generic offset primitive, and the reason Forge needs no concept of a cast point or a muzzle: an offset
/// of <c>(24, 0)</c> is "twenty-four pixels in front of them" and <c>(0, -32)</c> is "thirty-two above", both following
/// the entity as it moves and turns. Inverted, it answers the opposite question: where a world point sits relative to
/// the entity.
/// </remarks>
/// <param name="entityResolver">Resolves which entity the point is relative to.</param>
/// <param name="nodePath">Optional path to a descendant node to use as the frame of reference.</param>
/// <param name="offsetResolver">Resolves the point to convert.</param>
/// <param name="inverse">When set, converts world to local instead of local to world.</param>
internal sealed class EntityTransformPoint2DResolver(
	IEntityResolver entityResolver,
	string nodePath,
	IPropertyResolver offsetResolver,
	bool inverse) : SpatialResolverBase2D(entityResolver, nodePath)
{
	private readonly IPropertyResolver _offsetResolver = offsetResolver;
	private readonly bool _inverse = inverse;

	public override Type ValueType => typeof(NumericsVector2);

	protected override Variant128 ResolveFrom(Node2D spatialNode, GraphContext graphContext)
	{
		NumericsVector2 offset = _offsetResolver.Resolve(graphContext).AsVector2();
		var point = new Vector2(offset.X, offset.Y);

		Vector2 result = _inverse
			? spatialNode.GlobalTransform.AffineInverse() * point
			: spatialNode.GlobalTransform * point;

		return new Variant128(new NumericsVector2(result.X, result.Y));
	}
}
