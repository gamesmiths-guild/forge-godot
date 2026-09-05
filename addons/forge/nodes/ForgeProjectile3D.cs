// Copyright © Gamesmiths Guild.

using System.Collections.Generic;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Godot.Core;
using Gamesmiths.Forge.Godot.Core.Statescript.Physics;
using Godot;
using GodotRidArray = Godot.Collections.Array<Godot.Rid>;

namespace Gamesmiths.Forge.Godot.Nodes;

/// <summary>
/// A projectile that travels forward and applies its <see cref="ForgeEffect"/> children to whatever it hits.
/// </summary>
/// <remarks>
/// <para>Effects are authored as child nodes, exactly as on <see cref="EffectArea3D"/>, so a projectile is configured
/// without code. Aim is simply the rotation it was instantiated with: it always travels along its own forward axis,
/// which is what lets a graph aim one by binding the instantiation rotation to a look-at.</para>
/// <para>Ownership arrives through <see cref="IInstantiationReceiver"/> when a Statescript scene node creates it, so
/// the effects it applies are attributed to whoever cast it rather than to the projectile.</para>
/// <para>It travels on the physics tick rather than the render frame, because both ways of detecting a hit are settled
/// once per physics step: moving on the render frame would take several steps the physics server never sees, and which
/// of them it did see would depend on the frame rate.</para>
/// <para>With <see cref="Swept"/> on, each step is a shape cast along the motion rather than a reading of the area's
/// overlap list, which is what stops a target thinner than one step from being missed: the projectile meets whatever
/// stood in the step it crossed, not only what it happens to end the step inside. The cast repeats with each collider
/// it met excluded, so a piercing shot reports everything along the step in the order it met them, and the projectile
/// is placed at the impact rather than past it on the step that ends it.</para>
/// <para>That is detection, not collision. A collider carrying no entity is met, ignored and flown through, so level
/// geometry no more stops a projectile here than it did before: what ends a flight is the entity hit that spends the
/// last <see cref="Pierce"/> under <see cref="DestroyOnHit"/>, or <see cref="MaxRange"/> and
/// <see cref="MaxLifetime"/>.</para>
/// <para>The costs are a cast per step instead of a free overlap read, and that the first enabled
/// <see cref="CollisionShape3D"/> child becomes the query's shape — a projectile built from several shapes, or from a
/// <see cref="CollisionPolygon3D"/>, has to turn this off and keep <see cref="Speed"/> under the thinnest collider it
/// must hit divided by the physics tick.</para>
/// </remarks>
[GlobalClass]
[Icon("uid://tbuwwf03ikr3")]
public partial class ForgeProjectile3D : Area3D, IInstantiationReceiver
{
	private readonly HashSet<IForgeEntity> _alreadyHit = [];

	// Kept for the whole flight rather than per step: a collider the sweep has already answered for - hit, ignored as
	// the caster, or carrying no entity at all - would otherwise stop it again on every step that still touches it.
	private readonly GodotRidArray _exclusions = [];

	private EffectApplier? _effectApplier;
	private CollisionShape3D? _sweptShape;
	private IForgeEntity? _effectOwner;
	private IForgeEntity? _effectSource;
	private Vector3 _launchPosition;
	private float _distanceTraveled;
	private double _elapsedTime;
	private int _remainingHits;
	private bool _expired;

	/// <summary>
	/// Emitted when the projectile applies its effects to something.
	/// </summary>
	/// <param name="hit">The node that was hit.</param>
	[Signal]
	public delegate void HitEventHandler(Node3D hit);

	/// <summary>
	/// Emitted when the projectile runs out of lifetime or range, whether or not it hit anything on the way.
	/// </summary>
	[Signal]
	public delegate void ExpiredEventHandler();

	/// <summary>
	/// Gets or sets how fast the projectile travels, in units per second.
	/// </summary>
	[Export]
	public float Speed { get; set; } = 10.0f;

	/// <summary>
	/// Gets or sets how long the projectile lives, in seconds. Zero or less means it never expires on time alone.
	/// </summary>
	[Export]
	public float MaxLifetime { get; set; } = 5.0f;

