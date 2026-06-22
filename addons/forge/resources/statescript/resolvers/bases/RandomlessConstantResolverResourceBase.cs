// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;

using ForgeNode = Gamesmiths.Forge.Statescript.Node;

namespace Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers.Bases;

public abstract partial class RandomlessConstantResolverResourceBase : StatescriptResolverResource
{
	protected abstract string PropertyNamePrefix { get; }

	protected abstract IPropertyResolver CreateResolver();

#pragma warning disable SA1202 // Elements should be ordered by access
	public override void BindInput(Graph graph, ForgeNode runtimeNode, string nodeId, byte index)
#pragma warning restore SA1202 // Elements should be ordered by access
	{
		IPropertyResolver resolver = BuildResolver(graph);
		DefineAndBindInputProperty(graph, runtimeNode, $"{PropertyNamePrefix}_{nodeId}_{index}", index, resolver);
	}

	public override IPropertyResolver BuildResolver(Graph graph)
	{
		return CreateResolver();
	}
}
