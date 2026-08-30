// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Statescript.Nodes;
using Godot;

namespace Gamesmiths.Forge.Godot.Core.Statescript.Nodes.State;

/// <summary>
/// The context for a <see cref="PlayAnimationNode"/>. Holds the player the node started, so it watches and stops
/// exactly the one it drove rather than looking it up again against an entity that may since have been freed.
/// </summary>
public class PlayAnimationNodeContext : StateNodeContext
{
	/// <summary>
	/// Gets or sets the animation player the node is driving, or <see langword="null"/> when nothing was played.
	/// </summary>
	public AnimationPlayer? Player { get; set; }
}
