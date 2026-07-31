// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Effects;
using Gamesmiths.Forge.Godot.Core.Statescript.Resolvers;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;

using ForgeNode = Gamesmiths.Forge.Statescript.Node;

namespace Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;

/// <summary>
/// Resolver resource that queries the handles of the active effects on an entity, filtered by an effect query. Feed
/// the result into a Remove Effect node for dispel patterns, or into array operations.
/// </summary>
/// <remarks>
/// <para>The query is materialized lazily on first resolve, so editor-time graph builds never touch the runtime
/// managers.</para>
/// <para>To select the applications of one specific effect, set the query's Effect Definition.</para>
/// </remarks>
[Tool]
[GlobalClass]
public partial class QueryActiveEffectsResolverResource : StatescriptResolverResource
{
	/// <inheritdoc/>
	public override string ResolverTypeId => "QueryActiveEffects";

	/// <summary>
	/// Gets or sets the effect query to filter by. When unset, every active effect is returned.
	/// </summary>
	[Export]
	public ForgeEffectQuery? Query { get; set; }

	/// <summary>
	/// Gets or sets which entity should be inspected. Defaults to owner when omitted.
	/// </summary>
	[Export]
	public EntityResolverResourceBase? EntityResolver { get; set; }

	/// <inheritdoc/>
	public override void BindInput(Graph graph, ForgeNode runtimeNode, string nodeId, byte index)
	{
		if (!TryBuildArrayResolver(graph, out _, out IObjectArrayResolver? objectArrayResolver)
			|| objectArrayResolver is null)
		{
			return;
		}

		var propertyName = new StringKey($"__queryactiveeffects_{nodeId}_{index}");
		graph.VariableDefinitions.DefineObjectArrayProperty(propertyName, objectArrayResolver);
		runtimeNode.BindInput(index, propertyName);
	}

	/// <inheritdoc/>
	public override bool TryBuildArrayResolver(
		Graph graph,
		out IArrayPropertyResolver? valueArrayResolver,
		out IObjectArrayResolver? objectArrayResolver)
	{
		valueArrayResolver = null;

		ForgeEffectQuery? queryResource = Query;
		IEntityResolver entityResolver = EntityResolver?.BuildEntityResolver(graph) ?? new AbilityOwnerResolver();

		objectArrayResolver = new LazyObjectArrayResolver<ActiveEffectHandle>(
			() => new QueryActiveEffectsResolver(queryResource?.GetEffectQuery() ?? default, entityResolver));

		return true;
	}
}
