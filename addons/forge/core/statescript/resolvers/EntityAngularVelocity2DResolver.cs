// Copyright © Gamesmiths Guild.

using System;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;

namespace Gamesmiths.Forge.Godot.Core.Statescript.Resolvers;

/// <summary>
/// Resolves how fast the body an entity lives on is spinning, in radians per second.
/// </summary>
/// <remarks>
/// <para>The angular counterpart of Entity Velocity 2D, and the reason a graph can react to a spin at all: how fast
/// something is tumbling decides whether a grab lands, whether a thrown object shatters, whether a vehicle has spun
/// out.</para>
/// <para>It reports a <b>float</b> where its 3D twin reports a vector, for the same reason Entity Rotation 2D does: a
/// plane has one axis to turn around, so a spin is a rate and not an axis with a rate. Core's whole numeric toolbox
/// applies to it directly, and its sign is which way round it is going.</para>
/// <para>Only a <see cref="RigidBody2D"/> has one. A <see cref="CharacterBody2D"/> carries a linear velocity and no
/// angular one - Godot never turns it, the game does - so it resolves to zero here where it resolves to something real
/// in the linear twin.</para>
/// </remarks>
/// <param name="entityResolver">Resolves which entity to read.</param>
/// <param name="nodePath">Optional path to a descendant node to read instead.</param>
internal sealed class EntityAngularVelocity2DResolver(IEntityResolver entityResolver, string nodePath)
	: SpatialResolverBase2D(entityResolver, nodePath)
{
	public override Type ValueType => typeof(double);

	protected override Variant128 ResolveFrom(Node2D spatialNode, GraphContext graphContext)
	{
		if (spatialNode is not RigidBody2D rigidBody)
		{
			if (NodePath.Length > 0)
			{
				ReportUnusableNodeOnce(
					$"resolved to a {spatialNode.GetType().Name} at [{NodePath}], which has no angular velocity to" +
					" read - only a RigidBody2D does. Resolving to zero.");
			}

			return new Variant128(0.0);
		}

		return new Variant128((double)rigidBody.AngularVelocity);
	}
}
