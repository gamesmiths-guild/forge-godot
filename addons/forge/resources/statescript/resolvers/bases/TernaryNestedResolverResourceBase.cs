// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;

using ForgeNode = Gamesmiths.Forge.Statescript.Node;

namespace Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers.Bases;

public abstract partial class TernaryNestedResolverResourceBase : StatescriptResolverResource
{
	protected abstract string PropertyNamePrefix { get; }

	protected abstract IPropertyResolver CreateResolver(
		IPropertyResolver firstResolver,
		IPropertyResolver secondResolver,
		IPropertyResolver thirdResolver,
		Graph graph);

	[Export]
	public StatescriptResolverResource? First { get; set; }

	[Export]
	public StatescriptResolverResource? Second { get; set; }

	[Export]
	public StatescriptResolverResource? Third { get; set; }

	public override void BindInput(Graph graph, ForgeNode runtimeNode, string nodeId, byte index)
	{
		IPropertyResolver resolver = BuildResolver(graph);
		var propertyName = new StringKey($"{PropertyNamePrefix}_{nodeId}_{index}");
		graph.VariableDefinitions.DefineProperty(propertyName, resolver);
		runtimeNode.BindInput(index, propertyName);
	}

	public override IPropertyResolver BuildResolver(Graph graph)
	{
		IPropertyResolver firstResolver = First?.BuildResolver(graph) ?? CreateDefaultOperandResolver();
		IPropertyResolver secondResolver = Second?.BuildResolver(graph) ?? CreateDefaultOperandResolver();
		IPropertyResolver thirdResolver = Third?.BuildResolver(graph) ?? CreateDefaultOperandResolver();
		return CreateResolver(firstResolver, secondResolver, thirdResolver, graph);
	}

	protected virtual IPropertyResolver CreateDefaultOperandResolver()
	{
		return new VariantResolver(default, typeof(int));
	}
}
