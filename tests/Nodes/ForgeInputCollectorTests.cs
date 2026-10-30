// Copyright © Gamesmiths Guild.

using System;
using FluentAssertions;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Godot.Core;
using Gamesmiths.Forge.Godot.Core.Statescript.Nodes.State;
using Gamesmiths.Forge.Godot.Nodes;
using Gamesmiths.Forge.Godot.Resources.Abilities;
using Gamesmiths.Forge.Godot.Tests.Helpers;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Nodes;
using Gamesmiths.Forge.Statescript.Nodes.Action;
using GdUnit4;
using Godot;
using ForgeGraph = Gamesmiths.Forge.Statescript.Graph;
using ForgeInputActionNode = Gamesmiths.Forge.Statescript.Nodes.State.InputActionNode;
using NumericsVector2 = System.Numerics.Vector2;
using NumericsVector3 = System.Numerics.Vector3;

namespace Gamesmiths.Forge.Godot.Tests.Nodes;

/// <summary>
/// How the local player's input reaches a <see cref="ForgeEntity"/>: a <see cref="ForgeInputCollector"/> latches the
/// presses and releases it sees and submits one frame per physics tick, which the entity's tick then reads.
/// </summary>
[TestSuite]
public class ForgeInputCollectorTests
{
	private const double Step = 1.0 / 60.0;

	private const string Fire = "forge_test_fire";
	private const string Left = "forge_test_left";
	private const string Right = "forge_test_right";
	private const string Up = "forge_test_up";
	private const string Down = "forge_test_down";

	private static Window Root => ((SceneTree)Engine.GetMainLoop()).Root;

	[TestCase]
	[RequireGodotRuntime]
	public void A_collector_gives_its_entity_an_input_with_the_projects_actions_and_vectors()
	{
		WithProjectInput(() =>
		{
			ForgeEntity entity = CreatePlayer(out _, out Node3D body);

			try
			{
				entity.Input.Should().NotBeNull();
				entity.Input!.Actions.Actions.Should().Equal(Fire);
				entity.Input.Actions.Vectors.Should().ContainSingle()
					.Which.Should().Be(new InputVector(Left, Right, Up, Down));
			}
			finally
			{
				body.Free();
			}
		});
	}

	[TestCase]
	[RequireGodotRuntime]
	public void A_tap_between_two_physics_ticks_reaches_the_tick_as_a_press_and_a_release()
	{
		WithProjectInput(() =>
		{
			ForgeEntity entity = CreatePlayer(out ForgeInputCollector collector, out Node3D body);

			try
			{
				Press(collector, Fire);
				Release(collector, Fire);
				Tick(collector, entity);

				entity.Input!.WasPressed(Fire).Should().BeTrue();
				entity.Input.WasReleased(Fire).Should().BeTrue();
				entity.Input.IsHeld(Fire).Should().BeFalse();
			}
			finally
			{
				Input.ActionRelease(Fire);
				body.Free();
			}
		});
	}

	[TestCase]
	[RequireGodotRuntime]
	public void A_press_caught_as_the_tree_pauses_is_dropped()
	{
		WithProjectInput(() =>
		{
			ForgeEntity entity = CreatePlayer(out ForgeInputCollector collector, out Node3D body);

			try
			{
				Press(collector, Fire);
				collector._Notification((int)global::Godot.Node.NotificationPaused);
				collector._Notification((int)global::Godot.Node.NotificationUnpaused);
				Tick(collector, entity);

				entity.Input!.WasPressed(Fire).Should().BeFalse();
				entity.Input.IsHeld(Fire).Should().BeTrue("what is held is read afresh");
			}
			finally
			{
				Input.ActionRelease(Fire);
				body.Free();
			}
		});
	}

