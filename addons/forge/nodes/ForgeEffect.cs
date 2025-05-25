// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Godot.Resources;
using Godot;

namespace Gamesmiths.Forge.Godot.Nodes;

[GlobalClass]
[Icon("uid://bpl454nqdpfjx")]
public partial class ForgeEffect : Node
{
	[Export]
	public ForgeEffectData? EffectData { get; set; }
}
