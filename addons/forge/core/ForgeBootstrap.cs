// Copyright © Gamesmiths Guild.

using Godot;

namespace Gamesmiths.Forge.Godot.Core;

public partial class ForgeBootstrap : Node
{
	public override void _Ready()
	{
		ForgeData pluginData = ResourceLoader.Load<ForgeData>("uid://8j4xg16o3qnl");
		_ = new ForgeManagers(pluginData);
	}
}
