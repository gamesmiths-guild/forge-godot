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

		ReportMissingNodeOnce();
		return default;
	}
#pragma warning restore SA1202 // Elements should be ordered by access

	private void ReportMissingNodeOnce()
	{
		if (_reportedMissingNode)
		{
			return;
		}

		_reportedMissingNode = true;

		GD.PushWarning(
			$"Statescript: {GetType().Name} found no Node3D for its entity" +
			(_nodePath.Length == 0 ? "." : $" at [{_nodePath}].") +
			" Resolving to a default value.");
	}
}
