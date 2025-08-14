// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;
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

#if DEBUG
		Validation.Enabled = true;
#else
		Validation.Enabled = false;
#endif

		TagsManager = new TagsManager([.. pluginData.RegisteredTags]);
		CuesManager = new CuesManager();
	}
}
