// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Abilities;
using Gamesmiths.Forge.Godot.Resources.Abilities;
using Godot;

namespace Gamesmiths.Forge.Example;

[Tool]
[GlobalClass]
public partial class TestAbilityBehavior : ForgeAbilityBehavior
{
	[Export]
	public string TestProperty { get; set; } = "DefaultValue";

	public override IAbilityBehavior GetBehavior()
	{
		return new TestAbilityBehaviorImplementation(TestProperty);
	}
}
