// Copyright © Gamesmiths Guild.

using System;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;

namespace Gamesmiths.Forge.Godot.Core.Statescript.Resolvers;

/// <summary>
/// Resolves the rotation of the 2D node an entity lives on, in radians.
/// </summary>
/// <remarks>
/// A number rather than the quaternion its 3D twin reports: a plane has one axis to turn around, so an angle is the
/// whole rotation and core's whole numeric toolbox applies to it directly.
/// </remarks>
/// <param name="entityResolver">Resolves which entity to read.</param>
/// <param name="nodePath">Optional path to a descendant node to read instead.</param>
/// <param name="space">Whether to report world or parent-relative rotation.</param>
internal sealed class EntityRotation2DResolver(IEntityResolver entityResolver, string nodePath, TransformSpace space)
	: SpatialResolverBase2D(entityResolver, nodePath)
{
	private readonly TransformSpace _space = space;

	public override Type ValueType => typeof(float);

	protected override Variant128 ResolveFrom(Node2D spatialNode, GraphContext graphContext)
	{
		return new Variant128(_space == TransformSpace.Local ? spatialNode.Rotation : spatialNode.GlobalRotation);
	}
}
