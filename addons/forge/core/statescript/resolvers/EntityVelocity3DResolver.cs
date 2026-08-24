// Copyright © Gamesmiths Guild.

using System;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;
using NumericsVector3 = System.Numerics.Vector3;

namespace Gamesmiths.Forge.Godot.Core.Statescript.Resolvers;

/// <summary>
/// Resolves the linear velocity of the body an entity lives on.
/// </summary>
/// <remarks>
/// Reads <see cref="CharacterBody3D.Velocity"/> or <see cref="RigidBody3D.LinearVelocity"/>, whichever the node is.
/// Anything else has no velocity of its own and resolves to zero, since a plain <see cref="Node3D"/> moved by animation
/// or by a tween does not record one.
/// </remarks>
/// <param name="entityResolver">Resolves which entity to read.</param>
/// <param name="nodePath">Optional path to a descendant node to read instead.</param>
internal sealed class EntityVelocity3DResolver(IEntityResolver entityResolver, string nodePath)
	: SpatialResolverBase3D(entityResolver, nodePath)
{
	public override Type ValueType => typeof(NumericsVector3);

	protected override Variant128 ResolveFrom(Node3D spatialNode, GraphContext graphContext)
	{
		Vector3 velocity = spatialNode switch
		{
			CharacterBody3D characterBody => characterBody.Velocity,
			RigidBody3D rigidBody => rigidBody.LinearVelocity,
			_ => Vector3.Zero,
		};

		return new Variant128(new NumericsVector3(velocity.X, velocity.Y, velocity.Z));
	}
}
