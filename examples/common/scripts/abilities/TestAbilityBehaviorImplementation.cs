// Copyright © Gamesmiths Guild.

using System;
using Gamesmiths.Forge.Abilities;
using Godot;

namespace Gamesmiths.Forge.Example;

public sealed class TestAbilityBehaviorImplementation : IAbilityBehavior, IDisposable
{
	private readonly string _testProperty;
	private AbilityDelayTimer? _delayNode;

	public TestAbilityBehaviorImplementation(string testProperty)
	{
		_testProperty = testProperty;
	}

	public void OnStarted(AbilityBehaviorContext context)
	{
		GD.Print($"Activating ability with TestProperty: {_testProperty}");

		// Commit immediately.
		context.AbilityHandle.CommitAbility();

		// Create a helper node that manages a Timer and ends the instance after 1s.
		_delayNode = new AbilityDelayTimer();
		_delayNode.Initialize(context, _testProperty, 1.0);

		// Prefer attaching under a relevant node (e.g., the scene root).
		var tree = Engine.GetMainLoop() as SceneTree;
		tree?.Root.AddChild(_delayNode);
	}

	public void OnEnded(AbilityBehaviorContext context)
	{
		GD.Print($"Deactivating ability with TestProperty: {_testProperty}");

		// Ensure pending timer is cancelled if ability ends early.
		_delayNode?.CancelAndFree();
		_delayNode = null;
	}

	public void Dispose()
	{
		_delayNode?.Dispose();
		GC.SuppressFinalize(this);
	}
}
