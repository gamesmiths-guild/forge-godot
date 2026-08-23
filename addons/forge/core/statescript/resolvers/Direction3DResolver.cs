// Copyright © Gamesmiths Guild.

using System;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;
using NumericsVector3 = System.Numerics.Vector3;

namespace Gamesmiths.Forge.Godot.Core.Statescript.Resolvers;

/// <summary>
/// Resolves one of the facing directions of the 3D node an entity lives on, as a unit vector in world space.
/// </summary>
/// <remarks>
/// This is the "which way is it looking" primitive: feed it to a projectile's spawn rotation, a dash velocity, or a
/// cone query. Godot's forward is −Z, which this resolver hides behind <see cref="SpatialAxis.Forward"/>.
/// </remarks>
/// <param name="entityResolver">Resolves which entity to read.</param>
/// <param name="nodePath">Optional path to a descendant node to read instead.</param>
/// <param name="axis">Which of the node's own directions to report.</param>
internal sealed class Direction3DResolver(IEntityResolver entityResolver, string nodePath, SpatialAxis axis)
	: SpatialResolverBase3D(entityResolver, nodePath)
{
	private readonly SpatialAxis _axis = axis;

	public override Type ValueType => typeof(NumericsVector3);

	protected override Variant128 ResolveFrom(Node3D spatialNode, GraphContext graphContext)
	{
		Basis basis = spatialNode.GlobalBasis;

		Vector3 direction = _axis switch
		{
			SpatialAxis.Forward => -basis.Z,
			SpatialAxis.Back => basis.Z,
			SpatialAxis.Right => basis.X,
			SpatialAxis.Left => -basis.X,
			SpatialAxis.Up => basis.Y,
			SpatialAxis.Down => -basis.Y,
			_ => -basis.Z,
		};

		direction = direction.Normalized();
		return new Variant128(new NumericsVector3(direction.X, direction.Y, direction.Z));
	}
}
