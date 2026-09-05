// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Statescript.Nodes;
using Godot;

namespace Gamesmiths.Forge.Godot.Core.Statescript.Nodes.State;

/// <summary>
/// The context for a <see cref="NavMoveTo3DNode"/>. Holds the agent being steered and, under safe velocity, the
/// connection that carries the avoidance solver's answer back.
/// </summary>
/// <remarks>
/// The callable is stored rather than rebuilt on disconnect because Godot matches connections by the callable itself,
/// and a freshly built one - even over the same method - is a different callable that disconnects nothing.
/// </remarks>
public class NavMoveTo3DNodeContext : StateNodeContext
{
	/// <summary>
	/// Gets or sets the agent being steered, or <see langword="null"/> when activation found none.
	/// </summary>
	public NavigationAgent3D? Agent { get; set; }

	/// <summary>
	/// Gets or sets the connection carrying the avoidance solver's answer, when safe velocity is on.
	/// </summary>
	public Callable? VelocityComputed { get; set; }

	/// <summary>
	/// Gets or sets the velocity the avoidance solver last computed.
	/// </summary>
	public Vector3 SafeVelocity { get; set; }

	/// <summary>
	/// Gets or sets the physics frame the node activated on. Reachability is judged only after it has advanced, since
	/// a navigation map that has not synced yet reports every destination as unreachable.
	/// </summary>
	public ulong ActivationPhysicsFrame { get; set; }

	/// <summary>
	/// Gets or sets a value indicating whether the avoidance solver is actually driving this walk.
	/// </summary>
	/// <remarks>
	/// Safe velocity asks the agent for an avoidance-adjusted answer, and the agent only produces one when its own
	/// avoidance is switched on - with it off the signal never fires at all, so the setting has to be resolved against
	/// the agent rather than trusted from the node.
	/// </remarks>
	public bool SafeVelocityActive { get; set; }

	/// <summary>
	/// Gets or sets the destination last handed to the agent, so an unmoved one is not handed over again.
	/// </summary>
	/// <remarks>
	/// Godot's target setter requests a repath unconditionally and a repath discards the computed path, so submitting
	/// the same destination every update costs a full path query every rendered frame.
	/// </remarks>
	public Vector3 SubmittedTarget { get; set; }

	/// <summary>
	/// Gets or sets a value indicating whether a destination has been handed to the agent yet. Kept apart from the
	/// destination itself because the world origin is a destination a graph can legitimately name.
	/// </summary>
	public bool HasSubmittedTarget { get; set; }
}
