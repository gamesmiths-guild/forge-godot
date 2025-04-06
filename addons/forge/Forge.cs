// Copyright © 2025 Gamesmiths Guild.

using Gamesmiths.Forge.GameplayCues;
using Gamesmiths.Forge.GameplayTags;
using Gamesmiths.Forge.GameplayTags.Godot;
using Godot;

namespace Gamesmiths.Forge.Godot;

public partial class Forge : Node
{
	public static GameplayTagsManager TagsManager { get; set; }

	public static GameplayCuesManager CuesManager { get; set; }

	public override void _Ready()
	{
		RegisteredTags registeredTags =
			ResourceLoader.Load<RegisteredTags>("res://addons/forge/gameplay_tags/registered_tags.tres");

		TagsManager = new GameplayTagsManager([.. registeredTags.Tags]);
		CuesManager = new GameplayCuesManager();
	}
}
