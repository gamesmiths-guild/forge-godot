// Copyright © Gamesmiths Guild.

using Godot;

namespace Gamesmiths.Forge.Example;

public partial class QuitScene : Node
{
	public override void _EnterTree()
	{
		// Every demo has a QuitScene, and package installs don't ship the repository's project.godot input map.
		AddMissingAction(
			"move_up",
			KeyEvent(Key.W),
			KeyEvent(Key.Up),
			ButtonEvent(JoyButton.DpadUp),
			AxisEvent(JoyAxis.LeftY, -1));
		AddMissingAction(
			"move_down",
			KeyEvent(Key.S),
			KeyEvent(Key.Down),
			ButtonEvent(JoyButton.DpadDown),
			AxisEvent(JoyAxis.LeftY, 1));
		AddMissingAction(
			"move_left",
			KeyEvent(Key.A),
			KeyEvent(Key.Left),
			ButtonEvent(JoyButton.DpadLeft),
			AxisEvent(JoyAxis.LeftX, -1));
		AddMissingAction(
			"move_right",
			KeyEvent(Key.D),
			KeyEvent(Key.Right),
			ButtonEvent(JoyButton.DpadRight),
			AxisEvent(JoyAxis.LeftX, 1));
		AddMissingAction("wait", KeyEvent(Key.Space));
		AddMissingAction(
			"skill_1", new InputEventMouseButton { ButtonIndex = MouseButton.Left, Device = -1 }, KeyEvent(Key.Key1));
		AddMissingAction("skill_2", KeyEvent(Key.Key2), KeyEvent(Key.Space));
		AddMissingAction("skill_3", KeyEvent(Key.Key3));
		AddMissingAction("interact", KeyEvent(Key.E));
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event.IsActionPressed("ui_cancel"))
		{
			GetTree().Root.GetNodeOrNull<Main>("Main")?.ReturnToHub();
		}
	}

	private static void AddMissingAction(StringName action, params InputEvent[] events)
	{
		if (InputMap.HasAction(action))
		{
			return;
		}

		InputMap.AddAction(action, 0.2f);

		foreach (InputEvent inputEvent in events)
		{
			InputMap.ActionAddEvent(action, inputEvent);
		}
	}

	private static InputEventKey KeyEvent(Key key)
	{
		return new InputEventKey { PhysicalKeycode = key, Device = -1 };
	}

	private static InputEventJoypadButton ButtonEvent(JoyButton button)
	{
		return new InputEventJoypadButton { ButtonIndex = button, Device = -1 };
	}

	private static InputEventJoypadMotion AxisEvent(JoyAxis axis, float value)
	{
		return new InputEventJoypadMotion { Axis = axis, AxisValue = value, Device = -1 };
	}
}
