// Copyright © Gamesmiths Guild.

using Godot;

namespace Gamesmiths.Forge.Example;

public partial class QuitScene : Node
{
	public override void _Input(InputEvent @event)
	{
		if (@event is InputEventKey eventKey && eventKey.Pressed && eventKey.Keycode == Key.Escape)
		{
			GetTree().Root.GetNode<Main>("Main").ChangeScene("uid://c555ix6yk55jj");
		}
	}
}
