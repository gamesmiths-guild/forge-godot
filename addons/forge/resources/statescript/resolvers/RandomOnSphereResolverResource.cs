// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Godot.Core;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;

using ForgeNode = Gamesmiths.Forge.Statescript.Node;

namespace Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;

[Tool]
[GlobalClass]
public partial class RandomOnSphereResolverResource : StatescriptResolverResource
{
	public override string ResolverTypeId => "RandomOnSphere";

	public override void BindInput(Graph graph, ForgeNode runtimeNode, string nodeId, byte index)
	{
		var propertyName = new StringKey($"__randsphereon_{nodeId}_{index}");
		graph.VariableDefinitions.DefineProperty(propertyName, BuildResolver(graph));
		runtimeNode.BindInput(index, propertyName);
	}

	public override IPropertyResolver BuildResolver(Graph graph)
	{
		using var random = new ForgeRandom();
		return new RandomOnSphereResolver(random);
	}
}
