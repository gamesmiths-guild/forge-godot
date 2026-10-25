// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Statescript.Nodes;
using Node = Godot.Node;

namespace Gamesmiths.Forge.Godot.Core.Statescript.Nodes.State;

/// <summary>
/// The context for a <see cref="PlayAudioNode"/>. Holds what the activation plays and how long it lasts, counted in
/// fixed steps.
/// </summary>
/// <remarks>
/// The player, volume and pitch are resolved once, when the node activates or restarts, and the presentation pass plays
/// what was resolved then. Resolving them again in the pass could draw from the graph's random, which presentation must
/// not touch, and an input that changed since would play the sound at a length the node is not counting.
/// </remarks>
public class PlayAudioNodeContext : StateNodeContext
{
	/// <summary>
	/// Gets or sets the audio player the activation plays, or <see langword="null"/> when there was none.
	/// </summary>
	public Node? Player { get; set; }

	/// <summary>
	/// Gets or sets the volume to play at, in decibels, or <see langword="null"/> for the player's own.
	/// </summary>
	public double? VolumeDb { get; set; }

	/// <summary>
	/// Gets or sets the pitch scale to play at, or <see langword="null"/> for the player's own.
	/// </summary>
	public double? Pitch { get; set; }

	/// <summary>
	/// Gets or sets how long the sound plays at the pitch it was started with, in seconds. Infinite for a looping
	/// stream or one whose length is unknown, and zero when there was nothing to play.
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
