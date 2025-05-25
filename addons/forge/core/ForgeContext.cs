// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Cues;
using Gamesmiths.Forge.Tags;
using Godot;
using Godot.Collections;

namespace Gamesmiths.Forge.Godot.Core;

public partial class ForgeContext : Node
{
	public static TagsManager? TagsManager { get; private set; }

	public static CuesManager? CuesManager { get; private set; }

	public static Array<string>? RegisteredTags { get; private set; }

	public static Array<string>? RegisteredCues { get; private set; }

	public static void Initialize()
	{
		ForgeData pluginData = ResourceLoader.Load<ForgeData>("uid://8j4xg16o3qnl");

		pluginData.RegisteredTags ??= [];
		pluginData.RegisteredCues ??= [];

		RegisteredTags = pluginData.RegisteredTags;
		RegisteredCues = pluginData.RegisteredCues;

		TagsManager = new TagsManager([.. pluginData.RegisteredTags]);
		CuesManager = new CuesManager();
	}

	public static void RebuildTagsManager()
	{
		ForgeData pluginData = ResourceLoader.Load<ForgeData>("uid://8j4xg16o3qnl");

		pluginData.RegisteredTags ??= [];

		TagsManager = new TagsManager([.. pluginData.RegisteredTags]);
	}
}
