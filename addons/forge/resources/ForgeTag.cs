// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Godot.Core;
using Gamesmiths.Forge.Tags;
using Godot;

namespace Gamesmiths.Forge.Godot.Resources;

[Tool]
[GlobalClass]
[Icon("uid://vsfdeevye6jm")]
public partial class ForgeTag : Resource
{
	[Export]
	public required string Tag { get; set; }

	public Tag GetTag()
	{
		return Tags.Tag.RequestTag(ForgeManagers.Instance.TagsManager, Tag);
	}
}
