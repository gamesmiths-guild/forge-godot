// Copyright © Gamesmiths Guild.

using System;
using Gamesmiths.Forge.Godot.Core;
using Gamesmiths.Forge.Tags;
using Godot;

namespace Gamesmiths.Forge.Godot.Resources;

[Tool]
[GlobalClass]
public partial class ForgeTag : Resource
{
	[Export]
	public required string Tag { get; set; }

	public Tag GetTag()
	{
		if (string.IsNullOrEmpty(Tag))
		{
			throw new ArgumentNullException(nameof(Tag));
		}

		return Tags.Tag.RequestTag(ForgeManagers.Instance.TagsManager, Tag);
	}
}
