// Copyright © 2025 Gamesmiths Guild.

namespace Gamesmiths.Forge.Example;

public enum EffectTriggerMode
{
	/// <summary>
	/// Add effects when entering the area.
	/// </summary>
	OnEnter = 0,

	/// <summary>
	/// Add effects when exiting the area.
	/// </summary>
	OnExit = 1,

	/// <summary>
	/// Add effects when entering the area and removes when exiting it.
	/// </summary>
	OnStay = 2,
}
