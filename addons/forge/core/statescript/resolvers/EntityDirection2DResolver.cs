// Copyright © Gamesmiths Guild.

using System;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;
using NumericsVector2 = System.Numerics.Vector2;

namespace Gamesmiths.Forge.Godot.Core.Statescript.Resolvers;

/// <summary>
/// Resolves one of the facing directions of the 2D node an entity lives on, as a unit vector in world space.
/// </summary>
/// <remarks>
/// This is the "which way is it looking" primitive: feed it to a projectile's spawn rotation, a dash velocity, or a
/// cone query. Godot's 2D forward is +X, which this resolver hides behind <see cref="SpatialAxis2D.Forward"/>.
/// </remarks>
/// <param name="entityResolver">Resolves which entity to read.</param>
/// <param name="nodePath">Optional path to a descendant node to read instead.</param>
/// <param name="axis">Which of the node's own directions to report.</param>
internal sealed class EntityDirection2DResolver(IEntityResolver entityResolver, string nodePath, SpatialAxis2D axis)
	: SpatialResolverBase2D(entityResolver, nodePath)
{
	private readonly SpatialAxis2D _axis = axis;

	public override Type ValueType => typeof(NumericsVector2);

	protected override Variant128 ResolveFrom(Node2D spatialNode, GraphContext graphContext)
	{
		Transform2D transform = spatialNode.GlobalTransform;

		Vector2 direction = _axis switch
		{
			SpatialAxis2D.Back => -transform.X,
			SpatialAxis2D.Right => transform.Y,
			SpatialAxis2D.Left => -transform.Y,
			_ => transform.X,
		};

		direction = direction.Normalized();
		return new Variant128(new NumericsVector2(direction.X, direction.Y));
	}
}
