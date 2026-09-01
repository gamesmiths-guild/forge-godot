// Copyright © Gamesmiths Guild.

using System.Collections.Generic;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Godot.Core.Statescript.Physics;
using Gamesmiths.Forge.Statescript.Nodes;

namespace Gamesmiths.Forge.Godot.Core.Statescript.Nodes.State;

/// <summary>
/// The context for an <see cref="Overlap2DNode"/>. Holds the entities currently inside, the scratch collections
/// the per-poll diff needs, and the shape a transient query reuses.
/// </summary>
/// <remarks>
/// The scratch collections live here rather than being allocated per poll because this node polls for as long as it is
/// active, and an ability that watches an area for ten seconds should not leave ten seconds of garbage behind it.
/// </remarks>
public class Overlap2DNodeContext : StateNodeContext
{
	/// <summary>
	/// Gets the entities currently inside, as of the last poll.
	/// </summary>
	public HashSet<IForgeEntity> Overlapping { get; } = [];

	/// <summary>
	/// Gets the entities the current poll found, before it is diffed against <see cref="Overlapping"/>.
	/// </summary>
	public HashSet<IForgeEntity> Pending { get; } = [];

	/// <summary>
	/// Gets the entities that changed in the current poll, collected before they are reported so the set being
	/// reported from is not modified while it is walked.
	/// </summary>
	public List<IForgeEntity> Changed { get; } = [];

	/// <summary>
	/// Gets or sets how long it has been since the last poll, in seconds.
	/// </summary>
	public double TimeSincePoll { get; set; }

	/// <summary>
	/// Gets or sets whether anything was inside as of the last poll. Null before the first poll, so that poll counts
	/// as a transition into whatever it found.
	/// </summary>
	public bool? LastOccupied { get; set; }

	/// <summary>
	/// Gets or sets the debug marker drawing the watched volume while the node is active. Null unless the game is
	/// running with Visible Collision Shapes on and the volume is a transient shape, since an area already in the scene
	/// is drawn by Godot itself.
	/// </summary>
	internal PhysicsDebugMarker2D? DebugMarker { get; set; }
}
