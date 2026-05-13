// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;

using ForgeNode = Gamesmiths.Forge.Statescript.Node;

namespace Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;

[Tool]
[GlobalClass]
public partial class TagQueryResolverResource : StatescriptResolverResource
{
	public override string ResolverTypeId => "TagQuery";

	[Export]
	public ForgeQueryExpression? Query { get; set; }

	[Export]
	public TagQuerySource QuerySource { get; set; } = TagQuerySource.AllTags;

	[Export]
	public EntityResolverResourceBase? EntityResolver { get; set; }

	public override void BindInput(Graph graph, ForgeNode runtimeNode, string nodeId, byte index)
	{
		if (Query is null)
		{
			return;
		}

		var propertyName = new StringKey($"__tagquery_{nodeId}_{index}");
		graph.VariableDefinitions.DefineProperty(propertyName, BuildTagQueryResolver(graph));
		runtimeNode.BindInput(index, propertyName);
	}

	public override IPropertyResolver BuildResolver(Graph graph)
	{
		if (Query is null)
		{
			return new VariantResolver(new Variant128(false), typeof(bool));
		}

		return BuildTagQueryResolver(graph);
	}

	private TagQueryResolver BuildTagQueryResolver(Graph graph)
	{
		IEntityResolver entityResolver = EntityResolver?.BuildEntityResolver(graph) ?? new OwnerEntityResolver();
		return new TagQueryResolver(
			Query!.GetQueryExpression(),
			entityResolver,
			QuerySource);
	}
}
