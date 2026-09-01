// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Statescript.Nodes;
using Godot;

namespace Gamesmiths.Forge.Godot.Core.Statescript.Nodes.State;

/// <summary>
/// The context for a <see cref="MoveTo2DNode"/>. Holds the move captured at activation so the update loop does not
/// re-resolve its inputs every tick.
/// </summary>
/// <remarks>
/// Start and destination are snapshotted on purpose. Re-reading the destination each tick would make the move chase a
/// value that may itself be moving, which is a different behavior from travelling to where the target was when the
/// ability fired, and the arc would never resolve.
/// </remarks>
public class MoveTo2DNodeContext : StateNodeContext
{
	/// <summary>
	/// Gets or sets the position the move started from.
	/// </summary>
	public Vector2 StartPosition { get; set; }

	/// <summary>
	/// Gets or sets the position the move ends at.
	/// </summary>
	public Vector2 Destination { get; set; }

	/// <summary>
	/// Gets or sets how long the move takes, in seconds.
	/// </summary>
	public double Duration { get; set; }

	/// <summary>
	/// Gets or sets how far through the move it is, in seconds.
	/// </summary>
	public double ElapsedTime { get; set; }

	/// <summary>
	/// Gets or sets how high the move arcs at its midpoint, in units. Zero travels in a straight line.
	/// </summary>
	public float ArcHeight { get; set; }

	/// <summary>
	/// Gets or sets a value indicating whether the node found a node to move at activation. A move with no target does
	/// nothing rather than deactivating immediately, so the graph's own abort path stays in control.
	/// </summary>
	public bool HasTarget { get; set; }
}
