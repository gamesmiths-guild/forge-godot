// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Effects.Calculator;
using Gamesmiths.Forge.Godot.Resources;
using Gamesmiths.Forge.Godot.Resources.Calculators;
using Godot;

namespace Gamesmiths.Forge.Example;

[Tool]
[GlobalClass]
public partial class DamageExecutionWrapper : ForgeCustomExecution
{
	[Export]
	public required ForgeEffectData FireEffectData { get; set; }

	public override CustomExecution GetExecutionClass()
	{
		return new DamageExecution(FireEffectData.GetEffectData());
	}
}
