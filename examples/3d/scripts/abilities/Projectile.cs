// Copyright © Gamesmiths Guild.

using System;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Godot.Core;
using Gamesmiths.Forge.Godot.Nodes;
using Godot;

namespace Gamesmiths.Forge.Example;

public partial class Projectile : Node3D
{
	private float _velocity = 1f;
	private EffectApplier? _effectApplier;
	private IForgeEntity? _effectOwner;
	private Vector3 _direction = Vector3.Forward;
	private Vector3 _launchPosition = Vector3.Zero;

	[Export]
	public Area3D? CollisionArea { get; set; }

	[Export]
	public ForgeEntity? ForgeEntity { get; set; }

	public override void _Ready()
	{
		CollisionArea!.BodyEntered += Destroy;
		CollisionArea.AreaEntered += Destroy;

		_effectApplier = new EffectApplier(this);
	}

	public void Launch(Vector3 dir, float vel, Node owner)
	{
		_direction = dir.Normalized();
		_direction = new Vector3(_direction.X, 0, _direction.Z).Normalized();
		_velocity = vel;
		_effectOwner = owner as IForgeEntity;
		_launchPosition = GlobalPosition;
	}

	public override void _Process(double delta)
	{
		Vector3 motion = _direction * _velocity * (float)delta;
		GlobalTranslate(motion);
	}

	private void Destroy(Node3D other)
	{
		if (other is not CharacterBody3D)
		{
			return;
		}

		var distanceTraveled = GlobalPosition.DistanceTo(_launchPosition);
		var damageFalloffMultiplier = Math.Clamp(1f - (distanceTraveled / 10f), 0.1f, 1f);

		_effectApplier!.ApplyEffects(other, damageFalloffMultiplier, _effectOwner, ForgeEntity);

		QueueFree();
	}
}
