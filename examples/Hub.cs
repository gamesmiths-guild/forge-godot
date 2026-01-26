// Copyright © Gamesmiths Guild.

using Godot;

namespace Gamesmiths.Forge.Example;

public partial class Hub : Control
{
	public void OnDemoA()
	{
		GetTree().Root.GetNode<Main>("Main")
			.ChangeScene("uid://ca1f6valfuo1n");
	}

	public void OnDemoB()
	{
		GetTree().Root.GetNode<Main>("Main")
			.ChangeScene("uid://dpmrom6ut67fe");
	}
}
