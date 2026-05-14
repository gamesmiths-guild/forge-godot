// Copyright © Gamesmiths Guild.

using System;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;

using ForgeNode = Gamesmiths.Forge.Statescript.Node;

namespace Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;

/// <summary>
/// Resolver resource that binds a node property to an entity's shared variable by name. This is a Godot-side editor
/// convenience over Forge's scope-aware <see cref="VariableResolver"/>.
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

	/// <summary>
	/// Gets or sets a value indicating whether the selected shared variable is an array.
	/// </summary>
	[Export]
	public bool IsArray { get; set; }

	/// <inheritdoc/>
	public override void BindInput(Graph graph, ForgeNode runtimeNode, string nodeId, byte index)
	{
		if (string.IsNullOrEmpty(VariableName))
		{
			return;
		}

		var variableKey = new StringKey(VariableName);
		var propertyName = new StringKey($"__shared_{nodeId}_{index}");
		Type clrType = StatescriptVariableTypeConverter.ToSystemType(VariableType);

		if (IsArray)
		{
			if (VariableType == StatescriptVariableType.Entity)
			{
				graph.VariableDefinitions.DefineReferenceArrayProperty(
					propertyName,
					new ReferenceArrayVariableResolver<IForgeEntity>(variableKey, VariableScope.Shared));
			}
			else
			{
				graph.VariableDefinitions.DefineArrayProperty(
					propertyName,
					new ArrayVariableResolver(variableKey, clrType, VariableScope.Shared));
			}

			runtimeNode.BindInput(index, propertyName);
			return;
		}

		if (VariableType == StatescriptVariableType.Entity)
		{
			graph.VariableDefinitions.DefineReferenceProperty(
				propertyName,
				new EntityVariableResolver(variableKey, VariableScope.Shared));
			runtimeNode.BindInput(index, propertyName);
			return;
		}

		DefineAndBindInputProperty(
			graph,
			runtimeNode,
			propertyName.ToString(),
			index,
			new VariableResolver(variableKey, clrType, VariableScope.Shared));
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
		return new VariableResolver(new StringKey(VariableName), clrType, VariableScope.Shared);
	}
}
