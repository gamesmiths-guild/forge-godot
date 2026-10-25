// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Statescript.Nodes;
using Godot;

namespace Gamesmiths.Forge.Godot.Core.Statescript.Nodes.State;

/// <summary>
/// The context for a <see cref="PlayAnimationNode"/>. Holds what the activation plays and how long it lasts, counted in
/// fixed steps.
/// </summary>
/// <remarks>
/// The player, speed and blend are resolved once, when the node activates or restarts, and the presentation pass plays
/// what was resolved then. Resolving them again in the pass could draw from the graph's random, which presentation must
/// not touch, and a speed that changed since would play the animation at a length the node is not counting.
/// </remarks>
public class PlayAnimationNodeContext : StateNodeContext
{
	/// <summary>
	/// Gets or sets the animation player the activation plays on, or <see langword="null"/> when there was none.
	/// </summary>
	public AnimationPlayer? Player { get; set; }

	/// <summary>
	/// Gets or sets the speed multiplier to play at, or <see langword="null"/> for the player's own.
	/// </summary>
	public double? Speed { get; set; }

	/// <summary>
	/// Gets or sets the blend time, in seconds, or <see langword="null"/> for the player's authored blend.
	/// </summary>
	public double? Blend { get; set; }

	/// <summary>
	/// Gets or sets how long the animation plays at the speed it was started with, in seconds. Infinite for a looping
	/// clip, and zero when there was nothing to play.
	/// </summary>
	public double Duration { get; set; }

	/// <summary>
	/// Gets or sets the time counted since the node activated or restarted, in seconds.
	/// </summary>
	public double ElapsedTime { get; set; }

	// One per activation, made as it starts so that a snapshot taken before the presentation pass shares it: what the
	// pass started for the activation, marked finished when the length runs out.
	internal PresentationPlayback Playback { get; set; } = new();
}
