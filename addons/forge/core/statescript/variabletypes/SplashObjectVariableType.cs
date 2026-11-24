// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Godot.Resources;

namespace Gamesmiths.Forge.Godot.Core.Statescript.VariableTypes;

/// <summary>
/// Object variable type for <see cref="ForgeSplashData"/> references.
/// </summary>
internal sealed class SplashObjectVariableType : StatescriptObjectVariableType<ForgeSplashData>
{
	public override string TypeId => "Splash";

	public override string DisplayName => "Splash";
}
