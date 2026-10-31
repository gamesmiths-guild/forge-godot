// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Godot.Nodes;
using Godot;

namespace Gamesmiths.Forge.Example;

/// <summary>
/// Pauses the game on the rewind action and shows the world history as a timeline. Dragging the needle shows any tick
/// held, without simulating, and resuming continues the game from the tick shown. A corner readout shows what the
/// history costs: the snapshots it holds, about how much memory they take, and how long a tick takes to capture.
/// </summary>
public partial class RewindTimeline : CanvasLayer
{
	private bool _showing;

	[Export]
	public required ForgeWorldHistory WorldHistory { get; set; }

	[Export]
	public required Control Panel { get; set; }

	[Export]
	public required HSlider Timeline { get; set; }

	[Export]
	public required Label TimeLabel { get; set; }

	[Export]
	public required Label CostLabel { get; set; }

	public override void _Ready()
	{
		base._Ready();

		ProcessMode = ProcessModeEnum.Always;
		Panel.Visible = false;
		Timeline.ValueChanged += OnTimelineChanged;
	}

	public override void _ExitTree()
	{
		base._ExitTree();

		// Leaving for the hub while paused would otherwise leave the hub paused too.
		GetTree().Paused = false;
	}

	public override void _Input(InputEvent @event)
	{
		base._Input(@event);

		if (!@event.IsActionPressed("rewind"))
		{
			return;
		}

		GetViewport().SetInputAsHandled();

		// The world history ticks the whole world in one physics step, so the pause always falls between two ticks.
		SceneTree tree = GetTree();
		tree.Paused = !tree.Paused;
		Panel.Visible = tree.Paused;

		if (tree.Paused)
		{
			ShowTimeline();
		}
		else
		{
			Timeline.ReleaseFocus();
		}
	}

	public override void _Process(double delta)
	{
		base._Process(delta);

		WorldHistory history = WorldHistory.History;

		if (history.OldestTick is not long oldest || history.NewestTick is not long newest)
		{
			return;
		}

		double megabytes = WorldHistory.LastRecordBytes * (newest - oldest + 1) / (1024.0 * 1024.0);
		CostLabel.Text =
			$"{history.SnapshotCount} snapshots held, about {megabytes:0.0} MB, " +
			$"{WorldHistory.LastRecordTime.TotalMilliseconds:0.000} ms to capture a tick";

		if (Panel.Visible)
		{
			double secondsBack = (double)(newest - history.CurrentTick!.Value) / Engine.PhysicsTicksPerSecond;
			TimeLabel.Text = $"{secondsBack:0.00} s back";
		}
	}

	private void ShowTimeline()
	{
		WorldHistory history = WorldHistory.History;

		if (history.OldestTick is not long oldest || history.NewestTick is not long newest)
		{
			return;
		}

		_showing = true;
		Timeline.MinValue = oldest;
		Timeline.MaxValue = newest;
		Timeline.SetValueNoSignal(history.CurrentTick!.Value);
		_showing = false;

		// Focused, the arrow keys step the needle a tick at a time.
		Timeline.GrabFocus();
	}

	private void OnTimelineChanged(double value)
	{
		if (!_showing && GetTree().Paused)
		{
			WorldHistory.Seek((long)value);
		}
	}
}
