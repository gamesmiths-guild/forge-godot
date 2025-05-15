// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.GameplayCues;
using Gamesmiths.Forge.GameplayTags;
using Godot;
using Godot.Collections;

namespace Gamesmiths.Forge.Godot.Core;

public partial class Forge : Node
{
	private ForgePluginData? _pluginData;

	public static GameplayTagsManager? TagsManager { get; set; }

	public static GameplayCuesManager? CuesManager { get; private set; }

	public static Array<string>? RegisteredTags { get; private set; }

	public static Array<string>? RegisteredCues { get; private set; }

	public override void _Ready()
	{
		_pluginData = ResourceLoader.Load<ForgePluginData>("uid://8j4xg16o3qnl");

		_pluginData.RegisteredTags ??= [];
		_pluginData.RegisteredCues ??= [];

		RegisteredTags = _pluginData.RegisteredTags;
		RegisteredCues = _pluginData.RegisteredCues;

		TagsManager = new GameplayTagsManager([.. _pluginData.RegisteredTags]);
		CuesManager = new GameplayCuesManager();
	}
}
