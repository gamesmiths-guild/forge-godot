// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Godot.Core.Statescript.Nodes;

/// <summary>
/// Where a scene node parents what it creates.
/// </summary>
public enum InstantiateParentMode
{
	/// <summary>
	/// The root of the current scene. This is what most instances want: a projectile or a summon should keep flying or
	/// standing where it is when the caster moves, and should outlive the caster's own subtree.
	/// </summary>
	CurrentScene = 0,

	/// <summary>
	/// The spatial node of the entity given by the Parent Entity input, so the instance follows it. Use this for
	/// anything attached to the caster, such as a held effect or a shield mesh.
	/// </summary>
	Entity = 1,

	/// <summary>
	/// The node given by the Parent Node input, for instances that belong to something the graph looked up or to a
	/// container addressed by path.
	/// </summary>
	Node = 2,
}
