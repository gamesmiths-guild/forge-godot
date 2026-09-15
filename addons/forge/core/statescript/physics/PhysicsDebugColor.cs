// Copyright © Gamesmiths Guild.

using Godot;

namespace Gamesmiths.Forge.Godot.Core.Statescript.Physics;

/// <summary>
/// One colour of the physics debug drawing: the project setting that holds it, and what an unset project draws in.
/// </summary>
/// <remarks>
/// Read on first use and kept. The drawers ask for their colours on every poll and every flash, and a settings lookup
/// each time would marshal a Variant for a value that does not change while the game runs. A colour with zero alpha
/// turns its drawing off: the drawers allocate nothing for one.
/// </remarks>
/// <param name="setting">The project setting holding the colour.</param>
/// <param name="fallback">The colour an unset project draws in.</param>
internal sealed class PhysicsDebugColor(string setting, Color fallback)
{
	private Color? _value;

	/// <summary>
	/// Gets the project setting holding the colour.
	/// </summary>
	public string Setting { get; } = setting;

	/// <summary>
	/// Gets the colour an unset project draws in.
	/// </summary>
	public Color Default { get; } = fallback;

	/// <summary>
	/// Gets the colour the project draws in.
	/// </summary>
	public Color Value => _value ??= ProjectSettings.GetSetting(Setting, Default).AsColor();
}
