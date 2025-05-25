// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Cues;
using Gamesmiths.Forge.Tags;

namespace Gamesmiths.Forge.Godot.Core;

public class ForgeManagers
{
	public static ForgeManagers Instance { get; private set; } = null!;

	public TagsManager TagsManager { get; private set; }

	public CuesManager CuesManager { get; private set; }

	public ForgeManagers(ForgeData pluginData)
	{
		Instance = this;

		TagsManager = new TagsManager([.. pluginData.RegisteredTags]);
		CuesManager = new CuesManager();
	}
}
