// Copyright © Gamesmiths Guild.

using System;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Effects;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;

using ForgeNode = Gamesmiths.Forge.Statescript.Node;

namespace Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;

/// <summary>
/// Resolver resource that binds a node property to a graph or shared variable by name.
/// </summary>
[Tool]
[GlobalClass]
public partial class VariableResolverResource : EntityResolverResourceBase
{
	/// <inheritdoc/>
	public override string ResolverTypeId => "Variable";

	/// <summary>
	/// Gets or sets the name of the variable to bind to.
	/// </summary>
	[Export]
	public string VariableName { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets which variable bag should be read from or written to.
	/// </summary>
	[Export]
	public VariableScope Scope { get; set; }

	/// <summary>
	/// Gets or sets the resource path of the selected shared variable set. This is editor metadata used for
	/// highlighting and inspector integration when <see cref="Scope"/> is <see cref="VariableScope.Shared"/>.
	/// </summary>
	[Export]
	public string SharedVariableSetPath { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the authored variable type. This is primarily needed for shared-scope lookups whose definitions are
	/// not stored inside the runtime graph.
	/// </summary>
	[Export]
	public StatescriptVariableType VariableType { get; set; } = StatescriptVariableType.Int;

	/// <summary>
	/// Gets or sets a value indicating whether the selected variable is an array. This is primarily editor metadata for
	/// shared variables and output-variable authoring.
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

		VariableMetadata metadata = ResolveVariableMetadata(graph, VariableName);
		var variableKey = new StringKey(VariableName);

		if (metadata.IsArray)
		{
			BindArrayInput(graph, runtimeNode, nodeId, index, variableKey, metadata.ValueType);
			return;
		}

		if (metadata.ValueType == typeof(IForgeEntity))
		{
			base.BindInput(graph, runtimeNode, nodeId, index);
			return;
		}

		if (metadata.ValueType == typeof(Effect))
		{
			var effectPropertyKey = new StringKey($"__effect_{nodeId}_{index}");
			graph.VariableDefinitions.DefineObjectProperty(
				effectPropertyKey,
				new EffectVariableResolver(variableKey, Scope));
			runtimeNode.BindInput(index, effectPropertyKey);
			return;
		}

		if (Scope == VariableScope.Shared || NeedsNumericInputAdaptation(runtimeNode, index, metadata.ValueType))
		{
			DefineAndBindInputProperty(
				graph,
				runtimeNode,
				$"__var_{nodeId}_{index}",
				index,
				new VariableResolver(variableKey, metadata.ValueType, Scope));
			return;
		}

		runtimeNode.BindInput(index, variableKey);
	}

	/// <inheritdoc/>
	public override void BindOutput(ForgeNode runtimeNode, byte index)
	{
		if (string.IsNullOrEmpty(VariableName))
		{
			return;
		}

		runtimeNode.BindOutput(index, new StringKey(VariableName), Scope);
	}

	/// <inheritdoc/>
	public override IPropertyResolver BuildResolver(Graph graph)
	{
		if (string.IsNullOrEmpty(VariableName))
		{
			return new VariantResolver(default, typeof(int));
		}

		VariableMetadata metadata = ResolveVariableMetadata(graph, VariableName);

		if (metadata.IsArray)
		{
			GD.PushError(
				$"Statescript: Variable resolver '{VariableName}' is configured as an array and cannot be used " +
				"where a scalar property resolver is required.");
			return new VariantResolver(default, typeof(int));
		}

		if (metadata.ValueType == typeof(IForgeEntity))
		{
			GD.PushError(
				$"Statescript: Variable resolver '{VariableName}' targets an entity reference and cannot be used " +
				"where a Variant-based property resolver is required.");
			return new VariantResolver(default, typeof(int));
		}

		if (metadata.ValueType == typeof(Effect))
		{
			GD.PushError(
				$"Statescript: Variable resolver '{VariableName}' targets an effect reference and cannot be used " +
				"where a Variant-based property resolver is required.");
			return new VariantResolver(default, typeof(int));
		}

		return new VariableResolver(new StringKey(VariableName), metadata.ValueType, Scope);
	}

	/// <inheritdoc/>
	public override IEntityResolver BuildEntityResolver(Graph graph)
	{
		if (string.IsNullOrEmpty(VariableName))
		{
			GD.PushError(
				"Statescript: Variable resolver is missing a variable name and cannot build an entity resolver.");
			return new EntityVariableResolver(new StringKey(string.Empty), Scope);
		}

		return new EntityVariableResolver(new StringKey(VariableName), Scope);
	}

	private void BindArrayInput(
		Graph graph,
		ForgeNode runtimeNode,
		string nodeId,
		byte index,
		StringKey variableKey,
		Type elementType)
	{
		if (Scope != VariableScope.Shared)
		{
			runtimeNode.BindInput(index, variableKey);
			return;
		}

		var propertyName = new StringKey($"__var_{nodeId}_{index}");

		if (elementType == typeof(IForgeEntity))
		{
			graph.VariableDefinitions.DefineObjectArrayProperty(
				propertyName,
				new ObjectArrayVariableResolver<IForgeEntity>(variableKey, VariableScope.Shared));
		}
		else if (elementType == typeof(Effect))
		{
			graph.VariableDefinitions.DefineObjectArrayProperty(
				propertyName,
				new EffectArrayVariableResolver(variableKey, VariableScope.Shared));
		}
		else
		{
			graph.VariableDefinitions.DefineArrayProperty(
				propertyName,
				new ArrayVariableResolver(variableKey, elementType, VariableScope.Shared));
		}

		runtimeNode.BindInput(index, propertyName);
	}

	private VariableMetadata ResolveVariableMetadata(Graph graph, string variableName)
	{
		if (Scope == VariableScope.Shared)
		{
			return new VariableMetadata(
				StatescriptVariableTypeConverter.ToSystemType(VariableType),
				IsArray);
		}

		var key = new StringKey(variableName);

		foreach (VariableDefinition definition in graph.VariableDefinitions.VariableDefinitions)
		{
			if (definition.Name == key)
			{
				return new VariableMetadata(definition.ValueType, false);
			}
		}

		foreach (ObjectVariableDefinition definition in graph.VariableDefinitions.ObjectVariableDefinitions)
		{
			if (definition.Name == key)
			{
				return new VariableMetadata(definition.ValueType, false);
			}
		}

		foreach (ArrayVariableDefinition definition in graph.VariableDefinitions.ArrayVariableDefinitions)
		{
			if (definition.Name == key)
			{
				return new VariableMetadata(definition.ElementType, true);
			}
		}

		foreach (ObjectArrayVariableDefinition definition
			in graph.VariableDefinitions.ObjectArrayVariableDefinitions)
		{
			if (definition.Name == key)
			{
				return new VariableMetadata(definition.ElementType, true);
			}
		}

		return new VariableMetadata(
			StatescriptVariableTypeConverter.ToSystemType(VariableType),
			IsArray);
	}

	private readonly record struct VariableMetadata(Type ValueType, bool IsArray);
}
