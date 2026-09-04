// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Cues;
using Godot;

namespace Gamesmiths.Forge.Godot.Nodes;

/// <summary>
/// Cue handler that shakes the camera the player is looking through.
/// </summary>
/// <remarks>
/// <para>A screen effect rather than a target effect, which is why it is a cue handler and not a graph node: nothing
/// about it belongs to the entity that was hit, and a graph that could shake the screen would be a graph that has to
/// know which screen. It is also why it takes no target - it reads the cue's phase and ignores who the cue was applied
/// to.</para>
/// <para>The shake is written to the camera's <em>offset</em>, not its transform. A game's camera is nearly always
/// being driven by something else - a follow rig, a spring arm, a cutscene - and moving the transform would fight that
/// every frame; the offset shifts what the camera sees while leaving where it is alone. One handler serves 2D and 3D,
/// reading whichever camera the viewport has, so <see cref="Amplitude"/> is in that camera's own units: world units in
/// 3D, pixels in 2D.</para>
/// <para>Executing shakes for <see cref="Duration"/> and decays to nothing; applying shakes at full strength until the
/// cue is removed, for the length of a channel or a stun. The camera and its untouched offset are captured when a
/// shake starts and put back when it ends, so a camera swapped mid-shake - a cut to another view - is not left holding
/// somebody else's offset.</para>
/// <para>Time here is wall clock rather than frame delta on purpose. A shake and a hit stop are the pair everyone
/// reaches for together, and a shake driven by scaled time would crawl for exactly as long as the hit stop it is
/// supposed to punctuate.</para>
/// </remarks>
[GlobalClass]
public partial class CameraShakeCueHandler : ForgeCueHandler
{
	private const double MicrosecondsPerSecond = 1000000.0;

	private Camera3D? _camera3D;
	private Camera2D? _camera2D;
	private Vector2 _restingOffset;
	private ulong _startedAt;
	private float _magnitudeScale = 1.0f;
	private bool _sustained;
	private int _holds;

	/// <summary>
	/// Gets or sets how far the view is thrown at full strength, in the camera's own units - world units for a 3D
	/// camera, pixels for a 2D one.
	/// </summary>
	[Export]
	public float Amplitude { get; set; } = 0.1f;

	/// <summary>
	/// Gets or sets how long an executed shake lasts, in seconds. Ignored while a cue is applied, which shakes until
	/// it is removed.
	/// </summary>
	[Export]
	public float Duration { get; set; } = 0.25f;

	/// <summary>
	/// Gets or sets the curve scaling the amplitude by the cue's normalized magnitude. Unset shakes every hit the
	/// same, which for a cue that fires on damage is the one thing a shake should not do.
	/// </summary>
	[Export]
	public Curve? MagnitudeCurve { get; set; }

	/// <inheritdoc/>
	public override void _Ready()
	{
		base._Ready();
		SetProcess(false);
	}

	/// <inheritdoc/>
	public override void _ExitTree()
	{
		// Put the camera back before going away, whatever is still holding it. A handler freed mid-shake - a scene
		// change, a level reload - would otherwise leave the offset it wrote on a camera that outlives it.
		_holds = 0;
		Restore();
		base._ExitTree();
	}

	/// <inheritdoc/>
	public override void _Process(double delta)
	{
		base._Process(delta);

		if (!TryGetCamera(out Camera3D? camera3D, out Camera2D? camera2D))
		{
			Restore();
			return;
		}

		float strength = Amplitude * _magnitudeScale;

		if (!_sustained)
		{
			double elapsed = (Time.GetTicksUsec() - _startedAt) / MicrosecondsPerSecond;

			if (Duration <= 0 || elapsed >= Duration)
			{
				Restore();
				return;
			}

			strength *= 1.0f - (float)(elapsed / Duration);
		}

		var offset = new Vector2(
			(float)GD.RandRange(-strength, strength),
			(float)GD.RandRange(-strength, strength));

		Apply(camera3D, camera2D, _restingOffset + offset);
	}

	/// <inheritdoc/>
	public override void _CueOnExecute(CueParameters? parameters)
	{
		// A hold outranks a one-shot. One handler serves every target of its cue, so an execute landing while someone
		// is still holding the shake must refresh how hard it shakes without also handing it a timer - ending on that
		// timer would cut a shake its holder still expects to be running.
		Begin(parameters, sustained: _holds > 0);
	}

	/// <inheritdoc/>
	public override void _CueOnApply(CueParameters? parameters)
	{
		_holds++;
		Begin(parameters, sustained: true);
	}

	/// <inheritdoc/>
	public override void _CueOnRemove(bool interrupted)
	{
		// Counted rather than switched off, because one handler is shared by every target: two stunned characters are
		// two holds, and the first stun to expire must not steal the shake from the second.
		_holds = Mathf.Max(_holds - 1, 0);

		if (_holds == 0)
		{
			Restore();
		}
	}

	private static void Apply(Camera3D? camera3D, Camera2D? camera2D, Vector2 offset)
	{
		if (camera3D is not null)
		{
			camera3D.HOffset = offset.X;
			camera3D.VOffset = offset.Y;
			return;
		}

		if (camera2D is not null)
		{
			camera2D.Offset = offset;
		}
	}

	private bool TryGetCamera(out Camera3D? camera3D, out Camera2D? camera2D)
	{
		camera3D = _camera3D is not null && IsInstanceValid(_camera3D) ? _camera3D : null;
		camera2D = _camera2D is not null && IsInstanceValid(_camera2D) ? _camera2D : null;

		return camera3D is not null || camera2D is not null;
	}

	// A shake already running is restarted rather than layered: two shakes writing one offset is one shake at whatever
	// amplitude wrote last, so the second execute is the shake, which is also what a repeated hit should look like.
	private void Begin(CueParameters? parameters, bool sustained)
	{
		if (!TryGetCamera(out _, out _) && !TryCaptureCamera())
		{
			return;
		}

		_magnitudeScale = MagnitudeCurve is null
			? 1.0f
			: Mathf.Max(MagnitudeCurve.Sample(parameters?.NormalizedMagnitude ?? 0), 0.0f);

		_sustained = sustained;
		_startedAt = Time.GetTicksUsec();
		SetProcess(true);
	}

	private bool TryCaptureCamera()
	{
		Viewport? viewport = GetViewport();

		if (viewport is null)
		{
			WarnOnce("is not in a viewport, so there is no camera to shake.");
			return false;
		}

		_camera3D = viewport.GetCamera3D();
		_camera2D = _camera3D is null ? viewport.GetCamera2D() : null;

		if (_camera3D is null && _camera2D is null)
		{
			WarnOnce("found no active camera in its viewport. Nothing was shaken.");
			return false;
		}

		_restingOffset = _camera3D is not null
			? new Vector2(_camera3D.HOffset, _camera3D.VOffset)
			: _camera2D!.Offset;

		return true;
	}

	private void Restore()
	{
		SetProcess(false);

		if (TryGetCamera(out Camera3D? camera3D, out Camera2D? camera2D))
		{
			Apply(camera3D, camera2D, _restingOffset);
		}

		_camera3D = null;
		_camera2D = null;
	}
}
