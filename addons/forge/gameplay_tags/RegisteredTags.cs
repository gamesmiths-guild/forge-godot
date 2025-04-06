// Copyright © 2025 Gamesmiths Guild.

using Godot;
using Godot.Collections;

namespace Gamesmiths.Forge.GameplayTags.Godot;

[Tool]
public partial class RegisteredTags : Resource
{
	[Export]
	public Array<string> Tags { get; set; }
}
