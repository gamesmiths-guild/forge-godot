// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Statescript.Nodes;
using Godot;

namespace Gamesmiths.Forge.Godot.Core.Statescript.Nodes.State;

/// <summary>
/// The context for a <see cref="ForceOverride3DNode"/>. Holds the body it captured and what that body's constant force
/// and torque were before the node touched them.
/// </summary>
/// <remarks>
/// The two flags matter as much as the two values: a node with only a force bound must put the force back and leave
/// the torque alone, or an authored constant torque on the body would be destroyed by an ability that never mentioned
/// one.
/// </remarks>
public class ForceOverride3DNodeContext : StateNodeContext
{
	/// <summary>
	/// Gets or sets the body being driven, or <see langword="null"/> when activation found none.
	/// </summary>
	public RigidBody3D? Body { get; set; }

	/// <summary>
	/// Gets or sets the constant force the body carried before this node wrote one.
	/// </summary>
	public Vector3 PreviousForce { get; set; }

	/// <summary>
	/// Gets or sets the constant torque the body carried before this node wrote one.
	/// </summary>
	public Vector3 PreviousTorque { get; set; }

	/// <summary>
	/// Gets or sets a value indicating whether this node wrote the constant force, and so owes it back.
	/// </summary>
	public bool WroteForce { get; set; }

	/// <summary>
	/// Gets or sets a value indicating whether this node wrote the constant torque, and so owes it back.
	/// </summary>
	public bool WroteTorque { get; set; }

	/// <summary>
	/// Gets or sets the debug arrow held for as long as the push lasts.
	/// </summary>
	public MeshInstance3D? DebugMarker { get; set; }
}
