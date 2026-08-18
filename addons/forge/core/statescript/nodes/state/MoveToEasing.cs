// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Godot.Core.Statescript.Nodes.State;

/// <summary>
/// How a Move To node distributes its travel over time.
/// </summary>
public enum MoveToEasing
{
	/// <summary>Constant speed from start to finish.</summary>
	Linear = 0,

	/// <summary>Starts slowly and accelerates. Reads as winding up.</summary>
	EaseIn = 1,

	/// <summary>Starts fast and settles. Reads as a lunge or a dash.</summary>
	EaseOut = 2,

	/// <summary>Accelerates then settles. Reads as a deliberate reposition.</summary>
	EaseInOut = 3,
}
