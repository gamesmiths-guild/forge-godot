// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Godot.Resources.Abilities;

public enum TriggerSource
{
	/// <summary>
	/// No trigger source specified.
	/// </summary>
	None = 0,

	/// <summary>
	/// Triggered by an event.
	/// </summary>
	Event = 1,

	/// <summary>
	/// Triggered when a tag is added.
	/// </summary>
	TagAdded = 2,

	/// <summary>
	/// Triggered when a tag is present and removed when tag is gone.
	/// </summary>
	TagPresent = 3,
}
