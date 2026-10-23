// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Godot.Nodes;

/// <summary>
/// Where a <see cref="ForgeInputCollector"/> reads the player's aim from each physics tick.
/// </summary>
public enum ForgeAimSource
{
	/// <summary>
	/// The input carries no aim.
	/// </summary>
	None = 0,

	/// <summary>
	/// The point under the mouse cursor on the 3D world's ground, as <c>AimActivationData.FromMouseGround</c> finds
	/// it, aimed from the entity's node.
	/// </summary>
	MouseGround3D = 1,

	/// <summary>
	/// The point the active 3D camera looks at, as <c>AimActivationData.FromCamera</c> finds it.
	/// </summary>
	Camera3D = 2,

	/// <summary>
	/// The point under the mouse cursor in the 2D world, as <c>AimActivationData2D.FromMouse</c> finds it, aimed from
	/// the entity's node.
	/// </summary>
	Mouse2D = 3,
}
