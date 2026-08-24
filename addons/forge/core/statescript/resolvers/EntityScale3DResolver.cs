// Copyright © Gamesmiths Guild.

using System;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;
using NumericsVector3 = System.Numerics.Vector3;

namespace Gamesmiths.Forge.Godot.Core.Statescript.Resolvers;

/// <summary>
/// Resolves the scale of the 3D node an entity lives on.
/// </summary>
/// <param name="entityResolver">Resolves which entity to read.</param>
/// <param name="nodePath">Optional path to a descendant node to read instead.</param>
/// <param name="space">Whether to report world or parent-relative scale.</param>
internal sealed class EntityScale3DResolver(IEntityResolver entityResolver, string nodePath, TransformSpace space)
	: SpatialResolverBase3D(entityResolver, nodePath)
{
	private readonly TransformSpace _space = space;

	public override Type ValueType => typeof(NumericsVector3);

	protected override Variant128 ResolveFrom(Node3D spatialNode, GraphContext graphContext)
	{
		Vector3 scale = _space == TransformSpace.Local ? spatialNode.Scale : spatialNode.GlobalBasis.Scale;
		return new Variant128(new NumericsVector3(scale.X, scale.Y, scale.Z));
	}
}
