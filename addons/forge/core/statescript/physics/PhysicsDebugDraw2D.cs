// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Statescript;
using Godot;
using Node = Godot.Node;

namespace Gamesmiths.Forge.Godot.Core.Statescript.Physics;

/// <summary>
/// Draws the 2D physics queries a graph runs, so a query with no scene node behind it can be seen.
/// </summary>
/// <remarks>
/// <para>An overlap shape, a ray and an impulse all describe geometry that exists only for the instant it is asked
/// about. Godot already draws the collision shapes that live in the scene; this draws the ones that do not, where they
/// were asked about, through the same <see cref="Shape2D.Draw"/> call Godot uses for its own.</para>
/// <para>It is gated entirely on <b>Debug &gt; Visible Collision Shapes</b>, the same switch that reveals the scene's
/// own shapes, so there is nothing to author and nothing to remember to turn off. With that switch off every entry
/// point here returns on a single flag read and allocates nothing.</para>
/// <para>One-shot queries flash for a moment and vanish. A query a State node keeps asking gets a marker the node owns
/// for its lifetime and releases when it deactivates, so what is on screen is exactly what is being watched.</para>
/// </remarks>
internal static class PhysicsDebugDraw2D
{
	// Distinct from the 3D container's name on purpose: both dimensions can be alive in one viewport, and a container
	// found by name would otherwise be the wrong kind of node for whichever of them created it second.
	private const string ContainerName = "ForgeStatescriptPhysicsDebug2D";

	private const float FlashSeconds = 0.35f;

	// The head is a fraction of the shaft so a short arrow stays readable, capped so a long one does not grow a head
	// the size of the screen. Lengths are in pixels, which is what a 2D world measures distance in.
	private const float ArrowHeadFraction = 0.18f;
	private const float ArrowHeadMaxLength = 24.0f;

	// How many segments the wedge's arc is drawn with. Fewer than the 3D cone's ring, because the arc spans the
	// aperture rather than a full turn.
	private const int ConeArcSegments = 12;

	/// <summary>
	/// Gets a value indicating whether the running game was started with Visible Collision Shapes on.
	/// </summary>
	public static bool IsEnabled => Engine.GetMainLoop() is SceneTree { DebugCollisionsHint: true };

	/// <summary>
	/// Gets the colour of an overlap query that found nothing.
	/// </summary>
	public static Color OverlapEmptyColor { get; } = new(0.25f, 0.85f, 1.0f, 0.2f);

	/// <summary>
	/// Gets the colour of an overlap query that found something.
	/// </summary>
	public static Color OverlapFoundColor { get; } = new(1.0f, 0.55f, 0.2f, 0.35f);

	/// <summary>
	/// Gets the colour of a ray that hit something.
	/// </summary>
	public static Color RayHitColor { get; } = new(1.0f, 0.45f, 0.2f);

	/// <summary>
	/// Gets the colour of a ray that reached its full length without hitting anything.
	/// </summary>
	public static Color RayClearColor { get; } = new(1.0f, 0.85f, 0.3f);

	/// <summary>
	/// Gets the colour of an unobstructed line of sight.
	/// </summary>
	public static Color SightClearColor { get; } = new(0.35f, 1.0f, 0.45f);

	/// <summary>
	/// Gets the colour of a line of sight something is standing in.
	/// </summary>
	public static Color SightBlockedColor { get; } = new(1.0f, 0.3f, 0.35f);

	/// <summary>
	/// Gets the colour of a velocity or impulse arrow.
	/// </summary>
	public static Color ForceColor { get; } = new(1.0f, 0.4f, 0.95f);

