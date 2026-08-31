// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Godot.Core.Statescript.Physics;
using Gamesmiths.Forge.Statescript.Nodes;

namespace Gamesmiths.Forge.Godot.Core.Statescript.Nodes.State;

/// <summary>
/// The context for a <see cref="Ray2DNode"/>. Remembers whether the ray was hitting something, which is what turns a
/// per-tick cast into the transitions the node reports.
/// </summary>
public class Ray2DNodeContext : StateNodeContext
{
	/// <summary>
	/// Gets or sets whether the last cast hit. Null before the first cast, so that first result counts as a transition
	/// into whatever it found rather than being compared against a guess.
	/// </summary>
	public bool? LastHit { get; set; }

	/// <summary>
	/// Gets or sets the debug marker drawing the ray while the node is active. Null unless the game is running with
	/// Visible Collision Shapes on.
	/// </summary>
	internal PhysicsDebugMarker2D? DebugMarker { get; set; }
}
