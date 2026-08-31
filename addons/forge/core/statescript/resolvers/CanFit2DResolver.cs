// Copyright © Gamesmiths Guild.

using System;
using Gamesmiths.Forge.Godot.Core.Statescript.Physics;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;
using NumericsVector2 = System.Numerics.Vector2;

namespace Gamesmiths.Forge.Godot.Core.Statescript.Resolvers;

/// <summary>
/// Resolves whether a body would fit at a destination.
/// </summary>
/// <remarks>
/// <para>The guard the non-solving movement nodes need. Set Position 2D and Move To 2D drive the transform and pass
/// through geometry on purpose, which makes "can the character actually be there" a question the graph has to ask
/// before it blinks somebody into a wall.</para>
/// <para><b>It tests the body's own collision shapes</b>, which is what makes it a better answer here than a
/// Shapecast. A sweep can only test a shape somebody authored into it, and keeping that shape in sync with the
/// character it stands for is exactly the duplication that goes stale the first time an artist changes a capsule.
/// </para>
/// <para>It asks about the destination rather than the path. A blink is meant to skip what is in between, so this
/// reports whether the far end is clear, not whether the journey is — Shapecast 2D is the resolver for the journey.
/// </para>
/// <para>Only a <see cref="PhysicsBody2D"/> can be tested; an entity that is not one resolves to true, since a thing
/// with no collider cannot fail to fit anywhere.</para>
/// </remarks>
/// <param name="entityResolver">Resolves whose body to test.</param>
/// <param name="nodePath">Optional path to a descendant node to test instead.</param>
/// <param name="destinationResolver">Resolves where the body would be.</param>
internal sealed class CanFit2DResolver(
	IEntityResolver entityResolver,
	string nodePath,
	IPropertyResolver destinationResolver) : SpatialResolverBase2D(entityResolver, nodePath)
{
	private readonly IPropertyResolver _destinationResolver = destinationResolver;

	public override Type ValueType => typeof(bool);

	protected override Variant128 ResolveFrom(Node2D spatialNode, GraphContext graphContext)
	{
		if (spatialNode is not PhysicsBody2D body)
		{
			ReportUnusableNodeOnce(
				$"resolved to a {spatialNode.GetType().Name}, which has no collision shapes to test - only a" +
				" PhysicsBody2D does. Resolving to true.");

			return new Variant128(true);
		}

		NumericsVector2 target = _destinationResolver.Resolve(graphContext).AsVector2();

		// Tested as a body already standing at the destination and moving nowhere, which is what "would it fit here"
		// means. Godot's own check takes a motion, and passing the trip from here to there would answer the different
		// question of whether the character could walk it.
		Transform2D destination = body.GlobalTransform;
		destination.Origin = new Vector2(target.X, target.Y);

		bool fits = PhysicsQuery2D.CanFit(body, destination, Vector2.Zero);

		// One colour for the line and the outline, because they are one answer.
		Color color = fits ? PhysicsDebugDraw2D.SightClearColor : PhysicsDebugDraw2D.SightBlockedColor;

		PhysicsDebugDraw2D.FlashLine(graphContext, body.GlobalPosition, destination.Origin, color);
		PhysicsDebugDraw2D.FlashBody(graphContext, body, destination, color);

		return new Variant128(fits);
	}
}
