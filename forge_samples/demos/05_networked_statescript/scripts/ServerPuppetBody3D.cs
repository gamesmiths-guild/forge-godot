// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Godot.Nodes;

namespace Gamesmiths.Forge.Example;

/// <summary>
/// Sample 03's puppet body, moving only where its entity is simulated: a client's mirror is placed by the session, so
/// it adds no gravity and spends no velocity of its own.
/// </summary>
public partial class ServerPuppetBody3D : PuppetBody3D
{
	private ForgeEntity? _entity;

	/// <inheritdoc/>
	public override void _PhysicsProcess(double delta)
	{
		_entity ??= GetNode<ForgeEntity>("%Forge Entity");

		if (!_entity.IsMirror)
		{
			base._PhysicsProcess(delta);
		}
	}
}
