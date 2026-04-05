// Copyright © Gamesmiths Guild.

using System;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;

using ForgeNode = Gamesmiths.Forge.Statescript.Node;

namespace Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;

/// <summary>
/// Resolver resource that binds a node property to an entity's shared variable by name. At runtime the value is read
/// from the <see cref="GraphContext.SharedVariables"/> bag, which is populated from the entity's
/// <see cref="ForgeSharedVariableSet"/>.
/// </summary>
[Tool]
[GlobalClass]
public partial class SharedVariableResolverResource : StatescriptResolverResource
{
	/// <inheritdoc/>
	public override string ResolverTypeId => "SharedVariable";

	/// <summary>
	/// Gets or sets the resource path of the <see cref="ForgeSharedVariableSet"/> that defines the variable.
	/// </summary>
	[Export]
	public string SharedVariableSetPath { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the name of the shared variable to bind to.
	/// </summary>
	[Export]
	public string VariableName { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the expected type of the shared variable.
	/// </summary>
	[Export]
	public StatescriptVariableType VariableType { get; set; } = StatescriptVariableType.Int;

	/// <inheritdoc/>
	public override void BindInput(Graph graph, ForgeNode runtimeNode, string nodeId, byte index)
	{
		if (string.IsNullOrEmpty(VariableName))
		{
			return;
		}

		Type clrType = StatescriptVariableTypeConverter.ToSystemType(VariableType);
		var propertyName = new StringKey($"__shared_{nodeId}_{index}");

		graph.VariableDefinitions.DefineProperty(
			propertyName,
			new SharedVariableResolver(new StringKey(VariableName), clrType));

		runtimeNode.BindInput(index, propertyName);
	}

	/// <inheritdoc/>
	public override void BindOutput(ForgeNode runtimeNode, byte index)
	{
		if (string.IsNullOrEmpty(VariableName))
		{
			return;
		}

		runtimeNode.BindOutput(index, new StringKey(VariableName), VariableScope.Shared);
	}

	/// <inheritdoc/>
	public override IPropertyResolver BuildResolver(Graph graph)
	{
		if (string.IsNullOrEmpty(VariableName))
		{
			return new VariantResolver(default, typeof(int));
		}

		Type clrType = StatescriptVariableTypeConverter.ToSystemType(VariableType);
		return new SharedVariableResolver(new StringKey(VariableName), clrType);
	}
}
