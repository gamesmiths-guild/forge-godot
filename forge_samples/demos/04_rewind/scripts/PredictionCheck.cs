// Copyright © Gamesmiths Guild.

using System.Collections.Generic;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Effects;
using Gamesmiths.Forge.Godot.Nodes;
using Godot;

namespace Gamesmiths.Forge.Example;

/// <summary>
/// While the prediction check action has it on, replays the player the way a client the server corrects will: every
/// 12 ticks, 200 ms at 60 Hz, the player alone goes back 12 ticks and runs them again within one physics step, against
/// the world as it is now, and each tick is checked against the recording. The readout counts the ticks that came out
/// differently and shows how the last one did.
/// </summary>
/// <remarks>
/// <para>Godot moves a character body put in place only on its next physics step, which is why the world history
/// replays one tick per step. A predicting client puts back only its own body and runs every tick since in one step;
/// this checks that its ticks still come out the same.</para>
/// <para>The rest of the world isn't replayed: it's where it is now, and it doesn't act on the player. A tick in which
/// the fire reached the player comes out differently, as it would on a client until the server's state for it arrives,
/// and so can one in which the player touched a body that has moved since. Effects the replayed player applies to
/// others are refused, as a predicting client's are, and a stretch in which the player shot is skipped, since its
/// projectile would spawn again.</para>
/// <para>A body pressed against a wall or another body can also come out a thousandth of a unit off now and then, in a
/// world replay as much as here: Godot keeps a body's contacts inside the physics engine, where a snapshot can't reach
/// them.</para>
/// </remarks>
public partial class PredictionCheck : Label
{
	private const int Depth = 12;

	private static readonly RefuseAll _refuseAll = new();

	private WorldHistory? _player;
	private long _recorded;
	private long _lastShot = long.MinValue;
	private int _checks;
	private int _desynced;
	private int _skipped;
	private string _last = string.Empty;

	[Export]
	public required ForgeWorldHistory WorldHistory { get; set; }

	[Export]
	public required ForgeEntity Player { get; set; }

	public override void _Ready()
	{
		base._Ready();

		Visible = false;
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		base._UnhandledInput(@event);

		if (!@event.IsActionPressed("prediction_check"))
		{
			return;
		}

		GetViewport().SetInputAsHandled();
		Visible = !Visible;
		_player = null;
		_checks = 0;
		_desynced = 0;
		_skipped = 0;
		_last = string.Empty;
		Text = "Prediction check: waiting for 12 ticks";
	}

	public override void _PhysicsProcess(double delta)
	{
		base._PhysicsProcess(delta);

		// The timeline keeps this processing while paused, when the world shows a tick held rather than a live one.
		if (!Visible
			|| GetTree().Paused
			|| WorldHistory.Replaying
			|| WorldHistory.History.CurrentTick is not long tick
			|| tick == _recorded)
		{
			return;
		}

		if (_player is null)
		{
			_player = new WorldHistory(Depth + 1, WorldHistory.History.FixedDeltaTime);
			_player.Add(Player);
		}

		// A tick that doesn't follow the last starts the player's history over, and a shot from the discarded future
		// with it.
		if (tick != _recorded + 1)
		{
			_lastShot = long.MinValue;
		}

		_recorded = tick;
		_player.Record(tick);

		if (Player.Input?.WasPressed("skill_1") == true)
		{
			_lastShot = tick;
		}

		if (tick % Depth != 0 || _player.OldestTick > tick - Depth)
		{
			return;
		}

		if (_lastShot > tick - Depth)
		{
			_skipped++;
		}
		else
		{
			Check(tick);
		}

		Text = $"Prediction check: {_checks} replays of {Depth} ticks in one step, {_desynced} ticks came out " +
			$"differently, {_skipped} skipped for a shot{_last}";
	}

	private static string NameOf(IForgeEntity entity)
	{
		return entity is Node node && IsInstanceValid(node) && node.GetParent() is Node body
			? body.Name
			: "a released entity";
	}

	private void Check(long tick)
	{
		List<IForgeEntity> others = [];

		foreach (IForgeEntity entity in WorldHistory.History.Entities)
		{
			if (entity != Player)
			{
				entity.EffectsManager.RegisterApplicationBlocker(_refuseAll);
				others.Add(entity);
			}
		}

		try
		{
			_player!.Rollback(tick - Depth);
			IReadOnlyList<WorldDesync> desyncs = _player.Replay(tick);
			_checks++;
			_desynced += desyncs.Count;

			if (desyncs.Count > 0)
			{
				_last = $"\nLast, {tick - desyncs[^1].Tick} ticks back:\n{desyncs[^1].Describe(NameOf)}";
			}
		}
		finally
		{
			foreach (IForgeEntity entity in others)
			{
				entity.EffectsManager.UnregisterApplicationBlocker(_refuseAll);
			}
		}
	}

	private sealed class RefuseAll : IEffectApplicationBlocker
	{
		public bool AllowEffectApplication(in Effect effect)
		{
			return false;
		}
	}
}
