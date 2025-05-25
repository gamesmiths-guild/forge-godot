// Copyright © Gamesmiths Guild.

using System.Collections.Generic;
using Gamesmiths.Forge.Godot.Core;
using Gamesmiths.Forge.Tags;
using Godot;
using Godot.Collections;

namespace Gamesmiths.Forge.Godot.Resources;

[Tool]
[GlobalClass]
[Icon("uid://ddeg62ps243fn")]
public partial class ForgeTagContainer : Resource
{
	[Export]
	public Array<string>? ContainerTags { get; set; }

	public TagContainer GetTagContainer()
	{
		if (ContainerTags is null)
		{
			return new TagContainer(ForgeManagers.Instance.TagsManager);
		}

		var tags = new HashSet<Tag>();

		foreach (var tag in ContainerTags)
		{
			try
			{
				tags.Add(Tag.RequestTag(ForgeManagers.Instance.TagsManager, tag));
			}
			catch (TagNotRegisteredException)
			{
				GD.PushWarning($"Tag [{tag}] is not registered.");
			}
		}

		return new TagContainer(ForgeManagers.Instance.TagsManager, tags);
	}
}
