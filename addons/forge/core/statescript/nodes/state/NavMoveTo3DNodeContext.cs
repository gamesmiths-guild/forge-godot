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
}
