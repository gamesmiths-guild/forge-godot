// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.GameplayEffects.Calculator;
using Godot;

namespace Gamesmiths.Forge.Godot.Resources.Calculators;

[Tool]
[GlobalClass]
public abstract partial class CustomCalculator : Resource
{
	public abstract CustomModifierMagnitudeCalculator GetCustomCalculatorClass();
}
