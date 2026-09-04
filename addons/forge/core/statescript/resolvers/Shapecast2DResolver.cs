// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Godot.Core.Statescript.Physics;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;
using GodotNode = Godot.Node;
using GodotRidArray = Godot.Collections.Array<Godot.Rid>;
using NumericsVector2 = System.Numerics.Vector2;

namespace Gamesmiths.Forge.Godot.Core.Statescript.Resolvers;

/// <summary>
/// Resolves the first entity a shape swept through the world meets.
/// </summary>
/// <remarks>
/// <para>A ray is a line with no thickness, which is why a fast projectile fired at a thin target misses it and why a
/// dash checked with a ray walks a character's shoulders into a wall. This sweeps a real volume along the same line
/// and reports what stops it first, so the check has the width the thing being checked has.</para>
/// <para>It answers with one entity rather than an array on purpose: the question a sweep asks is what is in the way,
/// and everything behind that is not. An area of effect along a line is an Overlap 2D with a capsule, which is the
/// same shape asked a different question.</para>
/// <para>The ignore operand drops the bodies the sweep should pass through, the same as it does for a ray. It reads
/// differently here, though: a swept volume that starts inside a body is reported at zero distance rather than
/// stopping the sweep short of everything behind it, because the sweep and the initial-overlap test are two separate
/// questions. Leaving the caster in the list therefore makes a cast from its own origin answer "the caster", every
/// time.</para>
/// <para>The rotation is an angle in radians rather than a quaternion, and it is read as authored: an unfilled angle
/// is zero, which means unturned, so there is no guard to write.</para>
/// </remarks>
/// <param name="shapeResolver">Resolves the shape to sweep.</param>
/// <param name="originResolver">Resolves where the sweep starts.</param>
/// <param name="directionResolver">Resolves which way it goes.</param>
/// <param name="maxDistanceResolver">Resolves how far it reaches.</param>
/// <param name="rotationResolver">Resolves how the shape is turned, in radians, or <see langword="null"/> to leave it
/// unturned.</param>
/// <param name="maskResolver">Resolves the physics layers the sweep can hit. Zero means every layer.</param>
/// <param name="collideWithAreas">Whether areas stop the sweep, as well as bodies.</param>
/// <param name="ignoreResolver">Resolves the entities the sweep passes through, or <see langword="null"/> to pass
/// through nothing.</param>
internal sealed class Shapecast2DResolver(
	IObjectResolver<Shape2D> shapeResolver,
	IPropertyResolver originResolver,
	IPropertyResolver directionResolver,
	IPropertyResolver maxDistanceResolver,
	IPropertyResolver? rotationResolver,
	IPropertyResolver? maskResolver,
	bool collideWithAreas,
	IObjectArrayResolver? ignoreResolver) : ObjectResolver<IForgeEntity>, IEntityResolver
{
	private readonly IObjectResolver<Shape2D> _shapeResolver = shapeResolver;
	private readonly IPropertyResolver _originResolver = originResolver;
	private readonly IPropertyResolver _directionResolver = directionResolver;
	private readonly IPropertyResolver _maxDistanceResolver = maxDistanceResolver;
	private readonly IPropertyResolver? _rotationResolver = rotationResolver;
	private readonly IPropertyResolver? _maskResolver = maskResolver;
	private readonly bool _collideWithAreas = collideWithAreas;
	private readonly IObjectArrayResolver? _ignoreResolver = ignoreResolver;
	private readonly GodotRidArray _exclusions = [];

	public override IForgeEntity? Resolve(GraphContext graphContext)
	{
		World2D? world = PhysicsQuery2D.ResolveWorld(graphContext);
		Shape2D? shape = _shapeResolver.Resolve(graphContext);

		if (world is null || shape is null || !GodotObject.IsInstanceValid(shape))
		{
			return null;
		}

		NumericsVector2 originValue = _originResolver.Resolve(graphContext).AsVector2();
		NumericsVector2 directionValue = _directionResolver.Resolve(graphContext).AsVector2();
		float maxDistance = (float)_maxDistanceResolver.Resolve(graphContext).AsDouble();

		var origin = new Vector2(originValue.X, originValue.Y);
		var direction = new Vector2(directionValue.X, directionValue.Y);

		if (!PhysicsQuery2D.IsCastable(direction, maxDistance))
		{
			return null;
		}

		Vector2 motion = direction.Normalized() * maxDistance;
		var transform = new Transform2D(ResolveRotation(graphContext), origin);

		bool hasExclusions =
			PhysicsQuery2D.TryCollectExclusions(_ignoreResolver?.ResolveArray(graphContext), _exclusions);

		bool hit = PhysicsQuery2D.TryShapecast(
			world,
			shape,
			transform,
			motion,
			PhysicsQuery2D.ResolveMask(ResolveMaskValue(graphContext)),
			_collideWithAreas,
			hasExclusions ? _exclusions : null,
			out Transform2D hitTransform,
			out GodotNode? collider);

		// The shape is drawn where the sweep came to rest and the line runs the sweep's full reach, so a hit reads as
		// the shape stopped part way along its own path. A miss draws both at the far end, which tells an obstacle
		// that was never going to be met apart from one that is not there.
		PhysicsDebugDraw2D.FlashShapecast(
			graphContext,
			shape,
			hitTransform,
			origin,
			origin + motion,
			hit ? PhysicsDebugDraw2D.RayHitColor : PhysicsDebugDraw2D.RayClearColor);

		return hit && ForgeEntityBridge.TryGetEntityInHierarchy(collider, out IForgeEntity? entity) ? entity : null;
	}

	private float ResolveRotation(GraphContext graphContext)
	{
		return _rotationResolver is null ? 0.0f : (float)_rotationResolver.Resolve(graphContext).AsDouble();
	}

	private int ResolveMaskValue(GraphContext graphContext)
	{
		return _maskResolver is null ? 0 : (int)_maskResolver.Resolve(graphContext).AsDouble();
	}
}