	/// <summary>
	/// Gets the marker a State node holds for as long as it is watching something, creating it on the first call and
	/// recolouring it on later ones.
	/// </summary>
	/// <remarks>
	/// Callers keep the returned marker in their node context, pass it back each tick, and hand it to
	/// <see cref="Release"/> when they deactivate.
	/// </remarks>
	/// <param name="graphContext">The graph execution context, used to find the viewport to draw in.</param>
	/// <param name="existing">The marker from the previous tick, when there was one.</param>
	/// <param name="color">The colour the marker should currently be.</param>
	/// <returns>The marker, or <see langword="null"/> when debug drawing is off or the graph has no owner in the
	/// scene.</returns>
	public static PhysicsDebugMarker2D? EnsureMarker(
		GraphContext graphContext,
		PhysicsDebugMarker2D? existing,
		Color color)
	{
		if (!IsEnabled)
		{
			Release(existing);
			return null;
		}

		if (existing is not null && GodotObject.IsInstanceValid(existing))
		{
			existing.Color = color;
			return existing;
		}

		return CreateMarker(graphContext, color);
	}

	/// <summary>
	/// Releases a marker a State node was holding.
	/// </summary>
	/// <param name="marker">The marker, which may be <see langword="null"/> or already freed.</param>
	public static void Release(PhysicsDebugMarker2D? marker)
	{
		if (marker is not null && GodotObject.IsInstanceValid(marker))
		{
			marker.QueueFree();
		}
	}

	/// <summary>
	/// Points a marker at a shape sitting somewhere in the world.
	/// </summary>
	/// <param name="marker">The marker, which may be <see langword="null"/>.</param>
	/// <param name="shape">The shape to draw. Its own draw call is used, the same one Godot makes for a collision
	/// shape in the scene.</param>
	/// <param name="transform">Where the shape sits and how it is turned.</param>
	public static void SetShape(PhysicsDebugMarker2D? marker, Shape2D shape, Transform2D transform)
	{
		if (marker is null || !GodotObject.IsInstanceValid(marker))
		{
			return;
		}

		marker.SetShape(shape, transform);
	}

	/// <summary>
	/// Points a marker at a swept shape: its outline where it came to rest, and the line its centre travelled along.
	/// </summary>
	/// <remarks>
	/// <para>The outline alone says where the sweep <em>stopped</em> and nothing about where it came from, which for a
	/// cast that missed leaves a shape floating with no visible relationship to the caster. The line is what Godot's
	/// own <see cref="ShapeCast2D"/> gizmo draws for the same reason, and it is drawn to the sweep's full reach rather
	/// than to the resting point — on a hit, the gap between the end of the line and the shape is exactly the distance
	/// the sweep was stopped short by.</para>
	/// <para>Both are one colour, because they are one answer.</para>
	/// </remarks>
	/// <param name="marker">The marker, which may be <see langword="null"/>.</param>
	/// <param name="shape">The shape that was swept.</param>
	/// <param name="transform">Where the shape came to rest and how it is turned.</param>
	/// <param name="from">Where the sweep started.</param>
	/// <param name="to">Where the sweep would have reached had nothing stopped it.</param>
	public static void SetShapecast(
		PhysicsDebugMarker2D? marker,
		Shape2D shape,
		Transform2D transform,
		Vector2 from,
		Vector2 to)
	{
		if (marker is null || !GodotObject.IsInstanceValid(marker))
		{
			return;
		}

		marker.SetShapecast(shape, transform, from, to);
	}

	/// <summary>
	/// Points a marker at a line between two world points.
	/// </summary>
	/// <param name="marker">The marker, which may be <see langword="null"/>.</param>
	/// <param name="from">Where the line starts.</param>
	/// <param name="to">Where the line ends.</param>
	public static void SetLine(PhysicsDebugMarker2D? marker, Vector2 from, Vector2 to)
	{
		if (marker is null || !GodotObject.IsInstanceValid(marker))
		{
			return;
		}

		marker.BeginSegments();
		marker.AddSegment(from, to);
		marker.EndSegments();
	}

