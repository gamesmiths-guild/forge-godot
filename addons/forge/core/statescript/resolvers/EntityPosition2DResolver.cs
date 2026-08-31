// Copyright © Gamesmiths Guild.

using System;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;
using NumericsVector2 = System.Numerics.Vector2;

namespace Gamesmiths.Forge.Godot.Core.Statescript.Resolvers;

/// <summary>
/// Resolves the position of the 2D node an entity lives on.
/// </summary>
/// <param name="entityResolver">Resolves which entity to read.</param>
/// <param name="nodePath">Optional path to a descendant node to read instead.</param>
/// <param name="space">Whether to report world or parent-relative position.</param>
internal sealed class EntityPosition2DResolver(IEntityResolver entityResolver, string nodePath, TransformSpace space)
	: SpatialResolverBase2D(entityResolver, nodePath)
{
	private readonly TransformSpace _space = space;

	public override Type ValueType => typeof(NumericsVector2);

	protected override Variant128 ResolveFrom(Node2D spatialNode, GraphContext graphContext)
	{
		Vector2 position = _space == TransformSpace.Local ? spatialNode.Position : spatialNode.GlobalPosition;
		return new Variant128(new NumericsVector2(position.X, position.Y));
	}
}
