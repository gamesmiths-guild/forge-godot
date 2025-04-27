// Copyright © 2025 Gamesmiths Guild.

using Godot;

namespace Gamesmiths.Forge.GameplayEffects.Godot;

[Tool]
public partial class GameplayEffect : Node
{
	[Export]
	public GameplayEffectData? GameplayEffectData { get; set; }
}