	/// <summary>
	/// Points a marker at an arrow drawn from a world point along a vector.
	/// </summary>
	/// <remarks>
	/// The arrow is the vector at its true world length, so a velocity arrow reaches where the body would be one second
	/// from now, and an impulse that is far too strong looks it.
	/// </remarks>
	/// <param name="marker">The marker, which may be <see langword="null"/>.</param>
	/// <param name="origin">Where the arrow starts.</param>
	/// <param name="vector">The direction and length to draw.</param>
	public static void SetArrow(PhysicsDebugMarker2D? marker, Vector2 origin, Vector2 vector)
	{
		if (marker is null || !GodotObject.IsInstanceValid(marker))
		{
			return;
		}

		// Cleared before the length check, matching the 3D marker: an arrow that resolved to nothing must not leave the
		// previous frame's arrow on screen.
		marker.BeginSegments();

		float length = vector.Length();

		if (length <= 0.0001f)
		{
			marker.EndSegments();
			return;
		}

		Vector2 direction = vector / length;
		Vector2 tip = origin + vector;
		float headLength = Mathf.Min(length * ArrowHeadFraction, ArrowHeadMaxLength);

		// 2D has one perpendicular rather than a whole plane of them, so the head needs no axis picked for it.
		Vector2 side = direction.Orthogonal();
		Vector2 headBase = tip - (direction * headLength);

		marker.AddSegment(origin, tip);
		marker.AddSegment(tip, headBase + (side * headLength * 0.5f));
		marker.AddSegment(tip, headBase - (side * headLength * 0.5f));
		marker.EndSegments();
	}

	/// <summary>
	/// Points a marker at a wedge opening from a world point.
	/// </summary>
	/// <remarks>
	/// The 2D reading of the 3D cone, and a genuinely simpler shape: a plane's cone is a wedge, so two edges and the
	/// arc between them are the whole outline rather than the least geometry that suggests a volume.
	/// </remarks>
	/// <param name="marker">The marker, which may be <see langword="null"/>.</param>
	/// <param name="origin">The wedge's apex.</param>
	/// <param name="direction">Which way it opens. Normalized here.</param>
	/// <param name="range">How far it reaches.</param>
	/// <param name="halfAngle">Half the wedge's aperture, in radians.</param>
	public static void SetCone(
		PhysicsDebugMarker2D? marker,
		Vector2 origin,
		Vector2 direction,
		float range,
		float halfAngle)
	{
		if (marker is null || !GodotObject.IsInstanceValid(marker))
		{
			return;
		}

		// Cleared before the validity check, matching the arrow: a wedge that resolved to nothing must not leave the
		// previous frame's wedge on screen.
		marker.BeginSegments();

		if (direction.LengthSquared() <= 0.000001f || range <= 0)
		{
			marker.EndSegments();
			return;
		}

		float axis = direction.Angle();
		Vector2 previous = origin + (Vector2.FromAngle(axis - halfAngle) * range);

		marker.AddSegment(origin, previous);

		for (int i = 1; i <= ConeArcSegments; i++)
		{
			float angle = axis - halfAngle + (2 * halfAngle * i / ConeArcSegments);
			Vector2 point = origin + (Vector2.FromAngle(angle) * range);

			marker.AddSegment(previous, point);
			previous = point;
		}

		marker.AddSegment(previous, origin);
		marker.EndSegments();
	}

	/// <summary>
	/// Draws a shape where a one-shot query asked about it, for a moment.
	/// </summary>
	/// <param name="graphContext">The graph execution context, used to find the viewport to draw in.</param>
	/// <param name="shape">The shape that was queried.</param>
	/// <param name="transform">Where it was queried and how it was turned.</param>
	/// <param name="color">The colour to draw it in.</param>
	public static void FlashShape(GraphContext graphContext, Shape2D shape, Transform2D transform, Color color)
	{
		if (!IsEnabled)
		{
			return;
		}

		PhysicsDebugMarker2D? marker = CreateMarker(graphContext, color);
		SetShape(marker, shape, transform);
		Flash(marker);
	}

	/// <summary>
	/// Draws a swept shape where a one-shot cast ran, for a moment.
	/// </summary>
	/// <param name="graphContext">The graph execution context, used to find the viewport to draw in.</param>
	/// <param name="shape">The shape that was swept.</param>
	/// <param name="transform">Where the shape came to rest and how it is turned.</param>
	/// <param name="from">Where the sweep started.</param>
	/// <param name="to">Where the sweep would have reached had nothing stopped it.</param>
	/// <param name="color">The colour to draw it in.</param>
	public static void FlashShapecast(
		GraphContext graphContext,
		Shape2D shape,
		Transform2D transform,
		Vector2 from,
		Vector2 to,
		Color color)
	{
		if (!IsEnabled)
		{
			return;
		}

		PhysicsDebugMarker2D? marker = CreateMarker(graphContext, color);
		SetShapecast(marker, shape, transform, from, to);
		Flash(marker);
	}

