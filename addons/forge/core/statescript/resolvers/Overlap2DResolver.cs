// Copyright © Gamesmiths Guild.

using System.Collections.Generic;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Godot.Core.Statescript.Physics;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;
using NumericsVector2 = System.Numerics.Vector2;

namespace Gamesmiths.Forge.Godot.Core.Statescript.Resolvers;

/// <summary>
/// Resolves the entities inside a shape swept through the world at query time.
/// </summary>
/// <remarks>
/// <para>This is the area of effect that has no scene node behind it: a ground-targeted blast at the point the player
/// clicked, a cleave around the caster whose radius comes from an attribute. The shape, where it sits and how it is
/// turned are all resolvers, so every one of them can scale or track at runtime.</para>
/// <para>The position is a required operand rather than one that falls back to the entity, because a nested operand
/// always resolves to something - an unfilled one is the constant zero, which is the world origin and not the caster.
/// Centring on someone is spelled out with an Entity Position 2D resolver, which is what a fresh one starts as.</para>
/// <para>Nothing here filters. A team check, a health threshold or a tag requirement is a Where over this array.</para>
/// </remarks>
/// <param name="shapeResolver">Resolves the shape to sweep.</param>
/// <param name="positionResolver">Resolves where the shape sits.</param>
/// <param name="rotationResolver">Resolves how the shape is turned, in radians, or <see langword="null"/> to leave it
/// unturned.</param>
/// <param name="maskResolver">Resolves the physics layers the query can find. Zero means every layer.</param>
/// <param name="includeAreas">Whether areas count as overlaps, as well as bodies.</param>
/// <param name="ignoreResolver">Resolves the entities left out of the results, or <see langword="null"/> to leave out
/// nothing.</param>
internal sealed class Overlap2DResolver(
	IObjectResolver<Shape2D> shapeResolver,
	IPropertyResolver positionResolver,
	IPropertyResolver? rotationResolver,
	IPropertyResolver? maskResolver,
	bool includeAreas,
	IObjectArrayResolver? ignoreResolver) : ObjectArrayResolver<IForgeEntity>
{
	private readonly IObjectResolver<Shape2D> _shapeResolver = shapeResolver;
	private readonly IPropertyResolver _positionResolver = positionResolver;
	private readonly IPropertyResolver? _rotationResolver = rotationResolver;
	private readonly IPropertyResolver? _maskResolver = maskResolver;
	private readonly bool _includeAreas = includeAreas;
	private readonly IObjectArrayResolver? _ignoreResolver = ignoreResolver;
	private readonly HashSet<IForgeEntity> _found = [];

	public override IForgeEntity[] ResolveArray(GraphContext graphContext)
	{
		World2D? world = PhysicsQuery2D.ResolveWorld(graphContext);
		Shape2D? shape = _shapeResolver.Resolve(graphContext);

		if (world is null || shape is null || !GodotObject.IsInstanceValid(shape))
		{
			return [];
		}

		Transform2D transform = ResolveTransform(graphContext);

		// The set is kept between resolves so a per-tick read does not allocate one each time. Forge runs its graphs on
		// one thread, and the contents never outlive the copy returned below.
		_found.Clear();

		PhysicsQuery2D.CollectShapeOverlaps(
			world,
			shape,
			transform,
			PhysicsQuery2D.ResolveMask(ResolveMaskValue(graphContext)),
			_includeAreas,
			_ignoreResolver?.ResolveArray(graphContext),
			_found);

		Color color = _found.Count > 0
			? PhysicsDebugDraw2D.OverlapFoundColor
			: PhysicsDebugDraw2D.OverlapEmptyColor;

		PhysicsDebugDraw2D.FlashShape(graphContext, shape, transform, color);
		PhysicsDebugDraw2D.FlashTargets(graphContext, _found, color);

		var resolved = new IForgeEntity[_found.Count];
		_found.CopyTo(resolved);
		return resolved;
	}

	private int ResolveMaskValue(GraphContext graphContext)
	{
		return _maskResolver is null ? 0 : (int)_maskResolver.Resolve(graphContext).AsDouble();
	}

	private Transform2D ResolveTransform(GraphContext graphContext)
	{
		NumericsVector2 origin = _positionResolver.Resolve(graphContext).AsVector2();
		var position = new Vector2(origin.X, origin.Y);

		// An unfilled rotation operand resolves to zero, which in 2D is simply "unturned" - there is no zero quaternion
		// to guard against here.
		float rotation = _rotationResolver is null
			? 0.0f
			: (float)_rotationResolver.Resolve(graphContext).AsDouble();

		return new Transform2D(rotation, position);
	}
}
