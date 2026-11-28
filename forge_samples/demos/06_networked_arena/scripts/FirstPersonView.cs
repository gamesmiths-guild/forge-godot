// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Godot.Nodes;
using Godot;
using Godot.Collections;

namespace Gamesmiths.Forge.Example;

/// <summary>
/// The eyes of this machine's player: the mouse turns the camera, the camera follows the player's shown body, and the
/// player's own model hides behind its first-person weapon. The input collector aims from this camera, so the player
/// shoots and walks where it looks.
/// </summary>
/// <remarks>
/// It's presentation only, the same on every machine: the camera and the mouse touch nothing the server simulates.
/// On a machine whose player this isn't, it stays off.
/// </remarks>
public partial class FirstPersonView : Node3D
{
	private float _yaw;
	private float _pitch;
	private float _recoil;

	/// <summary>
	/// Gets or sets the player's entity.
	/// </summary>
	[Export]
	public required ForgeEntity Entity { get; set; }

	/// <summary>
	/// Gets or sets the camera, a child of this node.
	/// </summary>
	[Export]
	public required Camera3D Camera { get; set; }

	/// <summary>
	/// Gets or sets what the eyes follow: the movement's visual, which eases out a correction.
	/// </summary>
	[Export]
	public required Node3D Follow { get; set; }

	/// <summary>
	/// Gets or sets how high the eyes are above the body's origin.
	/// </summary>
	[Export]
	public float EyeHeight { get; set; } = 1.6f;

	/// <summary>
	/// Gets or sets how far the camera turns for a pixel of mouse motion, in radians.
	/// </summary>
	[Export]
	public float Sensitivity { get; set; } = 0.0025f;

	/// <summary>
	/// Gets or sets how fast a kick's pitch settles back, as the share of it left each second.
	/// </summary>
	[Export(PropertyHint.Range, "0,1,0.01")]
	public float RecoilLeftPerSecond { get; set; } = 0.002f;

	/// <summary>
	/// Gets or sets what this machine's player doesn't see of itself.
	/// </summary>
	[Export]
	public Array<Node3D> HiddenInFirstPerson { get; set; } = [];

	/// <summary>
	/// Gets or sets what only this machine's player sees: its weapon in hand.
	/// </summary>
	[Export]
	public Node3D? ViewModel { get; set; }

	/// <inheritdoc/>
	public override void _Ready()
	{
		base._Ready();

		TopLevel = true;
		Activate(false);

		if (ForgeNetworkSession.Of(this) is ForgeNetworkSession session)
		{
			session.LocalPlayerChanged += OnLocalPlayerChanged;
			Activate(session.LocalPlayer == Entity);
		}
	}

	/// <inheritdoc/>
	public override void _ExitTree()
	{
		base._ExitTree();

		if (ForgeNetworkSession.Of(this) is ForgeNetworkSession session)
		{
			session.LocalPlayerChanged -= OnLocalPlayerChanged;
		}
	}

	/// <summary>
	/// Kicks the view up, from where it settles back.
	/// </summary>
	/// <param name="degrees">How far up.</param>
	public void Kick(float degrees)
	{
		if (IsProcessing())
		{
			_recoil += Mathf.DegToRad(degrees);
		}
	}

	/// <inheritdoc/>
	public override void _UnhandledInput(InputEvent @event)
	{
		base._UnhandledInput(@event);

		if (Input.MouseMode == Input.MouseModeEnum.Captured && @event is InputEventMouseMotion motion)
		{
			_yaw -= motion.ScreenRelative.X * Sensitivity;
			_pitch = Mathf.Clamp(_pitch - (motion.ScreenRelative.Y * Sensitivity), -1.5f, 1.5f);
		}
	}

	/// <inheritdoc/>
	public override void _Process(double delta)
	{
		base._Process(delta);

		_recoil *= Mathf.Pow(RecoilLeftPerSecond, (float)delta);
		GlobalTransform = new Transform3D(
			Basis.FromEuler(new Vector3(Mathf.Clamp(_pitch + _recoil, -1.5f, 1.5f), _yaw, 0)),
			Follow.GetGlobalTransformInterpolated().Origin + (Vector3.Up * EyeHeight));
	}

	private void OnLocalPlayerChanged(Node? player)
	{
		Activate(player == Entity);
	}

	private void Activate(bool active)
	{
		SetProcess(active);
		SetProcessUnhandledInput(active);
		Camera.Current = active;

		if (ViewModel is not null)
		{
			ViewModel.Visible = active;
		}

		foreach (Node3D hidden in HiddenInFirstPerson)
		{
			hidden.Visible = !active;
		}

		if (active)
		{
			_yaw = Follow.GlobalRotation.Y;
			_pitch = 0;
			_recoil = 0;
			_Process(0);
		}
	}
}
