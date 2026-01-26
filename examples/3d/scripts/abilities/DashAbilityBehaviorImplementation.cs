// Copyright © Gamesmiths Guild.

using System;
using Gamesmiths.Forge.Abilities;
using Gamesmiths.Forge.Godot.Nodes;
using Godot;

namespace Gamesmiths.Forge.Example;

public sealed class DashAbilityBehaviorImplementation : IAbilityBehavior<Character3D.TargetData>, IDisposable
{
	private AbilityDelayTimer? _delayTimer;
	private uint _originalCollisionMask;

	public void OnStarted(AbilityBehaviorContext context, Character3D.TargetData data)
	{
		if (context.Owner is not ForgeEntity ownerNode)
		{
			context.InstanceHandle.End();
			return;
		}

		_delayTimer = new AbilityDelayTimer();
		_delayTimer.Initialize(context, 0.25f);

		context.AbilityHandle.CommitAbility();

		CharacterBody3D parentNode = ownerNode.GetParent<CharacterBody3D>();

		// Store original collision mask and disable enemy collision layer (adjust layer number as needed)
		_originalCollisionMask = parentNode.CollisionMask;
		parentNode.CollisionMask &= ~1u; // Assuming enemies are on layer 3 (index 2)

		parentNode.Velocity = data.Direction * 20f;

		var tree = Engine.GetMainLoop() as SceneTree;
		tree?.Root.AddChild(_delayTimer);
	}

	public void OnEnded(AbilityBehaviorContext context)
	{
		// Restore original collision mask
		if (context.Owner is ForgeEntity ownerNode)
		{
			CharacterBody3D parentNode = ownerNode.GetParent<CharacterBody3D>();
			parentNode.CollisionMask = _originalCollisionMask;
		}

		// Ensure pending timer is cancelled if ability ends early.
		_delayTimer?.CancelAndFree();
		_delayTimer = null;
	}

	public void Dispose()
	{
		_delayTimer?.Dispose();
		GC.SuppressFinalize(this);
	}
}
