// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;
using Godot.Collections;

using ForgeNode = Gamesmiths.Forge.Statescript.Node;

namespace Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;

[Tool]
[GlobalClass]
public partial class ArrayVariableResolverResource : StatescriptResolverResource
{
	[Export]
	public StatescriptVariableType ValueType { get; set; } = StatescriptVariableType.Int;

	[Export]
	public Array<Variant> ArrayValues { get; set; } = [];

	[Export]
	public bool IsArrayExpanded { get; set; }

	public override string ResolverTypeId => "ArrayVariable";

	public override void BindInput(Graph graph, ForgeNode runtimeNode, string nodeId, byte index)
	{
		var propertyName = new StringKey($"__array_{nodeId}_{index}");
		graph.VariableDefinitions.DefineProperty(propertyName, BuildResolver(graph));
		runtimeNode.BindInput(index, propertyName);
	}

	public override IPropertyResolver BuildResolver(Graph graph)
	{
		var values = new Variant128[ArrayValues.Count];
		for (int i = 0; i < ArrayValues.Count; i++)
		{
			values[i] = StatescriptVariableTypeConverter.GodotVariantToForge(ArrayValues[i], ValueType);
		}

		return new ArrayVariableResolver(values, StatescriptVariableTypeConverter.ToSystemType(ValueType));
	}
}
