// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Effects.Calculator;
using Gamesmiths.Forge.Godot.Resources.Calculators;
using Godot;

namespace Gamesmiths.Forge.Example;

[Tool]
[GlobalClass]
public partial class MyExecutionWrapper : ForgeCustomExecution
{
	[Export]
	public float Expoent { get; set; }

	public override CustomExecution GetExecutionClass()
	{
		return new MyExecution();
	}
}
