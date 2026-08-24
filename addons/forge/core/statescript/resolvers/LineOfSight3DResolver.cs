// Copyright © Gamesmiths Guild.

using System;
using Gamesmiths.Forge.Godot.Core.Statescript.Physics;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;
using GodotRidArray = Godot.Collections.Array<Godot.Rid>;
using NumericsVector3 = System.Numerics.Vector3;

namespace Gamesmiths.Forge.Godot.Core.Statescript.Resolvers;

/// <summary>
/// Resolves whether nothing blocks the line between two points.
/// </summary>
/// <remarks>
/// <para>This is a resolver rather than a node because its whole point is composing: a Where over an overlap query that
/// drops the enemies behind a wall, a Condition Monitor that ends a channel when the line breaks. A node could not be
/// used inside either. Line Of Sight 3D as a State node is the node form, for graphs that want to react to the line
/// breaking rather than ask about it.</para>
/// <para>It takes two <em>points</em>, not two entities. An entity's position is an Entity Position 3D resolver away,
/// and points are strictly more: a sight check to where the player clicked, or from a turret's muzzle to a predicted
/// intercept, has no entity to name at either end.</para>
/// <para>The ignore operand is what keeps the line off the bodies at its own ends, and it is a list because both ends
/// have that problem. A character's origin sits at its feet, so a line starting there grazes the bottom of its own
/// capsule; a line drawn to a marker inside someone is stopped by the body that marker belongs to. Either one reports
/// itself as cover.</para>
/// </remarks>
/// <param name="fromResolver">Resolves where the line starts.</param>
/// <param name="toResolver">Resolves where the line ends.</param>
/// <param name="ignoreResolver">Resolves the entities the line passes through, or <see langword="null"/> to ignore
/// nothing.</param>
/// <param name="maskResolver">Resolves the physics layers that block sight. Zero means every layer.</param>
internal sealed class LineOfSight3DResolver(
	IPropertyResolver fromResolver,
	IPropertyResolver toResolver,
	IObjectArrayResolver? ignoreResolver,
	IPropertyResolver? maskResolver) : IPropertyResolver
{
	private readonly IPropertyResolver _fromResolver = fromResolver;
	private readonly IPropertyResolver _toResolver = toResolver;
	private readonly IObjectArrayResolver? _ignoreResolver = ignoreResolver;
	private readonly IPropertyResolver? _maskResolver = maskResolver;
	private readonly GodotRidArray _exclusions = [];

	public Type ValueType => typeof(bool);

	public Variant128 Resolve(GraphContext graphContext)
	{
		World3D? world = PhysicsQuery3D.ResolveWorld(graphContext);

		if (world is null)
		{
			return new Variant128(false);
		}

		NumericsVector3 fromValue = _fromResolver.Resolve(graphContext).AsVector3();
		NumericsVector3 toValue = _toResolver.Resolve(graphContext).AsVector3();

		var from = new Vector3(fromValue.X, fromValue.Y, fromValue.Z);
		var to = new Vector3(toValue.X, toValue.Y, toValue.Z);

		bool hasExclusions =
			PhysicsQuery3D.TryCollectExclusions(_ignoreResolver?.ResolveArray(graphContext), _exclusions);

		bool clear = PhysicsQuery3D.TryLineOfSight(
			world,
			from,
			to,
			PhysicsQuery3D.ResolveMask(
				_maskResolver is null ? 0 : (int)_maskResolver.Resolve(graphContext).AsDouble()),
			hasExclusions ? _exclusions : null,
			out RaycastResult3D blocker);

		PhysicsDebugDraw3D.FlashLine(
			graphContext,
			from,
			clear ? to : blocker.Position,
			clear ? PhysicsDebugDraw3D.SightClearColor : PhysicsDebugDraw3D.SightBlockedColor);

		return new Variant128(clear);
	}
}
