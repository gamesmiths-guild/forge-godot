// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Godot.Core.Statescript.Resolvers;

/// <summary>
/// Which question the input action resolver asks about a button.
/// </summary>
public enum InputActionMode
{
	/// <summary>
	/// Whether the button is down right now. This is the one a condition wants: it reads the same on every frame the
	/// button is held.
	/// </summary>
	Pressed = 0,

	/// <summary>
	/// Whether the button went down this frame.
	/// </summary>
	JustPressed = 1,

	/// <summary>
	/// Whether the button came up this frame.
	/// </summary>
	JustReleased = 2,
}
