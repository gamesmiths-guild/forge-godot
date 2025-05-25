// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Effects.Calculator;
using Godot;

namespace Gamesmiths.Forge.Godot.Resources.Calculators;

[Tool]
[GlobalClass]
[Icon("uid://b3mnlhfvbttw0")]
public abstract partial class ForgeCustomCalculator : Resource
{
	public abstract CustomModifierMagnitudeCalculator GetCustomCalculatorClass();
}
