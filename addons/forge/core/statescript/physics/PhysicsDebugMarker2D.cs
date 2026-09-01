// Copyright © Gamesmiths Guild.

using System.Collections.Generic;
using Godot;

namespace Gamesmiths.Forge.Godot.Core.Statescript.Physics;

/// <summary>
/// The canvas item <see cref="PhysicsDebugDraw2D"/> draws one query into.
/// </summary>
/// <remarks>
/// <para>2D has no counterpart to a mesh instance holding geometry: everything a canvas item shows is drawn from its
/// own <see cref="CanvasItem._Draw"/>, so the marker has to be a script rather than a stock node. It holds whichever of
/// the two kinds of geometry it was last given - a shape sitting somewhere, or a run of line segments - and redraws it
/// when asked.</para>
/// <para>Shapes are drawn through <see cref="Shape2D.Draw"/>, the same call Godot makes for a collision shape in the
/// scene, which means the marker itself carries the shape's transform. Segments are already world points, so the marker
/// stays at the origin for those.</para>
/// </remarks>
internal sealed partial class PhysicsDebugMarker2D : Node2D
{
	private const float LineWidth = 2.0f;

	// Consecutive pairs, each one segment. Kept and refilled rather than reallocated, because a held marker redraws
	// every tick for as long as its node is active.
	private readonly List<Vector2> _points = [];

	private Shape2D? _shape;

	/// <summary>
	/// Gets or sets the colour everything this marker draws is drawn in.
	/// </summary>
	public Color Color { get; set; } = Colors.White;

	/// <summary>
	/// Draws a shape sitting somewhere in the world, replacing whatever the marker held.
	/// </summary>
	/// <param name="shape">The shape to draw.</param>
	/// <param name="transform">Where the shape sits and how it is turned.</param>
	public void SetShape(Shape2D shape, Transform2D transform)
	{
		_shape = shape;
		_points.Clear();
		GlobalTransform = transform;
		QueueRedraw();
	}

	/// <summary>
	/// Starts a new run of segments, discarding whatever the marker held.
	/// </summary>
	public void BeginSegments()
	{
		_shape = null;
		_points.Clear();

		// Segment endpoints are world points, so the marker itself carries no transform of its own.
		GlobalTransform = Transform2D.Identity;
	}

	/// <summary>
	/// Adds one segment to the current run.
	/// </summary>
	/// <param name="from">Where the segment starts.</param>
	/// <param name="to">Where the segment ends.</param>
	public void AddSegment(Vector2 from, Vector2 to)
	{
		_points.Add(from);
		_points.Add(to);
	}

	/// <summary>
	/// Redraws the marker with the segments added since <see cref="BeginSegments"/>.
	/// </summary>
	public void EndSegments()
	{
		QueueRedraw();
	}

	/// <inheritdoc/>
	public override void _Draw()
	{
		base._Draw();

		if (_shape is not null && IsInstanceValid(_shape))
		{
			_shape.Draw(GetCanvasItem(), Color);
			return;
		}

		for (int i = 0; i + 1 < _points.Count; i += 2)
		{
			DrawLine(_points[i], _points[i + 1], Color, LineWidth);
		}
	}
}
