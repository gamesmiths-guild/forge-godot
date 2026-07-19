// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Abilities;

namespace Gamesmiths.Forge.Godot.Core.Statescript.VariableTypes;

/// <summary>
/// Built-in object variable type for <see cref="AbilityData"/> values used by ability grant and lookup nodes.
/// </summary>
internal sealed class AbilityDataObjectVariableType : StatescriptObjectVariableType<AbilityData>
{
	public override string TypeId => "AbilityData";

	public override string DisplayName => "Ability Data";

	public override string FormatDebugValue(object? value)
	{
		if (value is not AbilityData abilityData)
		{
			return "<null>";
		}

		return $"AbilityData({abilityData.Name})";
	}
}