	/// <summary>
	/// Gets or sets how far the projectile travels before expiring, in units. Zero or less means unlimited.
	/// </summary>
	[Export]
	public float MaxRange { get; set; }

	/// <summary>
	/// Gets or sets how many extra entities the projectile passes through. Zero stops at the first one.
	/// </summary>
	[Export]
	public int Pierce { get; set; }

	/// <summary>
	/// Gets or sets the curve scaling the effect magnitude by how far the projectile has flown.
	/// </summary>
	/// <remarks>
	/// Sampled at distance travelled over <see cref="MaxRange"/>, and passed to the effect as float context data, which
	/// a custom execution or calculator reads back to scale its magnitude. Without a range there is nothing to measure
	/// the distance against, so the curve is sampled at zero; without a curve the multiplier is always one.
	/// </remarks>
	[Export]
	public Curve? DistanceFalloffCurve { get; set; }

	/// <summary>
	/// Gets or sets a value indicating whether the projectile is destroyed once it has no pierces left.
	/// </summary>
	[Export]
	public bool DestroyOnHit { get; set; } = true;

	/// <summary>
	/// Gets or sets a value indicating whether overlapping areas count as hits, in addition to bodies.
	/// </summary>
	[Export]
	public bool IncludeAreas { get; set; }

	/// <summary>
	/// Gets or sets a value indicating whether each step is swept, rather than read off the area's overlap list.
	/// </summary>
	/// <remarks>
	/// On, which is the answer for anything moving fast enough to be worth calling a projectile. Turn it off for one
	/// built from several collision shapes or from a polygon, where there is no single shape for the cast to sweep.
	/// </remarks>
	[Export]
	public bool Swept { get; set; } = true;

	/// <inheritdoc/>
	public void OnInstantiated(IForgeEntity? owner, IForgeEntity? source)
	{
		_effectOwner = owner;

		// The projectile's own entity, when it has one, is the more useful source: effects then trace back to the thing
		// that actually touched the target. Falling back to the instantiator's source keeps attribution sensible
		// without one.
		_effectSource = ForgeEntityBridge.TryGetEntity(this, out IForgeEntity? ownEntity) ? ownEntity : source;
	}

	/// <inheritdoc/>
	public override void _Ready()
	{
		base._Ready();

		_effectApplier = new EffectApplier(this);
		_launchPosition = GlobalPosition;
		_remainingHits = Pierce + 1;

		if (Swept)
		{
			ResolveSweptShape();
		}

		if (_sweptShape is null)
		{
			BodyEntered += OnBodyEntered;

			if (IncludeAreas)
			{
				AreaEntered += OnAreaEntered;
			}

			return;
		}

		// Nothing reads the overlap list once the sweep decides the hits, and an armed area pays for a test per
		// physics step that answers nothing. The projectile stays monitorable, so areas watching for it still see it.
		Monitoring = false;

		// A shape query run from inside the projectile would otherwise find the projectile.
		_exclusions.Add(GetRid());
	}

	/// <inheritdoc/>
	public override void _PhysicsProcess(double delta)
	{
		base._PhysicsProcess(delta);

		if (_expired)
		{
			return;
		}

		// Forward is -Z, so the rotation it was created with is the whole aiming story.
		Vector3 motion = -GlobalBasis.Z.Normalized() * Speed * (float)delta;

		if (_sweptShape is not null)
		{
			SweepStep(motion);

			if (_expired)
			{
				return;
			}
		}

		GlobalPosition += motion;

		_distanceTraveled = GlobalPosition.DistanceTo(_launchPosition);
		_elapsedTime += delta;

		if ((MaxRange > 0 && _distanceTraveled >= MaxRange)
			|| (MaxLifetime > 0 && _elapsedTime >= MaxLifetime))
		{
			Expire();
		}
	}

