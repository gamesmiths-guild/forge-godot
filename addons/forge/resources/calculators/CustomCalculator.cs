// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.GameplayEffects.Calculator;
using Gamesmiths.Forge.Godot.Nodes;
using Godot;

namespace Gamesmiths.Forge.Godot.Resources.Calculators;

[Tool]
[GlobalClass]
[Icon("uid://b3mnlhfvbttw0")]
public abstract partial class CustomCalculator : ForgeResource
{
	public abstract CustomModifierMagnitudeCalculator GetCustomCalculatorClass();
}
