// Copyright © Gamesmiths Guild.

using System;
using Gamesmiths.Forge.Godot.Core.Statescript.Physics;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;

namespace Gamesmiths.Forge.Godot.Core.Statescript.Resolvers;

/// <summary>
/// Base for every resolver that reads through the 3D camera a graph is looking through.
/// </summary>
/// <remarks>
/// <para>Which camera that is comes from the ability's owner, not from the main scene tree: the active camera of the
/// viewport the owner is standing in. A split-screen game runs one graph per player and each reads its own half, and a
/// world rendered inside a sub-viewport is aimed through the camera that renders it rather than through whatever the
/// main window holds.</para>
/// <para>These are deliberately not prefixed <c>Entity</c> and take no entity operand. A camera is not something an
/// entity has - it is how the graph's own player sees, which is a property of the ability being run.</para>
/// </remarks>
internal abstract class CameraResolverBase3D : IPropertyResolver
{
	private bool _reportedMissingCamera;

	/// <inheritdoc/>
	public abstract Type ValueType { get; }

	/// <summary>
	/// Reads this resolver's value through the resolved camera.
	/// </summary>
	/// <param name="contextNode">The owner's spatial node, for resolvers that also need where the graph is standing.
	/// </param>
	/// <param name="camera">The active camera of the owner's viewport.</param>
	/// <param name="graphContext">The running graph context, for subclasses with operands of their own to resolve.
	/// </param>
	/// <returns>The resolved value.</returns>
	protected abstract Variant128 ResolveFrom(Node3D contextNode, Camera3D camera, GraphContext graphContext);

#pragma warning disable SA1202 // Elements should be ordered by access
	/// <inheritdoc/>
	public Variant128 Resolve(GraphContext graphContext)
	{
		if (!PhysicsQuery3D.TryResolveContextNode(graphContext, out Node3D? contextNode))
		{
			ReportMissingCameraOnce("has no owner in the scene to read a viewport from");
			return default;
		}

		Camera3D? camera = contextNode.GetViewport()?.GetCamera3D();

		if (camera is null)
		{
			ReportMissingCameraOnce("found no active camera in its owner's viewport");
			return default;
		}

		return ResolveFrom(contextNode, camera, graphContext);
	}
#pragma warning restore SA1202 // Elements should be ordered by access

	// Resolvers run every tick, so a warning left unsuppressed would repeat every frame.
	private void ReportMissingCameraOnce(string message)
	{
		if (_reportedMissingCamera)
		{
			return;
		}

		_reportedMissingCamera = true;

		GD.PushWarning($"Statescript: {GetType().Name} {message}. Resolving to a default value.");
	}
}
