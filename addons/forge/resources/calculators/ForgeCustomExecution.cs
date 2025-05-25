// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Effects.Calculator;
using Godot;

namespace Gamesmiths.Forge.Godot.Resources.Calculators;

[Tool]
[GlobalClass]
[Icon("uid://dy874wbbpsewt")]
public abstract partial class ForgeCustomExecution : Resource
{
	public abstract CustomExecution GetExecutionClass();
}
