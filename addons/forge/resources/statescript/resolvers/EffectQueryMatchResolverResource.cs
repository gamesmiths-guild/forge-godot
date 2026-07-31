// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Effects;
using Gamesmiths.Forge.Godot.Core.Statescript.Resolvers;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;

using ForgeNode = Gamesmiths.Forge.Statescript.Node;

namespace Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;

/// <summary>
/// Resolver resource that matches a full effect query against the effect behind an <see cref="ActiveEffectHandle"/>
/// produced by a nested resolver.
/// </summary>
/// <remarks>
/// Use this when the filter needs more than tags — a specific effect data, or a modified attribute. For the common
/// tag-only case, <see cref="ActiveEffectTagQueryResolverResource"/> is cheaper to configure.
/// </remarks>
[Tool]
[GlobalClass]
public partial class EffectQueryMatchResolverResource : StatescriptResolverResource
{
	/// <inheritdoc/>
	public override string ResolverTypeId => "EffectQueryMatch";

	/// <summary>
	/// Gets or sets the query the effect must match. An unset query matches every effect.
	/// </summary>
	[Export]
	public ForgeEffectQuery? Query { get; set; }

	/// <summary>
	/// Gets or sets the nested resolver providing the active effect handle to inspect.
	/// </summary>
	[Export]
	public StatescriptResolverResource? ActiveEffect { get; set; }

	/// <inheritdoc/>
	public override void BindInput(Graph graph, ForgeNode runtimeNode, string nodeId, byte index)
	{
		DefineAndBindInputProperty(
			graph,
			runtimeNode,
			$"__effectquerymatch_{nodeId}_{index}",
			index,
			BuildResolver(graph));
	}

	/// <inheritdoc/>
	public override IPropertyResolver BuildResolver(Graph graph)
	{
		if (ActiveEffect is null
			|| !ActiveEffect.TryBuildObjectResolver(graph, out IObjectResolver? handleResolver)
			|| handleResolver is not IObjectResolver<ActiveEffectHandle> typedHandleResolver)
		{
			GD.PushError("Statescript: Effect Query Match resolver requires an Active Effect source.");
			return new VariantResolver(new Variant128(false), typeof(bool));
		}

		ForgeEffectQuery? queryResource = Query;

		// The query is materialized lazily so editor-time graph builds never touch the runtime managers through
		// GetEffectData() or the tags manager.
		return new LazyPropertyResolver(
			typeof(bool),
			() => new EffectQueryMatchResolver(
				typedHandleResolver,
				queryResource?.GetEffectQuery() ?? default));
	}
}
