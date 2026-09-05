// Copyright © Gamesmiths Guild.

using System;
using Gamesmiths.Forge.Godot.Core.Statescript.Physics;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;
using NumericsVector3 = System.Numerics.Vector3;

namespace Gamesmiths.Forge.Godot.Core.Statescript.Resolvers;

/// <summary>
/// Resolves whether a body would fit at a destination.
/// </summary>
/// <remarks>
/// <para>The guard the non-solving movement nodes need. Set Position 3D and Move To 3D drive the transform and pass
/// through geometry on purpose, which makes "can the character actually be there" a question the graph has to ask
/// before it blinks somebody into a wall.</para>
/// <para><b>It tests the body's own collision shapes</b>, which is what makes it a better answer here than a
/// Shapecast. A sweep can only test a shape somebody authored into it, and keeping that shape in sync with the
/// character it stands for is exactly the duplication that goes stale the first time an artist changes a capsule.
/// </para>
/// <para>It asks about the destination rather than the path. A blink is meant to skip what is in between, so this
/// reports whether the far end is clear, not whether the journey is — Shapecast 3D is the resolver for the journey.
/// </para>
/// <para>Only a <see cref="PhysicsBody3D"/> can be tested; an entity that is not one resolves to true, since a thing
/// with no collider cannot fail to fit anywhere.</para>
/// </remarks>
/// <param name="entityResolver">Resolves whose body to test.</param>
/// <param name="nodePath">Optional path to a descendant node to test instead.</param>
/// <param name="destinationResolver">Resolves where the body would be.</param>
internal sealed class CanFit3DResolver(
	IEntityResolver entityResolver,
	string nodePath,
	IPropertyResolver destinationResolver) : SpatialResolverBase3D(entityResolver, nodePath)
{
	private readonly IPropertyResolver _destinationResolver = destinationResolver;

	public override Type ValueType => typeof(bool);

	protected override Variant128 ResolveFrom(Node3D spatialNode, GraphContext graphContext)
	{
		if (spatialNode is not PhysicsBody3D body)
		{
			ReportUnusableNodeOnce(
				$"resolved to a {spatialNode.GetType().Name}, which has no collision shapes to test - only a" +
				" PhysicsBody3D does. Resolving to true.");

			return new Variant128(true);
		}

		NumericsVector3 target = _destinationResolver.Resolve(graphContext).AsVector3();

		// Tested as a body already standing at the destination and moving nowhere, which is what "would it fit here"
		// means. Godot's own check takes a motion, and passing the trip from here to there would answer the different
		// question of whether the character could walk it.
		var destination = new Transform3D(
			body.GlobalBasis,
			new Vector3(target.X, target.Y, target.Z));

		bool fits = PhysicsQuery3D.CanFit(body, destination, Vector3.Zero);

		// One colour for the line and the outline, because they are one answer.
		Color color = fits ? PhysicsDebugDraw3D.SightClearColor : PhysicsDebugDraw3D.SightBlockedColor;

		PhysicsDebugDraw3D.FlashLine(graphContext, body.GlobalPosition, destination.Origin, color);
		PhysicsDebugDraw3D.FlashBody(graphContext, body, destination, color);

		return new Variant128(fits);
	}
}
