// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Statescript.Nodes;
using Godot;

namespace Gamesmiths.Forge.Godot.Core.Statescript.Nodes.State;

/// <summary>
/// The context for a <see cref="Ray3DNode"/>. Remembers whether the ray was hitting something, which is what turns a
/// per-tick cast into the transitions the node reports.
/// </summary>
public class Ray3DNodeContext : StateNodeContext
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
	public MeshInstance3D? DebugMarker { get; set; }
}