	[TestCase]
	[RequireGodotRuntime]
	public void What_changed_while_paused_is_read_afresh_without_a_press_or_release()
	{
		WithProjectInput(() =>
		{
			ForgeEntity entity = CreatePlayer(out ForgeInputCollector collector, out Node3D body);

			try
			{
				Tick(collector, entity);
				collector._Notification((int)global::Godot.Node.NotificationPaused);
				Input.ActionPress(Fire);
				collector._Notification((int)global::Godot.Node.NotificationUnpaused);
				Tick(collector, entity);

				entity.Input!.IsHeld(Fire).Should().BeTrue("a button pushed while paused counts as held once resumed");
				entity.Input.WasPressed(Fire).Should().BeFalse();

				collector._Notification((int)global::Godot.Node.NotificationPaused);
				Input.ActionRelease(Fire);
				collector._Notification((int)global::Godot.Node.NotificationUnpaused);
				Tick(collector, entity);

				entity.Input.IsHeld(Fire).Should().BeFalse();
				entity.Input.WasReleased(Fire).Should().BeFalse();
			}
			finally
			{
				Input.ActionRelease(Fire);
				body.Free();
			}
		});
	}

	[TestCase]
	[RequireGodotRuntime]
	public void A_held_action_is_pressed_once_and_then_only_held()
	{
		WithProjectInput(() =>
		{
			ForgeEntity entity = CreatePlayer(out ForgeInputCollector collector, out Node3D body);

			try
			{
				Press(collector, Fire);
				Tick(collector, entity);
				entity.Input!.WasPressed(Fire).Should().BeTrue();

				Tick(collector, entity);

				entity.Input.WasPressed(Fire).Should().BeFalse();
				entity.Input.IsHeld(Fire).Should().BeTrue();
			}
			finally
			{
				Input.ActionRelease(Fire);
				body.Free();
			}
		});
	}

	[TestCase]
	[RequireGodotRuntime]
	public void A_press_the_interface_consumed_stays_out_of_the_game_until_it_is_let_go()
	{
		WithProjectInput(() =>
		{
			ForgeEntity entity = CreatePlayer(out ForgeInputCollector collector, out Node3D body);
			GraphProcessor watch = WatchFire(entity);

			try
			{
				Tick(collector, entity);
				watch.FixedUpdateGraph(Step);

				// Godot's action state changes, but no unhandled event reaches the collector.
				Input.ActionPress(Fire);
				Tick(collector, entity);
				watch.FixedUpdateGraph(Step);

				entity.Input!.IsHeld(Fire).Should().BeFalse();
				entity.Input.WasPressed(Fire).Should().BeFalse();
				Pressed(watch).Should().BeFalse("a watch already listening takes a held button for a press");

				Release(collector, Fire);
				Tick(collector, entity);
				entity.Input.WasReleased(Fire).Should().BeFalse("the game never saw it go down");

				Press(collector, Fire);
				Tick(collector, entity);
				watch.FixedUpdateGraph(Step);

				entity.Input.WasPressed(Fire).Should().BeTrue();
				entity.Input.IsHeld(Fire).Should().BeTrue();
				Pressed(watch).Should().BeTrue();
			}
			finally
			{
				Input.ActionRelease(Fire);
				body.Free();
			}
		});
	}

	[TestCase]
	[RequireGodotRuntime]
	public void A_collector_leaving_the_tree_lets_go_of_what_it_held()
	{
		WithProjectInput(() =>
		{
			ForgeEntity entity = CreatePlayer(out ForgeInputCollector collector, out Node3D body);

			try
			{
				Press(collector, Fire);
				Tick(collector, entity);
				entity.Input!.IsHeld(Fire).Should().BeTrue();

				entity.RemoveChild(collector);
				entity._PhysicsProcess(Step);

				entity.Input.IsHeld(Fire).Should().BeFalse("nothing collects the button any more");
			}
			finally
			{
				Input.ActionRelease(Fire);
				collector.Free();
				body.Free();
			}
		});
	}

	[TestCase]
	[RequireGodotRuntime]
	public void Vectors_carry_the_projects_analog_actions()
	{
		WithProjectInput(() =>
		{
			ForgeEntity entity = CreatePlayer(out ForgeInputCollector collector, out Node3D body);

			try
			{
				Input.ActionPress(Right);
				Tick(collector, entity);

				entity.Input!.GetVector(Left, Right, Up, Down).Should().Be(new NumericsVector2(1, 0));
				entity.Input.GetAxis(Left, Right).Should().Be(1);
			}
			finally
			{
				Input.ActionRelease(Right);
				body.Free();
			}
		});
	}

