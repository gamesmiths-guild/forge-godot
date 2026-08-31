// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Godot.Core.Statescript.Physics;
using Gamesmiths.Forge.Statescript.Nodes;
using GodotRidArray = Godot.Collections.Array<Godot.Rid>;

namespace Gamesmiths.Forge.Godot.Core.Statescript.Nodes.State;

/// <summary>
/// The context for a <see cref="LineOfSight2DNode"/>. Remembers whether the line was clear, which is what turns
/// a per-tick check into the transitions the node reports.
/// </summary>
public class LineOfSight2DNodeContext : StateNodeContext
{
	/// <summary>
	/// Gets the collision objects the line passes through, rebuilt each check and kept between them so a node watching
	/// a line for ten seconds does not leave ten seconds of garbage behind it.
	/// </summary>
	public GodotRidArray Exclusions { get; } = [];

	/// <summary>
	/// Gets or sets whether the line was clear on the last check. Null before the first one, so that check counts as a
	/// transition into whatever it found rather than being compared against a guess.
	/// </summary>
	public bool? LastClear { get; set; }

	/// <summary>
	/// Gets or sets the debug marker drawing the line while the node is active. Null unless the game is running with
	/// Visible Collision Shapes on.
	/// </summary>
	internal PhysicsDebugMarker2D? DebugMarker { get; set; }
}
