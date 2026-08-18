// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;

namespace Gamesmiths.Forge.Godot.Core;

/// <summary>
/// Implemented by a spawned scene's root to receive the Forge ownership of whatever spawned it.
/// </summary>
/// <remarks>
/// Statescript's spawn nodes call this immediately after adding the instance to the tree, which is how a projectile or
/// a summon learns who cast it without the graph having to wire up a bespoke launch method. Implementing this interface
/// is optional; spawning works without it, the instance just receives no ownership.
/// </remarks>
public interface ISpawnReceiver
{
	/// <summary>
	/// Called once, right after the instance enters the scene tree.
	/// </summary>
	/// <param name="owner">The entity that owns the effects this instance applies, usually the ability's owner.</param>
	/// <param name="source">The entity credited as the source of those effects. This can be the instance's own entity
	/// when the spawned scene is a Forge entity in its own right.</param>
	void OnSpawned(IForgeEntity? owner, IForgeEntity? source);
}
