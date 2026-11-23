// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Godot.Resources;

namespace Gamesmiths.Forge.Godot.Core.Statescript.VariableTypes;

/// <summary>
/// Object variable type for <see cref="ForgeHitscanData"/> references.
/// </summary>
internal sealed class HitscanObjectVariableType : StatescriptObjectVariableType<ForgeHitscanData>
{
	public override string TypeId => "Hitscan";

	public override string DisplayName => "Hitscan";
}
