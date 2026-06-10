// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Effects;

namespace Gamesmiths.Forge.Godot.Core.Statescript.VariableTypes;

/// <summary>
/// Built-in object variable type for <see cref="ActiveEffectHandle"/> references produced when effects are applied.
/// </summary>
internal sealed class ActiveEffectHandleObjectVariableType : StatescriptObjectVariableType<ActiveEffectHandle>
{
	public override string TypeId => "ActiveEffectHandle";

	public override string DisplayName => "Active Effect";

	public override string FormatDebugValue(object? value)
	{
		if (value is not ActiveEffectHandle handle)
		{
			return "<null>";
		}

		return handle.IsValid ? "ActiveEffect(valid)" : "ActiveEffect(invalid)";
	}
}
