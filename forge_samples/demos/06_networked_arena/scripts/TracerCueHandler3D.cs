// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Cues;
using Gamesmiths.Forge.Godot.Nodes;
using Godot;

namespace Gamesmiths.Forge.Example;

/// <summary>
/// Draws a shot's tracer: a line from the shooter's muzzle to the cue's <see cref="CueParameters.Position"/>, which
/// fades out. The point travels with the cue, so every screen draws the tracer to where the server's shot stopped.
/// </summary>
/// <remarks>
/// The muzzle is the shooter's <c>%ViewMuzzle</c> while it shows, as this machine's first-person weapon does, and its
/// <c>%Muzzle</c> otherwise.
/// </remarks>
public partial class TracerCueHandler3D : ForgeCueHandler
{
	/// <summary>
	/// Gets or sets the tracer's color.
	/// </summary>
	[Export]
	public Color Color { get; set; } = new(0.6f, 0.4f, 1f);

	/// <summary>
	/// Gets or sets the tracer's width.
	/// </summary>
	[Export]
	public float Width { get; set; } = 0.06f;

	/// <summary>
	/// Gets or sets how long the tracer takes to fade out, in seconds.
	/// </summary>
	[Export]
	public float Duration { get; set; } = 0.3f;

	/// <inheritdoc/>
	public override void _CueOnExecute(IForgeEntity forgeEntity, CueParameters? parameters)
	{
		if (parameters?.Position is not System.Numerics.Vector3 point
			|| forgeEntity is not Node node
			|| !node.IsInsideTree())
		{
			return;
		}

		Node shooter = node.GetParent();
		Node3D? muzzle = shooter.GetNodeOrNull<Node3D>("%ViewMuzzle") is { } view && view.IsVisibleInTree()
			? view
			: shooter.GetNodeOrNull<Node3D>("%Muzzle");

		if (muzzle is null)
		{
			return;
		}

		Vector3 from = muzzle.GlobalPosition;
		var to = new Vector3(point.X, point.Y, point.Z);
		float length = from.DistanceTo(to);

		if (length < 0.01f)
		{
			return;
		}

		var material = new StandardMaterial3D
		{
			ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
			Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
			AlbedoColor = Color,
			CullMode = BaseMaterial3D.CullModeEnum.Disabled,
		};

		var tracer = new MeshInstance3D
		{
			Mesh = new CylinderMesh { TopRadius = Width / 2, BottomRadius = Width / 2, Height = 1, RadialSegments = 6 },
			MaterialOverride = material,
			CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
		};

		GetTree().CurrentScene.AddChild(tracer);

		Vector3 along = (to - from) / length;
		Vector3 side = Mathf.Abs(along.Dot(Vector3.Up)) < 0.99f ? Vector3.Up : Vector3.Right;
		Vector3 x = side.Cross(along).Normalized();
		tracer.GlobalTransform = new Transform3D(
			new Basis(x, along * length, x.Cross(along)),
			(from + to) / 2);

		Tween fade = tracer.CreateTween();
		fade.TweenProperty(material, "albedo_color:a", 0f, Duration);
		fade.TweenCallback(Callable.From(tracer.QueueFree));
	}
}
