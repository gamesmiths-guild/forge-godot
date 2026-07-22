// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Abilities;

namespace Gamesmiths.Forge.Godot.Core.Statescript.VariableTypes;

/// <summary>
/// Built-in object variable type for <see cref="AbilityHandle"/> references produced by ability grants and lookups.
/// </summary>
internal sealed class AbilityHandleObjectVariableType : StatescriptObjectVariableType<AbilityHandle>
{
	public override string TypeId => "AbilityHandle";

	public override string DisplayName => "Ability";

	public override string FormatDebugValue(object? value)
	{
		if (value is not AbilityHandle handle)
		{
			return "<null>";
		}

		return handle.IsValid ? "Ability(valid)" : "Ability(invalid)";
	}
}
