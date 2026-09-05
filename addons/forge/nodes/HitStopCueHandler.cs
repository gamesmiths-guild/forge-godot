// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Cues;
using Godot;

namespace Gamesmiths.Forge.Godot.Nodes;

/// <summary>
/// Cue handler that slows the whole game for a moment, so a hit lands with weight.
/// </summary>
/// <remarks>
/// <para>A screen effect rather than a target effect, the same as the camera shake, and the two are usually the same
/// cue: the stop is what makes the hit read as heavy and the shake is what makes it read as loud.</para>
/// <para>It writes <see cref="Engine.TimeScale"/>, which is global. That is what a hit stop <em>is</em> - the whole
/// world pausing, not one character - but it means two of these fighting over the same moment is one handler's stop,
/// so put one in the scene and let cue tags decide when it fires.</para>
/// <para>The countdown is wall clock, not frame delta, and it has to be: at a time scale of a twentieth, a duration
/// measured in scaled time would last twenty times as long as it says, and every hit stop in the game would read as a
/// freeze.</para>
/// <para>The scale in force when a stop begins is captured and restored, rather than restoring to one. A game already
/// running in slow motion - a bullet-time ability, a pause ramp - gets its own scale back instead of being snapped to
/// full speed by a hit. A stop already running is restarted rather than nested, so the captured scale is always the
/// game's own and never another stop's.</para>
/// </remarks>
[GlobalClass]
public partial class HitStopCueHandler : ForgeCueHandler
{
	private const double MicrosecondsPerSecond = 1000000.0;

	private double _restingTimeScale = 1.0;
	private double _duration;
	private ulong _startedAt;
	private bool _stopping;

	/// <summary>
	/// Gets or sets how slowly the game runs during the stop, as a fraction of its normal speed. Zero freezes it
	/// outright.
	/// </summary>
	[Export]
	public float TimeScale { get; set; } = 0.05f;

	/// <summary>
	/// Gets or sets how long the stop lasts, in seconds of real time.
	/// </summary>
	[Export]
	public float Duration { get; set; } = 0.08f;

	/// <summary>
	/// Gets or sets the curve scaling the duration by the cue's normalized magnitude. Unset stops every hit for the
	/// same moment, where a heavier hit should hang for longer.
	/// </summary>
	[Export]
	public Curve? MagnitudeCurve { get; set; }

	/// <inheritdoc/>
	public override void _Ready()
	{
		base._Ready();
		SetProcess(false);

		// The stop has to keep counting while the game it stopped is not running, which is the one thing an ordinary
		// process mode does not do.
		ProcessMode = ProcessModeEnum.Always;
	}

	/// <inheritdoc/>
	public override void _ExitTree()
	{
		// Put the clock back before going away. A handler freed mid-stop - a scene change, a level reload - would
		// otherwise leave the whole game running in slow motion with nothing left to undo it.
		Restore();
		base._ExitTree();
	}

	/// <inheritdoc/>
	public override void _Process(double delta)
	{
		base._Process(delta);

		if ((Time.GetTicksUsec() - _startedAt) / MicrosecondsPerSecond >= _duration)
		{
			Restore();
		}
	}

	/// <inheritdoc/>
	public override void _CueOnExecute(CueParameters? parameters)
	{
		double duration = Duration;

		if (MagnitudeCurve is not null)
		{
			duration *= Mathf.Max(MagnitudeCurve.Sample(parameters?.NormalizedMagnitude ?? 0), 0.0f);
		}

		if (duration <= 0)
		{
			return;
		}

		// Captured only when no stop is running, so a second hit landing inside the first extends that stop rather
		// than recording its slowed clock as the speed to go back to.
		if (!_stopping)
		{
			_restingTimeScale = Engine.TimeScale;
			_stopping = true;
		}

		_duration = duration;
		_startedAt = Time.GetTicksUsec();
		Engine.TimeScale = Mathf.Max(TimeScale, 0.0f);
		SetProcess(true);
	}

	private void Restore()
	{
		SetProcess(false);

		if (!_stopping)
		{
			return;
		}

		_stopping = false;
		Engine.TimeScale = _restingTimeScale;
	}
}
