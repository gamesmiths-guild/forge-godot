// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Godot.Nodes;

/// <summary>
/// How often a server sends each client the world, in ticks of the session's clock.
/// </summary>
public enum NetworkSendRate
{
	/// <summary>
	/// Every tick: 64 a second at a 64 Hz tick. The freshest world, at about twice the bandwidth.
	/// </summary>
	EveryTick = 1,

	/// <summary>
	/// Every second tick: 32 a second at a 64 Hz tick. The default.
	/// </summary>
	EverySecondTick = 2,

	/// <summary>
	/// Every fourth tick: 16 a second at a 64 Hz tick, for slow games or thin links.
	/// </summary>
	EveryFourthTick = 4,
}
