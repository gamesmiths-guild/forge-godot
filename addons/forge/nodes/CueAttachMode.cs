// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Godot.Nodes;

/// <summary>
/// Where a cue handler parents what it instantiates.
/// </summary>
public enum CueAttachMode
{
	/// <summary>
	/// Under the target's own node, so the instance follows it. A burning aura, a shield bubble.
	/// </summary>
	TargetEntity = 0,

	/// <summary>
	/// Under the current scene, so the instance stays where it was spawned. An impact burst, a scorch mark, a corpse.
	/// </summary>
	World = 1,
}
