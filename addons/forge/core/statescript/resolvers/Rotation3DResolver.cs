// Copyright © Gamesmiths Guild.

using System;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;
using NumericsQuaternion = System.Numerics.Quaternion;

namespace Gamesmiths.Forge.Godot.Core.Statescript.Resolvers;

/// <summary>
/// Resolves the rotation of the 3D node an entity lives on.
/// </summary>
/// <param name="entityResolver">Resolves which entity to read.</param>
/// <param name="nodePath">Optional path to a descendant node to read instead.</param>
/// <param name="space">Whether to report world or parent-relative rotation.</param>
internal sealed class Rotation3DResolver(IEntityResolver entityResolver, string nodePath, TransformSpace space)
	: SpatialResolverBase3D(entityResolver, nodePath)
{
	private readonly TransformSpace _space = space;

	public override Type ValueType => typeof(NumericsQuaternion);

	protected override Variant128 ResolveFrom(Node3D spatialNode, GraphContext graphContext)
	{
		Quaternion rotation = _space == TransformSpace.Local
			? spatialNode.Quaternion
			: spatialNode.GlobalBasis.GetRotationQuaternion();

		return new Variant128(new NumericsQuaternion(rotation.X, rotation.Y, rotation.Z, rotation.W));
	}
}
