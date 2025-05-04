// Copyright © 2025 Gamesmiths Guild.

using System;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.GameplayEffects.Calculator;

namespace Gamesmiths.Forge.Example;

public class MyCustomCalculator : CustomModifierMagnitudeCalculator
{
	public override float CalculateBaseMagnitude(GameplayEffects.GameplayEffect effect, IForgeEntity target)
	{
		throw new NotImplementedException();
	}
}
