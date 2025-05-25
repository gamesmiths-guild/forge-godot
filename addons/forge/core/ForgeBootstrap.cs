// Copyright © Gamesmiths Guild.

using Godot;

namespace Gamesmiths.Forge.Godot.Core;

public partial class ForgeBootstrap : Node
{
	public override void _Ready()
	{
		ForgeContext.Initialize();
	}
}
