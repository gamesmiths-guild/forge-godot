// Copyright © Gamesmiths Guild.

using Node = Godot.Node;

namespace Gamesmiths.Forge.Godot.Core.Statescript.Nodes.State;

/// <summary>
/// What a Play Animation or Play Audio presentation started. The node marks it finished when its length runs out, so
/// the presentation pass leaves a natural finish to play out instead of stopping it.
/// </summary>
/// <remarks>
/// Playback starts in the pass at the end of the frame and so trails the fixed steps the node counts: when the length
/// runs out, the player can still be a frame short of the end, and stopping it there would snap an animation back to
/// its first frame or cut a sound's tail.
/// </remarks>
internal sealed class PresentationPlayback
{
	public Node? Player { get; set; }

	public bool Finished { get; set; }
}
