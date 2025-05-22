// Copyright © Gamesmiths Guild.

using System.Diagnostics;
using Godot;

namespace Gamesmiths.Forge.Example;

public partial class FloatText2D : Label
{
	[Export]
	public Timer? Timer { get; set; }

	public override void _Ready()
	{
		base._Ready();

		Debug.Assert(Timer is not null, $"{nameof(Timer)} reference is missing.");
		Timer.Timeout += QueueFree;
	}

	public override void _Process(double delta)
	{
		base._Process(delta);
		Position += new Vector2(0, -20 * (float)delta);
	}

	public void SetColor(Color color)
	{
		AddThemeColorOverride("font_color", color);
	}
}
