// Copyright © Gamesmiths Guild.

using Godot;
using Godot.Collections;

namespace Gamesmiths.Forge.Godot.Core;

[Tool]
public partial class ForgeData : Resource
{
	[Export]
	public Array<string> RegisteredTags { get; set; } = [];
}
