// Copyright © Gamesmiths Guild.

using System;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.GameplayEffects;
using Gamesmiths.Forge.GameplayEffects.Calculator;

namespace Gamesmiths.Forge.Example;

public class MyExecution : Execution
{
	public override ModifierEvaluatedData[] CalculateExecution(GameplayEffect effect, IForgeEntity target)
	{
		throw new NotImplementedException();
	}
}
