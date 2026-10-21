// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Statescript.Nodes;

namespace Gamesmiths.Forge.Godot.Core.Statescript.Nodes.State;

/// <summary>
/// The context for a <see cref="PlayAudioNode"/>. Holds how long the sound plays and how much of that has run, counted
/// in fixed steps.
/// </summary>
public class PlayAudioNodeContext : StateNodeContext
{
	/// <summary>
	/// Gets or sets how long the sound plays at the pitch it was started with, in seconds. Infinite for a looping
	/// stream or one whose length is unknown, and zero when there was nothing to play.
	/// </summary>
	public double Duration { get; set; }

	/// <summary>
	/// Gets or sets the time counted since the node activated or restarted, in seconds.
	/// </summary>
	public double ElapsedTime { get; set; }
}
