// Copyright © Gamesmiths Guild.

using System;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Effects;
using Gamesmiths.Forge.Effects.Calculator;

namespace Gamesmiths.Forge.Example;

public class MyExecution : CustomExecution
{
	public override ModifierEvaluatedData[] EvaluateExecution(
		Effect effect, IForgeEntity target, EffectEvaluatedData? effectEvaluatedData)
	{
		throw new NotImplementedException();
	}
}
