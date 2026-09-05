// Copyright © Gamesmiths Guild.

using System.Collections.Generic;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Godot.Core.Statescript.Physics;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;
using NumericsQuaternion = System.Numerics.Quaternion;
using NumericsVector3 = System.Numerics.Vector3;

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
/// Centring on someone is spelled out with an Entity Position 3D resolver, which is what a fresh one starts as.</para>
/// <para>Nothing here filters. A team check, a health threshold or a tag requirement is a Where over this array.</para>
/// </remarks>
/// <param name="shapeResolver">Resolves the shape to sweep.</param>
/// <param name="positionResolver">Resolves where the shape sits.</param>
/// <param name="rotationResolver">Resolves how the shape is turned, or <see langword="null"/> to leave it upright.
/// </param>
/// <param name="maskResolver">Resolves the physics layers the query can find. Zero means every layer.</param>
/// <param name="includeAreas">Whether areas count as overlaps, as well as bodies.</param>
/// <param name="ignoreResolver">Resolves the entities left out of the results, or <see langword="null"/> to leave out
/// nothing.</param>
internal sealed class Overlap3DResolver(
	IObjectResolver<Shape3D> shapeResolver,
	IPropertyResolver positionResolver,
	IPropertyResolver? rotationResolver,
	IPropertyResolver? maskResolver,
	bool includeAreas,
	IObjectArrayResolver? ignoreResolver) : ObjectArrayResolver<IForgeEntity>
{
	private readonly IObjectResolver<Shape3D> _shapeResolver = shapeResolver;
	private readonly IPropertyResolver _positionResolver = positionResolver;
	private readonly IPropertyResolver? _rotationResolver = rotationResolver;
	private readonly IPropertyResolver? _maskResolver = maskResolver;
	private readonly bool _includeAreas = includeAreas;
	private readonly IObjectArrayResolver? _ignoreResolver = ignoreResolver;
	private readonly HashSet<IForgeEntity> _found = [];

	public override IForgeEntity[] ResolveArray(GraphContext graphContext)
	{
		World3D? world = PhysicsQuery3D.ResolveWorld(graphContext);
		Shape3D? shape = _shapeResolver.Resolve(graphContext);

		if (world is null || shape is null || !GodotObject.IsInstanceValid(shape))
		{
			return [];
		}

		Transform3D transform = ResolveTransform(graphContext);

		// The set is kept between resolves so a per-tick read does not allocate one each time. Forge runs its graphs on
		// one thread, and the contents never outlive the copy returned below.
		_found.Clear();

		PhysicsQuery3D.CollectShapeOverlaps(
			world,
			shape,
			transform,
			PhysicsQuery3D.ResolveMask(ResolveMaskValue(graphContext)),
			_includeAreas,
			_ignoreResolver?.ResolveArray(graphContext),
			_found);

		Color color = _found.Count > 0
			? PhysicsDebugDraw3D.OverlapFoundColor
			: PhysicsDebugDraw3D.OverlapEmptyColor;

		PhysicsDebugDraw3D.FlashShape(graphContext, shape, transform, color);
		PhysicsDebugDraw3D.FlashTargets(graphContext, _found, color);

		var resolved = new IForgeEntity[_found.Count];
		_found.CopyTo(resolved);
		return resolved;
	}

	private int ResolveMaskValue(GraphContext graphContext)
	{
		return _maskResolver is null ? 0 : (int)_maskResolver.Resolve(graphContext).AsDouble();
	}

	private Transform3D ResolveTransform(GraphContext graphContext)
	{
		NumericsVector3 origin = _positionResolver.Resolve(graphContext).AsVector3();
		var position = new Vector3(origin.X, origin.Y, origin.Z);

		if (_rotationResolver is null)
		{
			return new Transform3D(Basis.Identity, position);
		}

		NumericsQuaternion rotation = _rotationResolver.Resolve(graphContext).AsQuaternion();

		// A zero quaternion is what an unfilled rotation operand resolves to, and Godot rejects one outright rather
		// than treating it as no rotation.
		var godotRotation = new Quaternion(rotation.X, rotation.Y, rotation.Z, rotation.W);

		return godotRotation.LengthSquared() <= 0.000001f
			? new Transform3D(Basis.Identity, position)
			: new Transform3D(new Basis(godotRotation.Normalized()), position);
	}
}
