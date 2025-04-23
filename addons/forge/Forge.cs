// Copyright © 2025 Gamesmiths Guild.

using Gamesmiths.Forge.GameplayCues;
using Gamesmiths.Forge.GameplayTags;
using Gamesmiths.Forge.GameplayTags.Godot;
using Godot;

namespace Gamesmiths.Forge.Godot;

public partial class Forge : Node
{
	public static GameplayTagsManager? TagsManager { get; set; }

	public static GameplayCuesManager? CuesManager { get; set; }

	public override void _Ready()
	{
		ForgePluginData pluginData =
			ResourceLoader.Load<ForgePluginData>("res://addons/forge/forge_data.tres");

		TagsManager = new GameplayTagsManager([.. pluginData.RegisteredTags]);
		CuesManager = new GameplayCuesManager();
	}
}
