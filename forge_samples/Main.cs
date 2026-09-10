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
		ChangeScene(GD.Load<PackedScene>(scenePath));
	}

	public void ChangeScene(PackedScene scene)
	{
		_currentScene?.QueueFree();

		_currentScene = scene.Instantiate();

		if (_currentScene is Hub hub)
		{
			hub.DemoSelected += ChangeScene;
		}

		AddChild(_currentScene);
	}
}
