// Copyright © Gamesmiths Guild.

using System.Collections.Generic;
using Gamesmiths.Forge.Statescript;
using Godot;
using Node = Godot.Node;

namespace Gamesmiths.Forge.Godot.Core.Statescript.Physics;

/// <summary>
/// Draws the physics queries a graph runs, so a query with no scene node behind it can be seen.
/// </summary>
/// <remarks>
/// <para>An overlap shape, a ray and an impulse all describe geometry that exists only for the instant it is asked
/// about. Godot already draws the collision shapes that live in the scene; this draws the ones that do not, where they
/// were asked about, using the same wireframes Godot uses for its own.</para>
/// <para>It is gated entirely on <b>Debug &gt; Visible Collision Shapes</b>, the same switch that reveals the scene's
/// own shapes, so there is nothing to author and nothing to remember to turn off. With that switch off every entry
/// point here returns on a single flag read and allocates nothing.</para>
/// <para>One-shot queries flash for a moment and vanish. A query a State node keeps asking gets a marker the node owns
/// for its lifetime and releases when it deactivates, so what is on screen is exactly what is being watched.</para>
/// </remarks>
internal static class PhysicsDebugDraw3D
{
	private const string ContainerName = "ForgeStatescriptPhysicsDebug";

	private const float FlashSeconds = 0.35f;

	// The head is a fraction of the shaft so a short arrow stays readable, capped so a long one does not grow a head
	// the size of a room.
	private const float ArrowHeadFraction = 0.18f;
	private const float ArrowHeadMaxLength = 0.6f;

	private static readonly Dictionary<Color, StandardMaterial3D> _materials = [];

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
	public static MeshInstance3D? EnsureMarker(GraphContext graphContext, MeshInstance3D? existing, Color color)
	{
		if (!IsEnabled)
		{
			Release(existing);
			return null;
		}

		if (existing is not null && GodotObject.IsInstanceValid(existing))
		{
			existing.MaterialOverride = ResolveMaterial(color);
			return existing;
		}

		return CreateMarker(graphContext, color);
	}

	/// <summary>
	/// Releases a marker a State node was holding.
	/// </summary>
	/// <param name="marker">The marker, which may be <see langword="null"/> or already freed.</param>
	public static void Release(MeshInstance3D? marker)
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
	/// <param name="shape">The shape to draw. Its own debug mesh is used, the same one Godot draws for a collision
	/// shape in the scene.</param>
	/// <param name="transform">Where the shape sits and how it is turned.</param>
	public static void SetShape(MeshInstance3D? marker, Shape3D shape, Transform3D transform)
	{
		if (marker is null || !GodotObject.IsInstanceValid(marker))
		{
			return;
		}

		marker.Mesh = shape.GetDebugMesh();
		marker.GlobalTransform = transform;
	}

