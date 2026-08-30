// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Godot.Core.Statescript.Interop;

/// <summary>
/// Which of a node's on/off switches the Set Node Enabled node writes.
/// </summary>
/// <remarks>
/// These are the switches that live on the node itself rather than on a property a game declares, and that are spelled
/// differently in 2D and 3D or reached through a method rather than a property - which is what keeps them out of Set
/// Node Property, where a path is all there is to say.
/// </remarks>
public enum NodeEnabledAspect
{
	/// <summary>
	/// Whether the node is drawn, on either a 2D or a 3D node. Hiding a parent hides everything under it.
	/// </summary>
	Visible = 0,

	/// <summary>
	/// Whether the node's per-frame processing runs.
	/// </summary>
	Processing = 1,

	/// <summary>
	/// Whether the node's physics-step processing runs.
	/// </summary>
	PhysicsProcessing = 2,

	/// <summary>
	/// Whether an area detects what enters it. Turning this off is how a trap is armed and disarmed without moving it.
	/// </summary>
	Monitoring = 3,

	/// <summary>
	/// Whether an area can be detected by other areas.
	/// </summary>
	Monitorable = 4,
}
