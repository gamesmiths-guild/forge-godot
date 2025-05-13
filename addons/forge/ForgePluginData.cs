// Copyright © Gamesmiths Guild.

using Godot;
using Godot.Collections;

namespace Gamesmiths.Forge.GameplayTags.Godot;

[Tool]
public partial class ForgePluginData : Resource
{
	[Export]
	public Array<string>? RegisteredTags { get; set; }

	[Export]
	public Array<string>? RegisteredCues { get; set; }
}
