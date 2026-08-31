// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Statescript.Nodes;
using Godot;

namespace Gamesmiths.Forge.Godot.Core.Statescript.Nodes.State;

/// <summary>
/// The context for a <see cref="CollisionOverride3DNode"/>. Holds the body it changed and the bits it found there, so
/// deactivation restores what the scene authored rather than what the node assumes.
/// </summary>
/// <remarks>
/// The body is captured rather than re-resolved on deactivate: the entity input may have moved on, and putting the
/// original bits back on a different body than the one they came from is worse than not restoring them at all.
/// </remarks>
public class CollisionOverride3DNodeContext : StateNodeContext
{
	/// <summary>
	/// Gets or sets the collision object the override was applied to.
	/// </summary>
	public CollisionObject3D? Body { get; set; }

	/// <summary>
	/// Gets or sets the bit field as it was before the override.
	/// </summary>
	public uint OriginalBits { get; set; }

	/// <summary>
	/// Gets or sets the bits this override acted on, which are the only ones it puts back.
	/// </summary>
	public uint OverriddenBits { get; set; }
}
