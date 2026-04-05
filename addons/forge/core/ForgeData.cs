// Copyright © Gamesmiths Guild.

using Godot;
using Godot.Collections;

namespace Gamesmiths.Forge.Godot.Core;

[Tool]
public partial class ForgeData : Resource
{
	public const string ForgeDataResourcePath = "res://addons/forge/core/forge_data.tres";

	[Export]
	public Array<string> RegisteredTags { get; set; } = [];
}
