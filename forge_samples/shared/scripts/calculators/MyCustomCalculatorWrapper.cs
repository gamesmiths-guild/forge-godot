// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Effects.Calculator;
using Gamesmiths.Forge.Godot.Resources.Calculators;
using Godot;

namespace Gamesmiths.Forge.Example;

[Tool]
[GlobalClass]
public partial class MyCustomCalculatorWrapper : ForgeCustomCalculator
{
	public override CustomModifierMagnitudeCalculator GetCustomCalculatorClass()
	{
		return new MyCustomCalculator();
	}
}
