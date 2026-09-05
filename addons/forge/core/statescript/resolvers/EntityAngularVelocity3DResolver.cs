// Copyright © Gamesmiths Guild.

using System;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;
using NumericsVector3 = System.Numerics.Vector3;

namespace Gamesmiths.Forge.Godot.Core.Statescript.Resolvers;

/// <summary>
/// Resolves how fast the body an entity lives on is spinning, in radians per second.
/// </summary>
/// <remarks>
/// <para>The angular counterpart of Entity Velocity 3D, and the reason a graph can react to a spin at all: how fast
/// something is tumbling decides whether a grab lands, whether a thrown object shatters, whether a vehicle has spun
/// out.</para>
/// <para>Only a <see cref="RigidBody3D"/> has one. A <see cref="CharacterBody3D"/> carries a linear velocity and no
/// angular one - Godot never turns it, the game does - so it resolves to zero here where it resolves to something real
/// in the linear twin. That asymmetry is the engine's, not this layer's.</para>
/// <para>The vector is an axis with the rate as its length, which is what makes core's Length the spin rate and
/// Normalize the axis it is spinning about.</para>
/// </remarks>
/// <param name="entityResolver">Resolves which entity to read.</param>
/// <param name="nodePath">Optional path to a descendant node to read instead.</param>
internal sealed class EntityAngularVelocity3DResolver(IEntityResolver entityResolver, string nodePath)
	: SpatialResolverBase3D(entityResolver, nodePath)
{
	public override Type ValueType => typeof(NumericsVector3);

	protected override Variant128 ResolveFrom(Node3D spatialNode, GraphContext graphContext)
	{
		if (spatialNode is not RigidBody3D rigidBody)
		{
			if (NodePath.Length > 0)
			{
				ReportUnusableNodeOnce(
					$"resolved to a {spatialNode.GetType().Name} at [{NodePath}], which has no angular velocity to" +
					" read - only a RigidBody3D does. Resolving to zero.");
			}

			return new Variant128(NumericsVector3.Zero);
		}

		Vector3 angularVelocity = rigidBody.AngularVelocity;
		return new Variant128(new NumericsVector3(angularVelocity.X, angularVelocity.Y, angularVelocity.Z));
	}
}
