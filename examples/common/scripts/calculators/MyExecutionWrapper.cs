// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Godot.Resources.Calculators;
using Godot;

using ForgeExecution = Gamesmiths.Forge.GameplayEffects.Calculator.Execution;

namespace Gamesmiths.Forge.Example;

[Tool]
[GlobalClass]
public partial class MyExecutionWrapper : Execution
{
	[Export]
	public float Expoent { get; set; }

	public override ForgeExecution GetExecutionClass()
	{
		return new MyExecution();
	}
}
