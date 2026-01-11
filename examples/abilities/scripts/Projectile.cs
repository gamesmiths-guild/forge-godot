// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Godot.Nodes;
using Godot;

namespace Gamesmiths.Forge.Example;

public partial class Projectile : Node3D
{
	private float _velocity = 1f;

	private Vector3 _direction = Vector3.Forward;

	[Export]
	public Area3D? CollisionArea { get; set; }

	[Export]
	public EffectArea3D? EffectArea { get; set; }

	public override void _Ready()
	{
		CollisionArea!.BodyEntered += Destroy;
		CollisionArea.AreaEntered += Destroy;
	}

	public void Launch(Vector3 dir, float vel, Node owner)
	{
		_direction = dir.Normalized();
		_direction = new Vector3(_direction.X, 0, _direction.Z).Normalized();
		_velocity = vel;

		if (EffectArea is null)
		{
			return;
		}

		EffectArea.EffectOwner = owner;
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

		QueueFree();
	}
}
