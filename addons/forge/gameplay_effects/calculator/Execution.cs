// Copyright © Gamesmiths Guild.

using Godot;

using ForgeExecution = Gamesmiths.Forge.GameplayEffects.Calculator.Execution;

namespace Gamesmiths.Forge.GameplayEffects.Calculator.Godot;

[Tool]
[GlobalClass]
public abstract partial class Execution : Resource
{
	public abstract ForgeExecution GetExecutionClass();
}
