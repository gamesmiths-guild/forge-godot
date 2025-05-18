// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Godot.Resources;
using Godot;

namespace Gamesmiths.Forge.Godot.Nodes;

[Tool]
[GlobalClass]
[Icon("uid://bpl454nqdpfjx")]
public partial class GameplayEffect : ForgeNode
{
	[Export]
	public GameplayEffectData? GameplayEffectData { get; set; }
}
