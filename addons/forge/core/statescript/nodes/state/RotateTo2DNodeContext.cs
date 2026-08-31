// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Statescript.Nodes;

namespace Gamesmiths.Forge.Godot.Core.Statescript.Nodes.State;

/// <summary>
/// The context for a <see cref="RotateTo2DNode"/>. Holds the turn captured at activation so the update loop does not
/// re-resolve its inputs every tick.
/// </summary>
/// <remarks>
/// The target is stored as the total signed angle to turn through rather than as an absolute facing. A plane's
/// rotation is a number that keeps counting past a full turn, so interpolating between two absolute angles would take
/// the long way round whenever the pair straddles the wrap; a delta resolved once at activation cannot.
/// </remarks>
public class RotateTo2DNodeContext : StateNodeContext
{
	/// <summary>
	/// Gets or sets the rotation the turn started from, in radians.
	/// </summary>
	public float StartRotation { get; set; }

	/// <summary>
	/// Gets or sets how far the turn goes from there, in radians, signed by which way it turns.
	/// </summary>
	public float DeltaRotation { get; set; }

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
