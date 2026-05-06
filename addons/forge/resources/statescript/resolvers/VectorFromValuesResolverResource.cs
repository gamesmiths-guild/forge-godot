// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;

using ForgeNode = Gamesmiths.Forge.Statescript.Node;

namespace Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;

[Tool]
[GlobalClass]
public partial class VectorFromValuesResolverResource : StatescriptResolverResource
{
	[Export]
	public StatescriptVariableType ValueType { get; set; } = StatescriptVariableType.Vector3;

	[Export]
	public StatescriptResolverResource? X { get; set; }

	[Export]
	public StatescriptResolverResource? Y { get; set; }

	[Export]
	public StatescriptResolverResource? Z { get; set; }

	[Export]
	public StatescriptResolverResource? W { get; set; }

	[Export]
	public bool XFolded { get; set; }

	[Export]
	public bool YFolded { get; set; }

	[Export]
	public bool ZFolded { get; set; }

	[Export]
	public bool WFolded { get; set; }

	public override string ResolverTypeId => "VectorFromValues";

	public override void BindInput(Graph graph, ForgeNode runtimeNode, string nodeId, byte index)
	{
		IPropertyResolver resolver = BuildResolver(graph);
		var propertyName = new StringKey($"__vectorfromvalues_{nodeId}_{index}");
		graph.VariableDefinitions.DefineProperty(propertyName, resolver);
		runtimeNode.BindInput(index, propertyName);
	}

	public override IPropertyResolver BuildResolver(Graph graph)
	{
		IPropertyResolver xResolver = X?.BuildResolver(graph) ?? CreateDefaultComponentResolver();
		IPropertyResolver yResolver = Y?.BuildResolver(graph) ?? CreateDefaultComponentResolver();
		IPropertyResolver zResolver = Z?.BuildResolver(graph) ?? CreateDefaultComponentResolver();
		IPropertyResolver wResolver = W?.BuildResolver(graph) ?? CreateDefaultComponentResolver();

		return GetNormalizedValueType() switch
		{
			StatescriptVariableType.Vector2 => new VectorFromValuesResolver(xResolver, yResolver),
			StatescriptVariableType.Vector4 => new VectorFromValuesResolver(xResolver, yResolver, zResolver, wResolver),
			_ => new VectorFromValuesResolver(xResolver, yResolver, zResolver),
		};
	}

	private static VariantResolver CreateDefaultComponentResolver()
	{
		return new VariantResolver(default, typeof(float));
	}

	private StatescriptVariableType GetNormalizedValueType()
	{
		return ValueType switch
		{
			StatescriptVariableType.Vector2 => StatescriptVariableType.Vector2,
			StatescriptVariableType.Vector4 => StatescriptVariableType.Vector4,
			_ => StatescriptVariableType.Vector3,
		};
	}
}
