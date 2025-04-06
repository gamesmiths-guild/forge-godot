// Copyright © 2025 Gamesmiths Guild.

using System.Collections.Generic;
using Godot;
using Godot.Collections;

using static Gamesmiths.Forge.Godot.Forge;

namespace Gamesmiths.Forge.GameplayTags.Godot;

[Tool]
[GlobalClass]
public partial class TagContainer : Resource
{
	[Export]
	public Array<string> ContainerTags { get; set; } = [];

	public GameplayTagContainer GetTagContainer()
	{
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
