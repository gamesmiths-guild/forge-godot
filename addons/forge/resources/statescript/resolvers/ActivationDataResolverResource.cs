// Copyright © Gamesmiths Guild.

using System;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;

using ForgeNode = Gamesmiths.Forge.Statescript.Node;

namespace Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;

/// <summary>
/// Resolver resource that binds a node property to a field declared by an <see cref="IActivationDataProvider"/>.
/// </summary>
/// <remarks>
/// At build time the resolver defines a graph variable for the field so that the data binder can write to it, and binds
/// the node input to that variable. At runtime the value is read from the graph's variables after the data binder has
/// populated them.
/// </remarks>
[Tool]
[GlobalClass]
public partial class ActivationDataResolverResource : StatescriptResolverResource
{
	/// <inheritdoc/>
	public override string ResolverTypeId => "ActivationData";

	/// <summary>
	/// Gets or sets the class name of the <see cref="IActivationDataProvider"/> implementation that declares the field.
	/// </summary>
	[Export]
	public string ProviderClassName { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the name of the activation data field to bind to.
	/// </summary>
	[Export]
	public string FieldName { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the expected type of the activation data field.
	/// </summary>
	[Export]
	public StatescriptVariableType FieldType { get; set; } = StatescriptVariableType.Int;

	/// <inheritdoc/>
	public override void BindInput(Graph graph, ForgeNode runtimeNode, string nodeId, byte index)
	{
		if (string.IsNullOrEmpty(ProviderClassName))
		{
			GD.PushError(
				$"Statescript: Activation Data resolver on node '{nodeId}' (input {index}) " +
				"has no provider selected. Select a provider and field in the graph editor.");
			return;
		}

		if (string.IsNullOrEmpty(FieldName))
		{
			GD.PushError(
				$"Statescript: Activation Data resolver on node '{nodeId}' (input {index}) " +
				$"has provider '{ProviderClassName}' but no field selected. " +
				"Select a field in the graph editor.");
			return;
		}

		Type clrType = StatescriptVariableTypeConverter.ToSystemType(FieldType);
		var variableName = new StringKey(FieldName);

		// Define the variable so the data binder's SetVar call succeeds at runtime.
		// Check if the variable is already defined to avoid duplicates when multiple nodes bind the same field.
		var alreadyDefined = false;
		foreach (VariableDefinition existing in graph.VariableDefinitions.VariableDefinitions)
		{
			if (existing.Name == variableName)
			{
				alreadyDefined = true;
				break;
			}
		}

		if (!alreadyDefined)
		{
			graph.VariableDefinitions.VariableDefinitions.Add(
				new VariableDefinition(variableName, default, clrType));
		}

		runtimeNode.BindInput(index, variableName);
	}

	/// <inheritdoc/>
	public override IPropertyResolver BuildResolver(Graph graph)
	{
		if (string.IsNullOrEmpty(ProviderClassName) || string.IsNullOrEmpty(FieldName))
		{
			GD.PushError(
				"Statescript: Activation Data resolver has incomplete configuration " +
				$"(provider: '{ProviderClassName}', field: '{FieldName}'). " +
				"The resolver will return a default value.");
			return new VariantResolver(default, typeof(int));
		}

		Type clrType = StatescriptVariableTypeConverter.ToSystemType(FieldType);
		return new VariableResolver(new StringKey(FieldName), clrType);
	}
}
