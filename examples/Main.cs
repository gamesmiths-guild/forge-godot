// Copyright © Gamesmiths Guild.

using Godot;

namespace Gamesmiths.Forge.Example;

public partial class Main : Node
{
	private Node? _currentScene;

	public override void _Ready()
	{
		ChangeScene("uid://c555ix6yk55jj");
	}

	public void ChangeScene(string scenePath)
	{
		_currentScene?.QueueFree();

		PackedScene packedScene = GD.Load<PackedScene>(scenePath);
		_currentScene = packedScene.Instantiate();
		AddChild(_currentScene);
	}
}
