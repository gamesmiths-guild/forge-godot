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
/// or by a tween does not record one. That is silent for the entity's own node and warned about for an authored path,
/// which was aimed somewhere on purpose.
/// </remarks>
/// <param name="entityResolver">Resolves which entity to read.</param>
/// <param name="nodePath">Optional path to a descendant node to read instead.</param>
internal sealed class EntityVelocity3DResolver(IEntityResolver entityResolver, string nodePath)
	: SpatialResolverBase3D(entityResolver, nodePath)
{
	public override Type ValueType => typeof(NumericsVector3);

	protected override Variant128 ResolveFrom(Node3D spatialNode, GraphContext graphContext)
	{
		Vector3 velocity = Vector3.Zero;

		switch (spatialNode)
		{
			case CharacterBody3D characterBody:
				velocity = characterBody.Velocity;
				break;

			case RigidBody3D rigidBody:
				velocity = rigidBody.LinearVelocity;
				break;

			default:
				if (NodePath.Length > 0)
				{
					ReportUnusableNodeOnce(
						$"resolved to a {spatialNode.GetType().Name} at [{NodePath}], which has no velocity to read." +
						" Point Node at the body, or leave it empty. Resolving to zero.");
				}

				break;
		}

		return new Variant128(new NumericsVector3(velocity.X, velocity.Y, velocity.Z));
	}
}
