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
	private readonly string _nodePath = nodePath;

	private bool _reportedMissingNode;

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

#pragma warning disable SA1202 // Elements should be ordered by access
	/// <inheritdoc/>
	public Variant128 Resolve(GraphContext graphContext)
	{
		IForgeEntity? entity = _entityResolver.Resolve(graphContext);

		if (ForgeEntityBridge.TryGetSpatialNode3D(entity, _nodePath, out Node3D? spatialNode))
		{
			return ResolveFrom(spatialNode, graphContext);
		}

		if (entity is null)
		{
			WarnOnce(
				"resolved no entity to read from. Check the entity operand, which is often an empty variable." +
				" Resolving to a default value.");

			return default;
		}

		// A marker path is an offset from the entity, not a different subject, so an entity that happens not to carry
		// the marker still has a right answer: its own node. One graph runs against every kind of entity and only some
		// of them are authored with a %CastPoint, so reading the body for the rest keeps the query on the entity the
		// author picked, where the default value silently aimed it at the world origin instead.
		if (_nodePath.Length > 0 && ForgeEntityBridge.TryGetSpatialNode3D(entity, out spatialNode))
		{
			WarnOnce($"found no Node3D at [{_nodePath}]. Reading the entity's own node instead.");
			return ResolveFrom(spatialNode, graphContext);
		}

		WarnOnce("found no Node3D for its entity. Resolving to a default value.");
		return default;
	}
#pragma warning restore SA1202 // Elements should be ordered by access

	// Resolvers run every tick, so a warning left unsuppressed would repeat every frame.
	private void WarnOnce(string message)
	{
		if (_reportedMissingNode)
		{
			return;
		}

		_reportedMissingNode = true;

		GD.PushWarning($"Statescript: {GetType().Name} {message}");
	}
}
