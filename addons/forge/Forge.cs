// Copyright © 2025 Gamesmiths Guild.

using Gamesmiths.Forge.GameplayCues;
using Gamesmiths.Forge.GameplayTags;
using Gamesmiths.Forge.Godot.GameplayTags;
using Godot;

namespace Gamesmiths.Forge.Godot;

public partial class Forge : Node
{
	public static GameplayTagsManager TagsManager { get; set; }

	public static GameplayCuesManager CuesManager { get; set; }

	public override void _Ready()
	{
		if (TagsManager is not null || CuesManager is not null)
		{
			GD.PrintErr("Multiple autoload instances detected!");
			return;
		}

		GD.Print("Forge autoload initialized.");

		RegisteredTags registeredTags =
			ResourceLoader.Load<RegisteredTags>("res://addons/forge/gameplay_tags/registered_tags.tres");

		TagsManager = new GameplayTagsManager([.. registeredTags.Tags]);
		CuesManager = new GameplayCuesManager();
	}
}
