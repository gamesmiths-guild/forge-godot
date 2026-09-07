// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Statescript.Nodes;
using Godot;
using GodotNode = Godot.Node;

namespace Gamesmiths.Forge.Godot.Core.Statescript.Nodes.State;

/// <summary>
/// The context for a <see cref="MoveBody3DNode"/>. Holds the move captured at activation so the update loop does not
/// re-resolve its inputs every step.
/// </summary>
/// <remarks>
/// The destination is snapshotted for the same reason Move To 3D snapshots it: travelling to where the target was when
/// the ability fired is a different behavior from chasing it, and chasing is what Look At and Nav Move To are for. The
/// body is captured too, so a move interrupted by a despawn does not write to whatever the input resolves to next.
/// </remarks>
public class MoveBody3DNodeContext : StateNodeContext
{
	/// <summary>
	/// Gets or sets the body being moved, or <see langword="null"/> when the move could not start.
	/// </summary>
	public PhysicsBody3D? Body { get; set; }

	/// <summary>
	/// Gets or sets the position the move ends at.
	/// </summary>
	public Vector3 Destination { get; set; }

	/// <summary>
	/// Gets or sets how fast the body travels, in units per second.
	/// </summary>
	public float Speed { get; set; }

	/// <summary>
	/// Gets or sets how long the move may run before it reports being blocked, in seconds. This is the time the move
	/// would have taken unobstructed, which is what bounds a slide that never arrives.
	/// </summary>
	public double Duration { get; set; }

	/// <summary>
	/// Gets or sets how far through the move it is, in seconds.
	/// </summary>
	public double ElapsedTime { get; set; }

	/// <summary>
	/// Gets or sets the last thing the move touched, or <see langword="null"/> when it has touched nothing.
	/// </summary>
	/// <remarks>
	/// Kept across steps rather than read off the step that ends the move, because under
	/// <see cref="BlockedResponse.Slide"/> the two are rarely the same one: a dash grazes a wall, slides clear of it,
	/// and runs out of time several steps later with nothing under it. Reporting nothing there would answer "what
	/// blocked me" with silence in exactly the case the port exists for.
	/// </remarks>
	public GodotNode? LastBlocker { get; set; }
}
