// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Godot.Core.Statescript.Physics;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;
using GodotRidArray = Godot.Collections.Array<Godot.Rid>;
using NumericsQuaternion = System.Numerics.Quaternion;
using NumericsVector3 = System.Numerics.Vector3;

namespace Gamesmiths.Forge.Godot.Core.Statescript.Resolvers;

/// <summary>
/// Resolves the first entity a shape swept through the world meets.
/// </summary>
/// <remarks>
/// <para>A ray is a line with no thickness, which is why a fast projectile fired at a thin target misses it and why a
/// dash checked with a ray walks a character's shoulders into a wall. This sweeps a real volume along the same line
/// and reports what stops it first, so the check has the width the thing being checked has.</para>
/// <para>It answers with one entity rather than an array on purpose: the question a sweep asks is what is in the way,
/// and everything behind that is not. An area of effect along a line is an Overlap 3D with a capsule, which is the
/// same shape asked a different question.</para>
/// <para>The ignore operand drops the bodies the sweep should pass through, the same as it does for a ray. It reads
/// differently here, though: a swept volume that starts inside a body is reported at zero distance rather than
/// stopping the sweep short of everything behind it, because the sweep and the initial-overlap test are two separate
/// questions. Leaving the caster in the list therefore makes a cast from its own origin answer "the caster", every
/// time.</para>
/// </remarks>
/// <param name="shapeResolver">Resolves the shape to sweep.</param>
/// <param name="originResolver">Resolves where the sweep starts.</param>
/// <param name="directionResolver">Resolves which way it goes.</param>
/// <param name="maxDistanceResolver">Resolves how far it reaches.</param>
/// <param name="rotationResolver">Resolves how the shape is turned, or <see langword="null"/> to leave it upright.
/// </param>
/// <param name="maskResolver">Resolves the physics layers the sweep can hit. Zero means every layer.</param>
/// <param name="collideWithAreas">Whether areas stop the sweep, as well as bodies.</param>
/// <param name="ignoreResolver">Resolves the entities the sweep passes through, or <see langword="null"/> to pass
/// through nothing.</param>
internal sealed class Shapecast3DResolver(
	IObjectResolver<Shape3D> shapeResolver,
	IPropertyResolver originResolver,
	IPropertyResolver directionResolver,
	IPropertyResolver maxDistanceResolver,
	IPropertyResolver? rotationResolver,
	IPropertyResolver? maskResolver,
	bool collideWithAreas,
	IObjectArrayResolver? ignoreResolver) : ObjectResolver<IForgeEntity>, IEntityResolver
{
	private readonly IObjectResolver<Shape3D> _shapeResolver = shapeResolver;
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
		World3D? world = PhysicsQuery3D.ResolveWorld(graphContext);
		Shape3D? shape = _shapeResolver.Resolve(graphContext);

		if (world is null || shape is null || !GodotObject.IsInstanceValid(shape))
		{
			return null;
		}

		NumericsVector3 originValue = _originResolver.Resolve(graphContext).AsVector3();
		NumericsVector3 directionValue = _directionResolver.Resolve(graphContext).AsVector3();
		float maxDistance = (float)_maxDistanceResolver.Resolve(graphContext).AsDouble();

		var origin = new Vector3(originValue.X, originValue.Y, originValue.Z);
		var direction = new Vector3(directionValue.X, directionValue.Y, directionValue.Z);

		if (!PhysicsQuery3D.IsCastable(direction, maxDistance))
		{
			return null;
		}

		Vector3 motion = direction.Normalized() * maxDistance;
		var transform = new Transform3D(ResolveBasis(graphContext), origin);

		bool hasExclusions =
			PhysicsQuery3D.TryCollectExclusions(_ignoreResolver?.ResolveArray(graphContext), _exclusions);

		bool hit = PhysicsQuery3D.TryShapecast(
			world,
			shape,
			transform,
			motion,
			PhysicsQuery3D.ResolveMask(ResolveMaskValue(graphContext)),
			_collideWithAreas,
			hasExclusions ? _exclusions : null,
			out Transform3D hitTransform,
			out RaycastResult3D hitResult);

		// The shape is drawn where the sweep came to rest and the line runs the sweep's full reach, so a hit reads as
		// the shape stopped part way along its own path. A miss draws both at the far end, which tells an obstacle
		// that was never going to be met apart from one that is not there.
		PhysicsDebugDraw3D.FlashShapecast(
			graphContext,
			shape,
			hitTransform,
			origin,
			origin + motion,
			hit ? PhysicsDebugDraw3D.RayHitColor : PhysicsDebugDraw3D.RayClearColor);

		PhysicsDebugDraw3D.FlashTarget(graphContext, hitResult.Entity, PhysicsDebugDraw3D.RayHitColor);

		// Only the entity survives, because a resolver returns one value. Everything else the sweep reported - where
		// it landed, the surface normal, the collider, the distance - is what the Shapecast 3D node exists to give a
		// graph, and is the reason the node form is not merely this resolver with ports.
		return hit ? hitResult.Entity : null;
	}

	private Basis ResolveBasis(GraphContext graphContext)
	{
		if (_rotationResolver is null)
		{
			return Basis.Identity;
		}

		NumericsQuaternion rotation = _rotationResolver.Resolve(graphContext).AsQuaternion();

		// A zero quaternion is what an unfilled rotation operand resolves to, and Godot rejects one outright rather
		// than treating it as no rotation.
		var godotRotation = new Quaternion(rotation.X, rotation.Y, rotation.Z, rotation.W);

		return godotRotation.LengthSquared() <= 0.000001f
			? Basis.Identity
			: new Basis(godotRotation.Normalized());
	}

	private int ResolveMaskValue(GraphContext graphContext)
	{
		return _maskResolver is null ? 0 : (int)_maskResolver.Resolve(graphContext).AsDouble();
	}
}
