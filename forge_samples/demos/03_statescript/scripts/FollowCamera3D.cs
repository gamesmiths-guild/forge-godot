// Copyright © Gamesmiths Guild.

using Godot;

namespace Gamesmiths.Forge.Example;

/// <summary>
/// A damped follow rig with a fixed view. It moves the rig itself and never touches the camera's own offset, which is
/// what CameraShakeCueHandler writes - so a shake rides on top of the follow instead of fighting it.
/// </summary>
public partial class FollowCamera3D : Node3D
{
	[Export]
	public Node3D? Target { get; set; }

	[Export]
	public float FollowSpeed { get; set; } = 12f;

	public override void _Ready()
	{
		base._Ready();

		if (Target is null)
		{
			SetProcess(false);
			return;
		}

		// The rig moves on the frame, so it must not also be interpolated against physics ticks.
		PhysicsInterpolationMode = PhysicsInterpolationModeEnum.Off;
		GlobalPosition = Target.GlobalPosition;
	}

	// Visual interpolation belongs on the frame, not the physics step. The target moves on the physics tick, so
	// its interpolated transform is where it is drawn this frame.
	public override void _Process(double delta)
	{
		base._Process(delta);

		GlobalPosition = GlobalPosition.Lerp(
			Target!.GetGlobalTransformInterpolated().Origin, Mathf.Min(FollowSpeed * (float)delta, 1f));
	}
}
