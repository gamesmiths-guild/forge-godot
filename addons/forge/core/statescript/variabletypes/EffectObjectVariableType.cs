// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Effects;

namespace Gamesmiths.Forge.Godot.Core.Statescript.VariableTypes;

/// <summary>
/// Built-in object variable type for <see cref="Effect"/> instances.
/// </summary>
internal sealed class EffectObjectVariableType : StatescriptObjectVariableType<Effect>
{
	public override string TypeId => "Effect";

	public override string DisplayName => "Effect";

	public override string FormatDebugValue(object? value)
	{
		return value is Effect effect ? effect.EffectData.Name : "<null>";
	}
}
