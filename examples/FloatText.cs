// Copyright © 2025 Gamesmiths Guild.

using Godot;

namespace Gamesmiths.Forge.Example;

public partial class FloatText : Sprite3D
{
	[Export]
	public Timer Timer { get; set; }

	[Export]
	public Label Label { get; set; }

	public override void _Ready()
	{
		base._Ready();

		Timer.Timeout += QueueFree;
	}

	public override void _Process(double delta)
	{
		base._Process(delta);

		Position += new Vector3(0, 1 * (float)delta, 0);
	}

	public void SetText(string text)
	{
		Label.Text = text;
	}
}