	/// <summary>
	/// Points a marker at a line between two world points.
	/// </summary>
	/// <param name="marker">The marker, which may be <see langword="null"/>.</param>
	/// <param name="from">Where the line starts.</param>
	/// <param name="to">Where the line ends.</param>
	public static void SetLine(MeshInstance3D? marker, Vector3 from, Vector3 to)
	{
		ImmediateMesh? mesh = PrepareLineMesh(marker);

		if (mesh is null)
		{
			return;
		}

		mesh.SurfaceBegin(Mesh.PrimitiveType.Lines);
		mesh.SurfaceAddVertex(from);
		mesh.SurfaceAddVertex(to);
		mesh.SurfaceEnd();
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
	public static void SetArrow(MeshInstance3D? marker, Vector3 origin, Vector3 vector)
	{
		ImmediateMesh? mesh = PrepareLineMesh(marker);

		if (mesh is null)
		{
			return;
		}

		float length = vector.Length();

		if (length <= 0.0001f)
		{
			return;
		}

		Vector3 direction = vector / length;
		Vector3 tip = origin + vector;
		float headLength = Mathf.Min(length * ArrowHeadFraction, ArrowHeadMaxLength);

		// Any axis not parallel to the arrow works for spreading the head; up is only unusable for a vertical arrow.
		Vector3 side = Mathf.Abs(direction.Dot(Vector3.Up)) > 0.99f
			? direction.Cross(Vector3.Right).Normalized()
			: direction.Cross(Vector3.Up).Normalized();

		Vector3 headBase = tip - (direction * headLength);

		mesh.SurfaceBegin(Mesh.PrimitiveType.Lines);
		mesh.SurfaceAddVertex(origin);
		mesh.SurfaceAddVertex(tip);
		mesh.SurfaceAddVertex(tip);
		mesh.SurfaceAddVertex(headBase + (side * headLength * 0.5f));
		mesh.SurfaceAddVertex(tip);
		mesh.SurfaceAddVertex(headBase - (side * headLength * 0.5f));
		mesh.SurfaceEnd();
	}

	/// <summary>
	/// Draws a shape where a one-shot query asked about it, for a moment.
	/// </summary>
	/// <param name="graphContext">The graph execution context, used to find the viewport to draw in.</param>
	/// <param name="shape">The shape that was queried.</param>
	/// <param name="transform">Where it was queried and how it was turned.</param>
	/// <param name="color">The colour to draw it in.</param>
	public static void FlashShape(GraphContext graphContext, Shape3D shape, Transform3D transform, Color color)
	{
		if (!IsEnabled)
		{
			return;
		}

		MeshInstance3D? marker = CreateMarker(graphContext, color);
		SetShape(marker, shape, transform);
		Flash(marker);
	}

	/// <summary>
	/// Draws a line where a one-shot query asked about it, for a moment.
	/// </summary>
	/// <param name="graphContext">The graph execution context, used to find the viewport to draw in.</param>
	/// <param name="from">Where the line starts.</param>
	/// <param name="to">Where the line ends.</param>
	/// <param name="color">The colour to draw it in.</param>
	public static void FlashLine(GraphContext graphContext, Vector3 from, Vector3 to, Color color)
	{
		if (!IsEnabled)
		{
			return;
		}

		MeshInstance3D? marker = CreateMarker(graphContext, color);
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
	public static void FlashArrow(GraphContext graphContext, Vector3 origin, Vector3 vector, Color color)
	{
		if (!IsEnabled)
		{
			return;
		}

		MeshInstance3D? marker = CreateMarker(graphContext, color);
		SetArrow(marker, origin, vector);
		Flash(marker);
	}

	private static MeshInstance3D? CreateMarker(GraphContext graphContext, Color color)
	{
		if (!PhysicsQuery3D.TryResolveContextNode(graphContext, out Node3D? context)
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

		var marker = new MeshInstance3D
		{
			MaterialOverride = ResolveMaterial(color),
			CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
		};

		ResolveContainer(viewport).AddChild(marker);
		return marker;
	}

	private static void Flash(MeshInstance3D? marker)
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

	private static ImmediateMesh? PrepareLineMesh(MeshInstance3D? marker)
	{
		if (marker is null || !GodotObject.IsInstanceValid(marker))
		{
			return null;
		}

		// Held markers redraw every tick, so the mesh is kept and its surfaces replaced rather than a new one built.
		// Line vertices are already world points, so the marker itself stays at the origin.
		if (marker.Mesh is not ImmediateMesh mesh)
		{
			mesh = new ImmediateMesh();
			marker.Mesh = mesh;
			marker.GlobalTransform = Transform3D.Identity;
		}

		mesh.ClearSurfaces();
		return mesh;
	}

	private static Node ResolveContainer(Viewport viewport)
	{
		Node? container = viewport.GetNodeOrNull(ContainerName);

		if (container is not null)
		{
			return container;
		}

		container = new Node3D { Name = ContainerName };
		viewport.AddChild(container);
		return container;
	}

	private static StandardMaterial3D ResolveMaterial(Color color)
	{
		if (_materials.TryGetValue(color, out StandardMaterial3D? material)
			&& GodotObject.IsInstanceValid(material))
		{
			return material;
		}

		material = new StandardMaterial3D
		{
			ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
			Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
			CullMode = BaseMaterial3D.CullModeEnum.Disabled,
			NoDepthTest = true,
			AlbedoColor = color,
		};

		_materials[color] = material;
		return material;
	}
}
