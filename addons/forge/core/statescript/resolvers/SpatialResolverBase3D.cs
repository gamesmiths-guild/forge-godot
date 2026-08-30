// Copyright © Gamesmiths Guild.

using System;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;

namespace Gamesmiths.Forge.Godot.Core.Statescript.Resolvers;

/// <summary>
/// Base for every resolver that reads something off the 3D node an entity lives on.
/// </summary>
/// <remarks>
/// <para>Handles the three things all of them share: resolving the entity operand, getting from that entity to a
/// <see cref="Node3D"/> through <see cref="ForgeEntityBridge"/>, and honoring an authored child path so a marker node
/// such as <c>%CastPoint</c> can stand in for the body itself.</para>
/// <para>Subclasses implement <see cref="ResolveFrom"/> and never deal with entities or paths.</para>
/// </remarks>
/// <param name="entityResolver">Resolves which entity to read.</param>
/// <param name="nodePath">Optional path to a descendant node to read instead of the entity's own spatial node.</param>
internal abstract class SpatialResolverBase3D(IEntityResolver entityResolver, string nodePath) : IPropertyResolver
{
	private readonly IEntityResolver _entityResolver = entityResolver;

	private bool _reportedMissingNode;
	private bool _reportedUnusableNode;

	/// <inheritdoc/>
	public abstract Type ValueType { get; }

	/// <summary>
	/// Reads this resolver's value off the resolved node.
	/// </summary>
	/// <param name="spatialNode">The entity's spatial node, or the authored descendant of it.</param>
	/// <param name="graphContext">The running graph context, for subclasses with operands of their own to resolve.
	/// </param>
	/// <returns>The resolved value.</returns>
	protected abstract Variant128 ResolveFrom(Node3D spatialNode, GraphContext graphContext);

	/// <summary>
	/// Gets the authored path to a descendant node, or an empty string when the entity's own node is read.
	/// </summary>
	/// <remarks>
	/// Subclasses read this to tell an entity that simply has no velocity, rotation or scale of its own - where a
	/// default is the honest answer - from one the author explicitly aimed a path at, where the same default is a
	/// number that looks real and is not.
	/// </remarks>
	protected string NodePath { get; } = nodePath;

#pragma warning disable SA1202 // Elements should be ordered by access
	/// <inheritdoc/>
	public Variant128 Resolve(GraphContext graphContext)
	{
		IForgeEntity? entity = _entityResolver.Resolve(graphContext);

		if (ForgeEntityBridge.TryGetSpatialNode3D(entity, NodePath, out Node3D? spatialNode))
		{
			return ResolveFrom(spatialNode, graphContext);
		}

		if (entity is null)
		{
			ReportMissingNodeOnce(
				"resolved no entity to read from. Check the entity operand, which is often an empty variable." +
				" Resolving to a default value.");

			return default;
		}

		// A marker path is an offset from the entity, not a different subject, so an entity that happens not to carry
		// the marker still has a right answer: its own node. One graph runs against every kind of entity and only some
		// of them are authored with a %CastPoint, so reading the body for the rest keeps the query on the entity the
		// author picked, where the default value silently aimed it at the world origin instead.
		if (NodePath.Length > 0 && ForgeEntityBridge.TryGetSpatialNode3D(entity, out spatialNode))
		{
			ReportMissingNodeOnce($"found no Node3D at [{NodePath}]. Reading the entity's own node instead.");
			return ResolveFrom(spatialNode, graphContext);
		}

		ReportMissingNodeOnce("found no Node3D for its entity. Resolving to a default value.");
		return default;
	}
#pragma warning restore SA1202 // Elements should be ordered by access

	/// <summary>
	/// Warns once that the resolved node has nothing for this resolver to read, such as a marker where a body is
	/// required.
	/// </summary>
	/// <remarks>
	/// Suppressed separately from the missing-node warning, matching the spatial action nodes: an entity without the
	/// marker falls back to its own node and can then still fail the subclass's type check, and one warning silencing
	/// the other would leave that second failure invisible.
	/// </remarks>
	/// <param name="message">What is wrong with the node, completing "Statescript: {resolver type} ".</param>
	protected void ReportUnusableNodeOnce(string message)
	{
		if (_reportedUnusableNode)
		{
			return;
		}

		_reportedUnusableNode = true;

		GD.PushWarning($"Statescript: {GetType().Name} {message}");
	}

	// Resolvers run every tick, so a warning left unsuppressed would repeat every frame.
	private void ReportMissingNodeOnce(string message)
	{
		if (_reportedMissingNode)
		{
			return;
		}

		_reportedMissingNode = true;

		GD.PushWarning($"Statescript: {GetType().Name} {message}");
	}
}
