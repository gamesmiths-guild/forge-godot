// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Godot.Nodes;

/// <summary>
/// Whether what a client's actions hit meets an entity where that client was shown it, a moment behind the server.
/// </summary>
public enum EntityLagCompensation
{
	/// <summary>
	/// They do, for a networked entity with a body that the instigator doesn't own.
	/// </summary>
	Default = 0,

	/// <summary>
	/// They meet it where it is on the server.
	/// </summary>
	Never = 1,
}
