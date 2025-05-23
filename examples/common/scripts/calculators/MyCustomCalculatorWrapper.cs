// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Godot.Resources.Calculators;
using Godot;

using ForgeCustomCalculator = Gamesmiths.Forge.GameplayEffects.Calculator.CustomModifierMagnitudeCalculator;

namespace Gamesmiths.Forge.Example;

[Tool]
[GlobalClass]
public partial class MyCustomCalculatorWrapper : CustomCalculator
{
	public override ForgeCustomCalculator GetCustomCalculatorClass()
	{
		return new MyCustomCalculator();
	}
}
