// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Statescript.Nodes;
using Godot;

namespace Gamesmiths.Forge.Godot.Core.Statescript.Nodes.State;

/// <summary>
/// The context for a <see cref="RotateTo3DNode"/>. Holds the turn captured at activation so the update loop does not
/// re-resolve its inputs every tick.
/// </summary>
/// <remarks>
/// Start and target are snapshotted for the same reason <see cref="MoveTo3DNodeContext"/> snapshots its endpoints:
/// re-reading the target each tick would make the turn chase a value that may itself be moving, which is tracking
/// rather than turning to face where something was when the ability fired.
/// </remarks>
public class RotateTo3DNodeContext : StateNodeContext
{
	/// <summary>
	/// Gets or sets the rotation the turn started from.
	/// </summary>
	public Quaternion StartRotation { get; set; }

	/// <summary>
	/// Gets or sets the rotation the turn ends at.
	/// </summary>
	public Quaternion TargetRotation { get; set; }

	/// <summary>
	/// Gets or sets how long the turn takes, in seconds.
	/// </summary>
	public double Duration { get; set; }

	/// <summary>
	/// Gets or sets how far through the turn it is, in seconds.
	/// </summary>
	public double ElapsedTime { get; set; }

	/// <summary>
	/// Gets or sets a value indicating whether the node found a node to turn at activation.
	/// </summary>
	public bool HasTarget { get; set; }
}
