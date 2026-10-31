// Copyright © Gamesmiths Guild.

using System.Collections.Generic;
using System.Linq;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Godot.Nodes;
using Godot;

namespace Gamesmiths.Forge.Example;

/// <summary>
/// Pauses the game on the rewind action and shows the world history as a timeline. Dragging the needle shows any tick
/// held, without simulating, and resuming continues the game from the tick shown. The replay action plays the ticks
/// after the needle again from the recording and marks the ones that came out differently in red; with the needle on a
/// mark, the panel lists what differed. A corner readout shows what the history costs: the snapshots it holds, about
/// how much memory they take, and how long a tick takes to capture.
/// </summary>
public partial class RewindTimeline : CanvasLayer
{
	private readonly SortedDictionary<long, WorldDesync> _desyncs = [];
	private bool _showing;
	private bool _replayed;

	[Export]
	public required ForgeWorldHistory WorldHistory { get; set; }

	[Export]
	public required Control Panel { get; set; }

	[Export]
	public required HSlider Timeline { get; set; }

	[Export]
	public required Control Marks { get; set; }

	[Export]
	public required Label TimeLabel { get; set; }

	[Export]
	public required Label StatusLabel { get; set; }

	[Export]
	public required Label DiffLabel { get; set; }

	[Export]
	public required Label CostLabel { get; set; }

	public override void _Ready()
	{
		base._Ready();

		ProcessMode = ProcessModeEnum.Always;
		Panel.Visible = false;
		Timeline.ValueChanged += OnTimelineChanged;
		Marks.Draw += DrawMarks;
		WorldHistory.Desynced += OnDesynced;
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

		bool rewind = @event.IsActionPressed("rewind");
		bool replay = @event.IsActionPressed("replay");

		if (!rewind && !replay)
		{
			return;
		}

		GetViewport().SetInputAsHandled();
		SceneTree tree = GetTree();

		// Either stops a replay where it is.
		if (WorldHistory.Replaying)
		{
			StopReplay();
			return;
		}

		if (replay)
		{
			if (tree.Paused)
			{
				StartReplay();
			}

			return;
		}

		// The world history ticks the whole world in one physics step, so the pause always falls between two ticks.
		tree.Paused = !tree.Paused;
		Panel.Visible = tree.Paused;

		if (tree.Paused)
		{
			ShowTimeline();
		}
		else
		{
			Timeline.ReleaseFocus();

			// Resuming leaves the ticks after the needle behind, and what a replay found in them with them.
			foreach (long tick in _desyncs.Keys.Where(x => x > WorldHistory.History.CurrentTick).ToList())
			{
				_desyncs.Remove(tick);
			}
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

		if (!Panel.Visible)
		{
			return;
		}

		long current = history.CurrentTick!.Value;

		if (WorldHistory.Replaying)
		{
			_showing = true;
			Timeline.SetValueNoSignal(current);
			_showing = false;

			// The replay caught up with the present.
			if (current == newest)
			{
				StopReplay();
			}
		}

		double secondsBack = (double)(newest - current) / Engine.PhysicsTicksPerSecond;
		TimeLabel.Text = WorldHistory.Replaying
			? $"Replaying, {secondsBack:0.00} s back"
			: $"{secondsBack:0.00} s back";
		StatusLabel.Text = Status(oldest);

		WorldDesync? desync = WorldHistory.Replaying ? null : _desyncs.GetValueOrDefault(current);
		DiffLabel.Visible = desync is not null;

		if (desync is not null)
		{
			DiffLabel.Text = desync.Describe(NameOf);
		}

		Marks.QueueRedraw();
	}

	private static string NameOf(IForgeEntity entity)
	{
		// The entity lives on its body; one only the replay had is released, its body freed.
		return entity is Node node && IsInstanceValid(node) && node.GetParent() is Node body
			? body.Name
			: "a released entity";
	}

	private void StartReplay()
	{
		WorldHistory history = WorldHistory.History;

		if (history.CurrentTick is not long current || current >= history.NewestTick)
		{
			return;
		}

		// The ticks after the needle are checked anew.
		foreach (long tick in _desyncs.Keys.Where(x => x > current).ToList())
		{
			_desyncs.Remove(tick);
		}

		_replayed = true;
		WorldHistory.Replaying = true;
		Timeline.Editable = false;
		Timeline.ReleaseFocus();
		GetTree().Paused = false;
	}

	private void StopReplay()
	{
		WorldHistory.Replaying = false;
		Timeline.Editable = true;
		GetTree().Paused = true;
		ShowTimeline();
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

	private string Status(long oldest)
	{
		int count = _desyncs.Keys.Count(x => x >= oldest);

		if (count > 0)
		{
			return $"{count} ticks ran differently from the recording, marked red. Put the needle on one to see how.";
		}

		if (WorldHistory.Replaying)
		{
			return "Every tick so far ran the way it was recorded.";
		}

		return _replayed
			? "The replay ran every tick the way it was recorded."
			: "R plays the ticks after the needle again and checks each against the recording.";
	}

	private void OnDesynced(WorldDesync desync)
	{
		_desyncs[desync.Tick] = desync;
	}

	private void DrawMarks()
	{
		WorldHistory history = WorldHistory.History;

		if (history.OldestTick is not long oldest || history.NewestTick is not long newest || newest <= oldest)
		{
			return;
		}

		foreach (long tick in _desyncs.Keys.Where(x => x >= oldest && x <= newest))
		{
			float x = (float)((tick - oldest) / (double)(newest - oldest)) * Marks.Size.X;
			Marks.DrawLine(new Vector2(x, 0), new Vector2(x, Marks.Size.Y), Colors.Red, 2);
		}
	}

	private void OnTimelineChanged(double value)
	{
		if (!_showing && GetTree().Paused)
		{
			WorldHistory.Seek((long)value);
		}
	}
}
