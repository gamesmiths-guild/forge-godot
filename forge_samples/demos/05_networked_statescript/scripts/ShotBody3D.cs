// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Godot.Nodes;
using Godot;

namespace Gamesmiths.Forge.Example;

/// <summary>
/// The shot's body, which passes through the player who fired it and meets everyone else. The shooter is the player of
/// the peer the shot belongs to, which every way of making it sets before it enters the tree: a graph's on the server
/// and ahead of it, and a spawn the session makes from the server's word.
/// </summary>
public partial class ShotBody3D : CharacterBody3D
{
	/// <inheritdoc/>
	public override void _Ready()
	{
		base._Ready();

		int owner = GetNode<ForgeEntity>("%Forge Entity").NetworkOwner;

		foreach (Node node in GetTree().GetNodesInGroup("player"))
		{
			if (node is PhysicsBody3D player
				&& player.GetNodeOrNull<ForgeEntity>("Forge Entity")?.NetworkOwner == owner)
			{
				AddCollisionExceptionWith(player);
			}
		}
	}
}
