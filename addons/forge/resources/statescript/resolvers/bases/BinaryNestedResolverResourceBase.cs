// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;

using ForgeNode = Gamesmiths.Forge.Statescript.Node;

namespace Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers.Bases;

public abstract partial class BinaryNestedResolverResourceBase : StatescriptResolverResource
{
	protected abstract string PropertyNamePrefix { get; }

	protected abstract IPropertyResolver CreateResolver(
		IPropertyResolver leftResolver,
		IPropertyResolver rightResolver,
		Graph graph);

	[Export]
	public StatescriptResolverResource? Left { get; set; }

	[Export]
	public StatescriptResolverResource? Right { get; set; }

	[Export]
	public bool LeftFolded { get; set; }

	[Export]
	public bool RightFolded { get; set; }

	public override void BindInput(Graph graph, ForgeNode runtimeNode, string nodeId, byte index)
	{
		IPropertyResolver resolver = BuildResolver(graph);
		DefineAndBindInputProperty(graph, runtimeNode, $"{PropertyNamePrefix}_{nodeId}_{index}", index, resolver);
	}

	public override IPropertyResolver BuildResolver(Graph graph)
	{
		IPropertyResolver leftResolver = Left?.BuildResolver(graph) ?? CreateDefaultOperandResolver();
		IPropertyResolver rightResolver = Right?.BuildResolver(graph) ?? CreateDefaultOperandResolver();
		return CreateResolver(leftResolver, rightResolver, graph);
	}

	protected virtual IPropertyResolver CreateDefaultOperandResolver()
	{
		return new VariantResolver(default, typeof(int));
	}
}
