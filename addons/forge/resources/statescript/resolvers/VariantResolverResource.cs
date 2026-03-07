// Copyright © Gamesmiths Guild.

using System;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;
using Godot.Collections;

using ForgeNode = Gamesmiths.Forge.Statescript.Node;

namespace Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;

/// <summary>
/// Resolver resource that holds a constant (inline) value for a node property.
/// </summary>
[Tool]
[GlobalClass]
public partial class VariantResolverResource : StatescriptResolverResource
{
	/// <summary>
	/// Gets or sets the constant value. Used when <see cref="IsArray"/> is <see langword="false"/>.
	/// </summary>
	[Export]
	public Variant Value { get; set; }

	/// <summary>
	/// Gets or sets the type interpretation for the value.
	/// </summary>
	[Export]
	public StatescriptVariableType ValueType { get; set; } = StatescriptVariableType.Int;

	/// <summary>
	/// Gets or sets a value indicating whether this resolver holds an array of values.
	/// </summary>
	[Export]
	public bool IsArray { get; set; }

	/// <summary>
	/// Gets or sets the array values. Used when <see cref="IsArray"/> is <see langword="true"/>.
	/// </summary>
	[Export]
	public Array<Variant> ArrayValues { get; set; } = [];

	/// <summary>
	/// Gets or sets a value indicating whether the array section is expanded in the editor.
	/// </summary>
	[Export]
	public bool IsArrayExpanded { get; set; }

	/// <inheritdoc/>
	public override void BindInput(Graph graph, ForgeNode runtimeNode, string nodeId, byte index)
	{
		var propertyName = new StringKey($"__const_{nodeId}_{index}");

		if (IsArray)
		{
			var values = new Variant128[ArrayValues.Count];
			for (var i = 0; i < ArrayValues.Count; i++)
			{
				values[i] = StatescriptVariableTypeConverter.GodotVariantToForge(ArrayValues[i], ValueType);
			}

			Type clrType = StatescriptVariableTypeConverter.ToSystemType(ValueType);

			graph.VariableDefinitions.ArrayVariableDefinitions.Add(
				new ArrayVariableDefinition(propertyName, values, clrType));
		}
		else
		{
			Variant128 value = StatescriptVariableTypeConverter.GodotVariantToForge(Value, ValueType);
			Type clrType = StatescriptVariableTypeConverter.ToSystemType(ValueType);

			graph.VariableDefinitions.PropertyDefinitions.Add(
				new PropertyDefinition(propertyName, new VariantResolver(value, clrType)));
		}

		runtimeNode.BindInput(index, propertyName);
	}

	/// <inheritdoc/>
	public override IPropertyResolver BuildResolver(Graph graph)
	{
		Variant128 value = StatescriptVariableTypeConverter.GodotVariantToForge(Value, ValueType);
		Type clrType = StatescriptVariableTypeConverter.ToSystemType(ValueType);
		return new VariantResolver(value, clrType);
	}
}
