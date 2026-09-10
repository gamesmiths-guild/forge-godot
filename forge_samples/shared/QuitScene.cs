// Copyright © Gamesmiths Guild.

using Godot;

namespace Gamesmiths.Forge.Example;

public partial class QuitScene : Node
{
	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event.IsActionPressed("ui_cancel"))
		{
			GetTree().Root.GetNodeOrNull<Main>("Main")?.ReturnToHub();
		}
	}
}
