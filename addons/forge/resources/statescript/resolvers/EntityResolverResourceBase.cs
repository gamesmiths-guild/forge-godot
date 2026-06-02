// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;
using ForgeNode = Gamesmiths.Forge.Statescript.Node;

namespace Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;

[Tool]
public abstract partial class EntityResolverResourceBase : StatescriptResolverResource
{
	public abstract IEntityResolver BuildEntityResolver(Graph graph);

	public override void BindInput(Graph graph, ForgeNode runtimeNode, string nodeId, byte index)
	{
		var propertyKey = new StringKey($"__entity_{nodeId}_{index}");
		graph.VariableDefinitions.DefineObjectProperty(propertyKey, BuildEntityResolver(graph));
		runtimeNode.BindInput(index, propertyKey);
	}
}
