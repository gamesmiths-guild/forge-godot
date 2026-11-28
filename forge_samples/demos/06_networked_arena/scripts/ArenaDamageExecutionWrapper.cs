// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Effects.Calculator;
using Gamesmiths.Forge.Godot.Resources.Calculators;
using Godot;

namespace Gamesmiths.Forge.Example;

/// <summary>
/// The arena's damage, as a resource an effect's executions take.
/// </summary>
[Tool]
[GlobalClass]
public partial class ArenaDamageExecutionWrapper : ForgeCustomExecution
{
	/// <inheritdoc/>
	public override CustomExecution GetExecutionClass()
	{
		return new ArenaDamageExecution();
	}
}
