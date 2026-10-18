// Copyright © Gamesmiths Guild.

using Godot;

namespace Gamesmiths.Forge.Example;

/// <summary>
/// Arrow-key movement. Plain Godot: nothing in it knows about Forge.
/// </summary>
public partial class QuickStartPlayer : CharacterBody2D
{
	[Export]
	public float Speed { get; set; } = 250;

	public override void _PhysicsProcess(double delta)
	{
		Velocity = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down") * Speed;
		MoveAndSlide();
	}
}
