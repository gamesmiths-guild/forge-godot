// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Godot.Core.Statescript.Physics;
using Gamesmiths.Forge.Statescript.Nodes;

namespace Gamesmiths.Forge.Godot.Core.Statescript.Nodes.State;

/// <summary>
/// The context for a <see cref="Sweep2DNode"/>. Remembers whether the sweep was meeting something, which is what turns
/// a per-tick cast into the transitions the node reports.
/// </summary>
public class Sweep2DNodeContext : StateNodeContext
{
	/// <summary>
	/// Gets or sets whether the last sweep met something. Null before the first sweep, so that first result counts as
	/// a transition into whatever it found rather than being compared against a guess.
	/// </summary>
	public bool? LastHit { get; set; }

	/// <summary>
	/// Gets or sets the debug marker drawing the swept shape while the node is active. Null unless the game is running
	/// with Visible Collision Shapes on.
	/// </summary>
	internal PhysicsDebugMarker2D? DebugMarker { get; set; }
}