	private void ResolveSweptShape()
	{
		int enabledShapes = 0;

		foreach (Node child in GetChildren())
		{
			if (child is not CollisionShape3D { Disabled: false, Shape: not null } collisionShape)
			{
				continue;
			}

			enabledShapes++;
			_sweptShape ??= collisionShape;
		}

		if (enabledShapes == 0)
		{
			GD.PushWarning(
				$"ForgeProjectile3D [{Name}] is set to sweep but has no enabled CollisionShape3D child to sweep " +
				"with. Falling back to the area's own overlaps, which a fast projectile can tunnel through.");
			return;
		}

		if (enabledShapes > 1)
		{
			GD.PushWarning(
				$"ForgeProjectile3D [{Name}] sweeps one shape and has {enabledShapes} enabled. Only " +
				$"[{_sweptShape!.Name}] is swept; the rest are detecting nothing.");
		}
	}

	// Casts the step rather than reading the overlap list, and repeats with each collider met excluded so a pierce
	// reports everything along the step. The whole motion is re-swept each time rather than the remainder, so the
	// distances the hits report stay measured from where the step began.
	private void SweepStep(Vector3 motion)
	{
		World3D? world = GetWorld3D();

		if (world is null || _sweptShape?.Shape is null)
		{
			return;
		}

		Transform3D start = _sweptShape.GlobalTransform;
		float traveled = GlobalPosition.DistanceTo(_launchPosition);

		while (PhysicsQuery3D.TryShapecast(
			world,
			_sweptShape.Shape,
			start,
			motion,
			CollisionMask,
			IncludeAreas,
			_exclusions,
			out Transform3D hitTransform,
			out RaycastResult3D result))
		{
			// Excluded by rid rather than by the collider node, because not every shape owner is a
			// CollisionObject3D - a GridMap owns its own - and one that cannot be excluded would stop every
			// remaining cast of this step at exactly the same place.
			if (!result.Rid.IsValid)
			{
				return;
			}

			_exclusions.Add(result.Rid);

			// Set before the hit so the falloff is sampled at the impact rather than at where the step started. The
			// move below overwrites it with where the projectile actually ends up.
			_distanceTraveled = traveled + result.Distance;

			if (result.Node is not null)
			{
				TryHit(result.Node);
			}

			if (_expired)
			{
				// Placed at the impact rather than past it: this is the step that stops here, and a projectile
				// rendered on the far side of what stopped it is the artefact the sweep exists to remove.
				GlobalPosition += hitTransform.Origin - start.Origin;
				return;
			}
		}
	}

	private void OnAreaEntered(Area3D area)
	{
		TryHit(area);
	}

	private void OnBodyEntered(Node3D body)
	{
		TryHit(body);
	}

	private void TryHit(Node3D other)
	{
		// The wider lookup on purpose: a hurtbox is often nested below the body that owns the entity, and a projectile
		// that ignored those would pass straight through anything built that way.
		if (_expired || !ForgeEntityBridge.TryGetEntityInHierarchy(other, out IForgeEntity? target))
		{
			return;
		}

		// A pierce should cost one hit per victim, and an overlap can report the same one more than once.
		if (ReferenceEquals(target, _effectOwner) || !_alreadyHit.Add(target))
		{
			return;
		}

		// The resolved entity, not the node the walk started from: the node overload would look it up narrowly again
		// and find nothing under a nested hurtbox, burning a pierce on a hit that applied no effects.
		_effectApplier?.ApplyEffects(target, ResolveFalloff(), _effectOwner, _effectSource);
		EmitSignalHit(other);

		_remainingHits--;

		if (_remainingHits <= 0 && DestroyOnHit)
		{
			_expired = true;
			QueueFree();
		}
	}

	private float ResolveFalloff()
	{
		if (DistanceFalloffCurve is null)
		{
			return 1.0f;
		}

		float position = MaxRange > 0 ? Mathf.Clamp(_distanceTraveled / MaxRange, 0.0f, 1.0f) : 0.0f;
		return DistanceFalloffCurve.Sample(position);
	}

	private void Expire()
	{
		_expired = true;
		EmitSignalExpired();
		QueueFree();
	}
}
