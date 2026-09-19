// Copyright © Gamesmiths Guild.

using Godot;

namespace Gamesmiths.Forge.Example;

/// <summary>
/// Every non-player body in the demo. It owns no intent at all: graphs write the velocity through Set Velocity 3D or
/// steer it through Nav Move To 3D, and this only adds gravity and spends it. Forge Entity runs its fixed step before
/// the body it hangs under, so the nav write lands first and the gravity here is added on top of it.
/// </summary>
public partial class PuppetBody3D : CharacterBody3D
{
	private readonly float _gravity = ProjectSettings.GetSetting("physics/3d/default_gravity").AsSingle();

	public override void _PhysicsProcess(double delta)
	{
		base._PhysicsProcess(delta);

		if (!IsOnFloor())
		{
			Vector3 velocity = Velocity;
			velocity.Y -= _gravity * (float)delta;
			Velocity = velocity;
		}

		MoveAndSlide();
	}
}
