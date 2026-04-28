// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;

using ForgeNode = Gamesmiths.Forge.Statescript.Node;

namespace Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers.Bases;

public abstract partial class UnaryNestedResolverResourceBase : StatescriptResolverResource
{
	protected abstract string PropertyNamePrefix { get; }

	protected abstract IPropertyResolver CreateResolver(IPropertyResolver operandResolver, Graph graph);

	[Export]
	public StatescriptResolverResource? Operand { get; set; }

	public override void BindInput(Graph graph, ForgeNode runtimeNode, string nodeId, byte index)
	{
		IPropertyResolver resolver = BuildResolver(graph);
		var propertyName = new StringKey($"{PropertyNamePrefix}_{nodeId}_{index}");
		graph.VariableDefinitions.DefineProperty(propertyName, resolver);
		runtimeNode.BindInput(index, propertyName);
	}

	public override IPropertyResolver BuildResolver(Graph graph)
	{
		IPropertyResolver operandResolver = Operand?.BuildResolver(graph) ?? CreateDefaultOperandResolver();
		return CreateResolver(operandResolver, graph);
	}

	protected virtual IPropertyResolver CreateDefaultOperandResolver()
	{
		return new VariantResolver(default, typeof(int));
	}
}
