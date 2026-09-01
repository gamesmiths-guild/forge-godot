// Copyright © Gamesmiths Guild.

using System.Collections.Generic;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Godot.Core;
using Godot;

namespace Gamesmiths.Forge.Godot.Nodes;

/// <summary>
/// A projectile that travels forward and applies its <see cref="ForgeEffect"/> children to whatever it hits.
/// </summary>
/// <remarks>
/// <para>Effects are authored as child nodes, exactly as on <see cref="EffectArea2D"/>, so a projectile is configured
/// without code. Aim is simply the rotation it was instantiated with: it always travels along its own forward axis,
/// which is what lets a graph aim one by binding the instantiation rotation to a look-at.</para>
/// <para>Ownership arrives through <see cref="IInstantiationReceiver"/> when a Statescript scene node creates it, so
/// the effects it applies are attributed to whoever cast it rather than to the projectile.</para>
/// <para>It travels on the physics tick rather than the render frame, because an <see cref="Area2D"/> is tested for
/// overlaps once per physics step: moving on the render frame would take several steps the physics server never sees,
/// and which of them it did see would depend on the frame rate. One move per test is the strongest guarantee an
/// area-based projectile can give. It is not immunity to tunnelling — a projectile whose step is longer than a collider
/// is thick still passes through it — so keep <see cref="Speed"/> under the thinnest collider it must hit, divided by
/// the physics tick, or use a swept scene node for the rest.</para>
/// </remarks>
[GlobalClass]
[Icon("uid://cnjrjkgwyewjx")]
public partial class ForgeProjectile2D : Area2D, IInstantiationReceiver
{
	private readonly HashSet<IForgeEntity> _alreadyHit = [];

	private EffectApplier? _effectApplier;
	private IForgeEntity? _effectOwner;
	private IForgeEntity? _effectSource;
	private Vector2 _launchPosition;
	private float _distanceTraveled;
	private double _elapsedTime;
	private int _remainingHits;
	private bool _expired;

	/// <summary>
	/// Emitted when the projectile applies its effects to something.
	/// </summary>
	/// <param name="hit">The node that was hit.</param>
	[Signal]
	public delegate void HitEventHandler(Node2D hit);

	/// <summary>
	/// Emitted when the projectile runs out of lifetime or range, whether or not it hit anything on the way.
	/// </summary>
	[Signal]
	public delegate void ExpiredEventHandler();

	/// <summary>
	/// Gets or sets how fast the projectile travels, in units per second.
	/// </summary>
	[Export]
	public float Speed { get; set; } = 400.0f;

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

		BodyEntered += OnBodyEntered;

		if (IncludeAreas)
		{
			AreaEntered += OnAreaEntered;
		}
	}

	/// <inheritdoc/>
	public override void _PhysicsProcess(double delta)
	{
		base._PhysicsProcess(delta);

		if (_expired)
		{
			return;
		}

		// 2D forward is +X, so the rotation it was created with is the whole aiming story.
		GlobalPosition += GlobalTransform.X.Normalized() * Speed * (float)delta;

		_distanceTraveled = GlobalPosition.DistanceTo(_launchPosition);
		_elapsedTime += delta;

		if ((MaxRange > 0 && _distanceTraveled >= MaxRange)
			|| (MaxLifetime > 0 && _elapsedTime >= MaxLifetime))
		{
			Expire();
		}
	}

	private void OnAreaEntered(Area2D area)
	{
		TryHit(area);
	}

	private void OnBodyEntered(Node2D body)
	{
		TryHit(body);
	}

	private void TryHit(Node2D other)
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
