// Copyright © 2025 Gamesmiths Guild.

using System.Diagnostics;
using Godot;

namespace Gamesmiths.Forge.Example;

public partial class FloatText : Sprite3D
{
	[Export]
	public Timer? Timer { get; set; }

	[Export]
	public Label? Label { get; set; }

	public override void _Ready()
	{
		base._Ready();

		Debug.Assert(Timer is not null, $"{nameof(Timer)} reference is missing.");
		Timer.Timeout += QueueFree;
	}

	public override void _Process(double delta)
	{
		base._Process(delta);

		Position += new Vector3(0, 1 * (float)delta, 0);
	}

	public void SetText(string text)
	{
		Debug.Assert(Label is not null, $"{nameof(Label)} reference is missing.");
		Label.Text = text;
	}
}
