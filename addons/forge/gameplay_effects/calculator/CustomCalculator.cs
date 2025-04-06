// Copyright © 2025 Gamesmiths Guild.

using Godot;

namespace Gamesmiths.Forge.GameplayEffects.Calculator.Godot;

[Tool]
[GlobalClass]
public abstract partial class CustomCalculator : Resource
{
	public abstract CustomModifierMagnitudeCalculator GetCustomCalculatorClass();
}
