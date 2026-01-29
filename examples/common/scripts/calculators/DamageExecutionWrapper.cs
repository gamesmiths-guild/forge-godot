// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Effects.Calculator;
using Gamesmiths.Forge.Godot.Resources.Calculators;
using Godot;

namespace Gamesmiths.Forge.Example;

[Tool]
[GlobalClass]
public partial class DamageExecutionWrapper : ForgeCustomExecution
{
	[Export]
	public DamageType DamageType { get; set; } = DamageType.Physical;

	public override CustomExecution GetExecutionClass()
	{
		return new DamageExecution(DamageType);
	}
}
