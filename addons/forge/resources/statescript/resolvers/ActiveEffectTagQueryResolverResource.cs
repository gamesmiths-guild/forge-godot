// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Effects;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Gamesmiths.Forge.Tags;
using Godot;

using ForgeNode = Gamesmiths.Forge.Statescript.Node;

namespace Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;

/// <summary>
/// Resolver resource that evaluates a tag query against the tags of the effect behind an
/// <see cref="ActiveEffectHandle"/> produced by a nested resolver.
/// </summary>
/// <remarks>
/// Pair this with a Where operation over an active-effect array to filter effects by category — the dispel pattern.
/// </remarks>
[Tool]
[GlobalClass]
public partial class ActiveEffectTagQueryResolverResource : StatescriptResolverResource
{
	/// <inheritdoc/>
	public override string ResolverTypeId => "ActiveEffectTagQuery";

	/// <summary>
	/// Gets or sets the query to evaluate against the selected tags.
	/// </summary>
	[Export]
	public ForgeQueryExpression? Query { get; set; }

	/// <summary>
	/// Gets or sets which set of the effect's tags to evaluate against.
	/// </summary>
	[Export]
	public EffectTagSource TagSource { get; set; } = EffectTagSource.OwningTags;

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
			$"__activeeffecttagquery_{nodeId}_{index}",
			index,
			BuildResolver(graph));
	}

	/// <inheritdoc/>
	public override IPropertyResolver BuildResolver(Graph graph)
	{
		if (Query is null)
		{
			GD.PushError("Statescript: Active Effect Tag Query resolver requires a query.");
			return new VariantResolver(new Variant128(false), typeof(bool));
		}

		if (ActiveEffect is null
			|| !ActiveEffect.TryBuildObjectResolver(graph, out IObjectResolver? handleResolver)
			|| handleResolver is not IObjectResolver<ActiveEffectHandle> typedHandleResolver)
		{
			GD.PushError("Statescript: Active Effect Tag Query resolver requires an Active Effect source.");
			return new VariantResolver(new Variant128(false), typeof(bool));
		}

		var query = new TagQuery();
		query.Build(Query.GetQueryExpression());

		return new ActiveEffectTagQueryResolver(typedHandleResolver, query, TagSource);
	}
}
