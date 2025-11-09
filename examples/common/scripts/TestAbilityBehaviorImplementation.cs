// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Abilities;
using Godot;

namespace Gamesmiths.Forge.Example;

public class TestAbilityBehaviorImplementation : IAbilityBehavior
{
	private readonly string _testProperty;

	public TestAbilityBehaviorImplementation(string testProperty)
	{
		_testProperty = testProperty;
	}

	public void OnStarted(AbilityBehaviorContext context)
	{
		GD.Print($"Activating ability with TestProperty: {_testProperty}");
		context.AbilityHandle.CommitAbility();
		context.InstanceHandle.End();
	}

	public void OnEnded(AbilityBehaviorContext context)
	{
		GD.Print($"Deactivating ability with TestProperty: {_testProperty}");
	}
}