	/// <summary>
	/// Draws a line where a one-shot query asked about it, for a moment.
	/// </summary>
	/// <param name="graphContext">The graph execution context, used to find the viewport to draw in.</param>
	/// <param name="from">Where the line starts.</param>
	/// <param name="to">Where the line ends.</param>
	/// <param name="color">The colour to draw it in.</param>
	public static void FlashLine(GraphContext graphContext, Vector2 from, Vector2 to, Color color)
	{
		if (!IsEnabled)
		{
			return;
		}

		PhysicsDebugMarker2D? marker = CreateMarker(graphContext, color);
		SetLine(marker, from, to);
		Flash(marker);
	}

	/// <summary>
	/// Draws an arrow for a force a node just applied, for a moment.
	/// </summary>
	/// <param name="graphContext">The graph execution context, used to find the viewport to draw in.</param>
	/// <param name="origin">Where the arrow starts.</param>
	/// <param name="vector">The direction and length to draw.</param>
	/// <param name="color">The colour to draw it in.</param>
	public static void FlashArrow(GraphContext graphContext, Vector2 origin, Vector2 vector, Color color)
	{
		if (!IsEnabled)
		{
			return;
		}

		PhysicsDebugMarker2D? marker = CreateMarker(graphContext, color);
		SetArrow(marker, origin, vector);
		Flash(marker);
	}

	/// <summary>
	/// Draws a wedge where a one-shot query asked about it, for a moment.
	/// </summary>
	/// <param name="graphContext">The graph execution context, used to find the viewport to draw in.</param>
	/// <param name="origin">The wedge's apex.</param>
	/// <param name="direction">Which way it opens.</param>
	/// <param name="range">How far it reaches.</param>
	/// <param name="halfAngle">Half the wedge's aperture, in radians.</param>
	/// <param name="color">The colour to draw it in.</param>
	public static void FlashCone(
		GraphContext graphContext,
		Vector2 origin,
		Vector2 direction,
		float range,
		float halfAngle,
		Color color)
	{
		if (!IsEnabled)
		{
			return;
		}

		PhysicsDebugMarker2D? marker = CreateMarker(graphContext, color);
		SetCone(marker, origin, direction, range, halfAngle);
		Flash(marker);
	}

	private static PhysicsDebugMarker2D? CreateMarker(GraphContext graphContext, Color color)
	{
		if (!PhysicsQuery2D.TryResolveContextNode(graphContext, out Node2D? context)
			|| !context.IsInsideTree())
		{
			return null;
		}

		// Parented to the owner's own viewport rather than to the main scene, so a game rendering its world inside a
		// sub-viewport draws its debug geometry in that world instead of an empty one.
		Viewport? viewport = context.GetViewport();

		if (viewport is null)
		{
			return null;
		}

		var marker = new PhysicsDebugMarker2D { Color = color };

		ResolveContainer(viewport).AddChild(marker);
		return marker;
	}

	private static void Flash(PhysicsDebugMarker2D? marker)
	{
		if (marker is null)
		{
			return;
		}

		if (Engine.GetMainLoop() is not SceneTree tree)
		{
			Release(marker);
			return;
		}

		tree.CreateTimer(FlashSeconds).Timeout += () => Release(marker);
	}

	private static Node ResolveContainer(Viewport viewport)
	{
		Node? container = viewport.GetNodeOrNull(ContainerName);

		if (container is not null)
		{
			return container;
		}

		// Drawn above the game rather than into it, which is what NoDepthTest buys the 3D markers.
		container = new Node2D
		{
			Name = ContainerName,
			ZIndex = (int)RenderingServer.CanvasItemZMax,
			ZAsRelative = false,
		};

		viewport.AddChild(container);
		return container;
	}
}
