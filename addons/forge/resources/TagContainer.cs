// Copyright © Gamesmiths Guild.

using System.Collections.Generic;
using System.Diagnostics;
using Gamesmiths.Forge.GameplayTags;
using Godot;
using Godot.Collections;

using static Gamesmiths.Forge.Godot.Core.Forge;

namespace Gamesmiths.Forge.Godot.Resources;

[Tool]
[GlobalClass]
[Icon("uid://ddeg62ps243fn")]
public partial class TagContainer : ForgeResource
{
	[Export]
	public Array<string>? ContainerTags { get; set; }

	public GameplayTagContainer GetTagContainer()
	{
		Debug.Assert(TagsManager is not null, $"{TagsManager} should have been initialized by the Forge plugin.");

		if (ContainerTags is null)
		{
			return new GameplayTagContainer(TagsManager);
		}

		var tags = new HashSet<GameplayTag>();

		foreach (var tag in ContainerTags)
		{
			try
			{
				tags.Add(GameplayTag.RequestTag(TagsManager, tag));
			}
			catch (GameplayTagNotRegisteredException)
			{
				GD.PushWarning($"Tag [{tag}] is not registered.");
			}
		}

		return new GameplayTagContainer(TagsManager, tags);
	}
}