	[TestCase]
	[RequireGodotRuntime]
	public void The_aim_is_carried_only_when_the_collector_has_an_aim_source()
	{
		WithProjectInput(() =>
		{
			ForgeEntity aimed = CreatePlayer(out ForgeInputCollector aimedCollector, out Node3D aimedBody);
			aimedCollector.AimSource = ForgeAimSource.MouseGround3D;
			aimedBody.Position = new Vector3(1, 0, 2);
			ForgeEntity unaimed = CreatePlayer(out ForgeInputCollector unaimedCollector, out Node3D unaimedBody);

			try
			{
				Tick(aimedCollector, aimed);
				Tick(unaimedCollector, unaimed);

				// With no camera to cast from, the mouse aim falls back to the body's own position.
				aimed.Input!.Current.HasAim.Should().BeTrue();
				aimed.Input.Current.AimPoint.Should().Be(new NumericsVector3(1, 0, 2));
				unaimed.Input!.Current.HasAim.Should().BeFalse();
			}
			finally
			{
				aimedBody.Free();
				unaimedBody.Free();
			}
		});
	}

	[TestCase]
	[RequireGodotRuntime]
	public void An_ability_bound_to_an_input_action_activates_on_the_ticks_press_with_the_aim()
	{
		WithProjectInput(() =>
		{
			ForgeEntity entity = CreatePlayer(out ForgeInputCollector collector, out Node3D body);
			body.Position = new Vector3(3, 0, 0);
			var behavior = new RecordingAimBehavior();

			var abilityData = new ForgeAbilityData
			{
				Name = "Fire",
				AbilityBehavior = behavior,
				InputAction = Fire,
				InputActivationData = new AimInputActivationData(),
			};

			try
			{
				entity.Abilities.GrantAbilityPermanently(abilityData.GetAbilityData(), 1, LevelComparison.Higher, null);

				Tick(collector, entity);
				behavior.Activations.Should().Be(0);

				Press(collector, Fire);
				Tick(collector, entity);
				Tick(collector, entity);

				behavior.Activations.Should().Be(1, "holding the action does not activate it again");
				behavior.Received!.Value.Origin.Should().Be(new Vector3(3, 0, 0));
				behavior.Received.Value.Direction.Should().Be(Vector3.Forward, "an input without aim aims ahead");
			}
			finally
			{
				Input.ActionRelease(Fire);
				body.Free();
			}
		});
	}

	[TestCase]
	[RequireGodotRuntime]
	public void An_aim_taken_from_elsewhere_is_turned_to_meet_its_point_from_the_entity()
	{
		WithProjectInput(() =>
		{
			ForgeEntity entity = CreatePlayer(out _, out Node3D body);
			body.Position = new Vector3(3, 0, 0);
			var behavior = new RecordingAimBehavior();

			var abilityData = new ForgeAbilityData
			{
				Name = "Fire",
				AbilityBehavior = behavior,
				InputAction = Fire,
				InputActivationData = new AimInputActivationData(),
			};

			try
			{
				entity.Abilities.GrantAbilityPermanently(abilityData.GetAbilityData(), 1, LevelComparison.Higher, null);

				// A camera above the origin looking at a point ahead of it, and a ground aim at a point on a ledge.
				var point = new NumericsVector3(0, 1, -10);
				Shoot(entity, 1, point, NumericsVector3.Normalize(new NumericsVector3(0, -1, -10)));
				Toward(behavior, new Vector3(-3, 1, -10)).Should().BeApproximately(
					1,
					1e-4f,
					"from the entity, the camera's direction would pass beside the point");

				Shoot(entity, 3, point, NumericsVector3.Normalize(new NumericsVector3(-3, 0, -10)));
				Toward(behavior, new Vector3(-3, 0, -10)).Should().BeApproximately(
					1,
					1e-4f,
					"a ground aim turns the entity rather than tilting it");

				Shoot(entity, 5, point, new NumericsVector3(0, 0, -1));
				Toward(behavior, new Vector3(-3, 1, -10)).Should().BeApproximately(
					1,
					1e-4f,
					"a level camera is not a ground aim");
			}
			finally
			{
				body.Free();
			}
		});

		static float Toward(RecordingAimBehavior behavior, Vector3 expected)
		{
			return behavior.Received!.Value.Direction.Dot(expected.Normalized());
		}

		static void Shoot(ForgeEntity entity, long tick, NumericsVector3 point, NumericsVector3 direction)
		{
			entity.Input!.Submit(new InputFrame { Held = 0b1, Pressed = 0b1 }.WithAim(point, direction));
			EntitySimulation.Tick(entity, tick, Step);
			entity.Input.Submit(default);
			EntitySimulation.Tick(entity, tick + 1, Step);
		}
	}

