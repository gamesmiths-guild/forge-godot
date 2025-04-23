// Copyright © 2025 Gamesmiths Guild.

using Godot;
using Godot.Collections;

namespace Gamesmiths.Forge.GameplayTags.Godot;

[Tool]
public partial class ForgePluginData : Resource
{
	[Export]
	public required Array<string> RegisteredTags { get; set; }

	[Export]
	public required Array<string> RegisteredCues { get; set; }
}
