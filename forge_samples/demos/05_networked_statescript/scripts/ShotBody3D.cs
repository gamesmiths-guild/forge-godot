// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Godot.Core;
using Godot;

namespace Gamesmiths.Forge.Example;

/// <summary>
/// The shot's body, which passes through the player who fired it and meets everyone else.
/// </summary>
public partial class ShotBody3D : CharacterBody3D, IInstantiationReceiver
{
	/// <inheritdoc/>
	public void OnInstantiated(IForgeEntity? owner, IForgeEntity? source)
	{
		if (ForgeEntityBridge.TryGetSpatialNode3D(owner, out Node3D? body) && body is PhysicsBody3D shooter)
		{
			AddCollisionExceptionWith(shooter);
		}
	}
}