	private static ForgeEntity CreatePlayer(out ForgeInputCollector collector, out Node3D body)
	{
		body = new Node3D();
		var entity = new ForgeEntity();
		collector = new ForgeInputCollector();
		entity.AddChild(collector);
		body.AddChild(entity);
		Root.AddChild(body);
		return entity;
	}

	private static GraphProcessor WatchFire(ForgeEntity entity)
	{
		var graph = new ForgeGraph();
		graph.VariableDefinitions.DefineObjectVariable<IForgeEntity>("source", entity);
		graph.VariableDefinitions.DefineVariable("yes", true);
		graph.VariableDefinitions.DefineVariable("pressed", false);

		var watch = new InputActionNode(Fire);
		watch.BindInput(ForgeInputActionNode.InputSourceInput, "source");
		var pressed = new SetVariableNode();
		pressed.BindInput(SetVariableNode.SourceInput, "yes");
		pressed.BindOutput(SetVariableNode.TargetOutput, "pressed");

		graph.AddNode(watch);
		graph.AddNode(pressed);
		graph.AddConnection(new Connection(
			graph.EntryNode.OutputPorts[EntryNode.OutputPort],
			watch.InputPorts[InputActionNode.InputPort]));
		graph.AddConnection(new Connection(
			watch.OutputPorts[ForgeInputActionNode.OnPressedPort],
			pressed.InputPorts[ActionNode.InputPort]));

		var processor = new GraphProcessor(graph);
		processor.StartGraph();
		return processor;
	}

	private static bool Pressed(GraphProcessor processor)
	{
		return processor.GraphContext.GraphVariables.TryGetVar("pressed", out bool value) && value;
	}

	private static void Press(ForgeInputCollector collector, string action)
	{
		Input.ActionPress(action);
		collector._UnhandledInput(new InputEventAction { Action = action, Pressed = true });
	}

	private static void Release(ForgeInputCollector collector, string action)
	{
		Input.ActionRelease(action);
		collector._UnhandledInput(new InputEventAction { Action = action, Pressed = false });
	}

	// The collector runs its physics step before the entity's, as its priority makes Godot do.
	private static void Tick(ForgeInputCollector collector, ForgeEntity entity)
	{
		collector._PhysicsProcess(Step);
		entity._PhysicsProcess(Step);
	}

	private static void WithProjectInput(Action test)
	{
		Variant actions = ProjectSettings.GetSetting(ForgeSettings.InputActionsSetting, Array.Empty<string>());
		Variant vectors = ProjectSettings.GetSetting(ForgeSettings.InputVectorsSetting, Array.Empty<string>());
		string[] inputActions = [Fire, Left, Right, Up, Down];

		foreach (string action in inputActions)
		{
			InputMap.AddAction(action);
		}

		ProjectSettings.SetSetting(ForgeSettings.InputActionsSetting, new[] { Fire });
		ProjectSettings.SetSetting(ForgeSettings.InputVectorsSetting, new[] { $"{Left}, {Right}, {Up}, {Down}" });

		try
		{
			test();
		}
		finally
		{
			ProjectSettings.SetSetting(ForgeSettings.InputActionsSetting, actions);
			ProjectSettings.SetSetting(ForgeSettings.InputVectorsSetting, vectors);

			foreach (string action in inputActions)
			{
				InputMap.EraseAction(action);
			}
		}
	}
}
