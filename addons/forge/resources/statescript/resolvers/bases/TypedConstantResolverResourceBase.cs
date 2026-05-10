// Copyright © Gamesmiths Guild.

using System;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;

using ForgeNode = Gamesmiths.Forge.Statescript.Node;

namespace Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers.Bases;

public abstract partial class TypedConstantResolverResourceBase : StatescriptResolverResource
{
	protected abstract string PropertyNamePrefix { get; }

	protected abstract IPropertyResolver CreateResolver(Type valueType);

	[Export]
	public StatescriptVariableType ValueType { get; set; } = StatescriptVariableType.Double;

	public override void BindInput(Graph graph, ForgeNode runtimeNode, string nodeId, byte index)
	{
		IPropertyResolver resolver = BuildResolver(graph);
		DefineAndBindInputProperty(graph, runtimeNode, $"{PropertyNamePrefix}_{nodeId}_{index}", index, resolver);
	}

	public override IPropertyResolver BuildResolver(Graph graph)
	{
		return CreateResolver(StatescriptVariableTypeConverter.ToSystemType(StatescriptVariableType.Double));
	}
}
