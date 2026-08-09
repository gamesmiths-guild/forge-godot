// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Godot.Resources;
using Godot;

namespace Gamesmiths.Forge.Godot.Core;

public partial class ForgeBootstrap : Node
{
	public override void _Ready()
	{
		_ = new ForgeManagers(ForgeTagsSource.LoadRegisteredTags());
	}
}
