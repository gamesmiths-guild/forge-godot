// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.GameplayCues;
using Gamesmiths.Forge.GameplayTags;
using Godot;
using Godot.Collections;

namespace Gamesmiths.Forge.Godot.Core;

public partial class Forge : Node
{
	public static GameplayTagsManager? TagsManager { get; private set; }

	public static GameplayCuesManager? CuesManager { get; private set; }

	public static Array<string>? RegisteredTags { get; private set; }

	public static Array<string>? RegisteredCues { get; private set; }

	public static void RebuildTagsManager()
	{
		ForgeData pluginData = ResourceLoader.Load<ForgeData>("uid://8j4xg16o3qnl");

		pluginData.RegisteredTags ??= [];

		TagsManager = new GameplayTagsManager([.. pluginData.RegisteredTags]);
	}

	public override void _Ready()
	{
		Initialize();
	}

	private static void Initialize()
	{
		ForgeData pluginData = ResourceLoader.Load<ForgeData>("uid://8j4xg16o3qnl");

		pluginData.RegisteredTags ??= [];
		pluginData.RegisteredCues ??= [];

		RegisteredTags = pluginData.RegisteredTags;
		RegisteredCues = pluginData.RegisteredCues;

		TagsManager = new GameplayTagsManager([.. pluginData.RegisteredTags]);
		CuesManager = new GameplayCuesManager();
	}
}
