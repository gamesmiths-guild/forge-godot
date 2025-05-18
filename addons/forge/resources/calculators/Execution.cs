// Copyright © Gamesmiths Guild.

using Godot;

using ForgeExecution = Gamesmiths.Forge.GameplayEffects.Calculator.Execution;

namespace Gamesmiths.Forge.Godot.Resources.Calculators;

[Tool]
[GlobalClass]
[Icon("uid://dy874wbbpsewt")]
public abstract partial class Execution : Resource
{
	public abstract ForgeExecution GetExecutionClass();
}
