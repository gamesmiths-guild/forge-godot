// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Statescript.Nodes;

namespace Gamesmiths.Forge.Godot.Core.Statescript.Nodes.State;

/// <summary>
/// The context for a <see cref="PlayAnimationNode"/>. Holds how long the animation plays and how much of that has run,
/// counted in fixed steps.
/// </summary>
public class PlayAnimationNodeContext : StateNodeContext
{
	/// <summary>
	/// Gets or sets how long the animation plays at the speed it was started with, in seconds. Infinite for a looping
	/// clip, and zero when there was nothing to play.
	/// </summary>
	public double Duration { get; set; }

	/// <summary>
	/// Gets or sets the time counted since the node activated or restarted, in seconds.
	/// </summary>
	public double ElapsedTime { get; set; }
}
