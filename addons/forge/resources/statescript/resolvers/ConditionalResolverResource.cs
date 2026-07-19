// Copyright © Gamesmiths Guild.

using System;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;

using ForgeNode = Gamesmiths.Forge.Statescript.Node;

namespace Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;

/// <summary>
/// Resolver resource that selects one of two values based on a boolean condition — the ternary select. Supports both
/// value-typed branches and object-backed branches (for example, picking between two entities).
/// </summary>
[Tool]
[GlobalClass]
public partial class ConditionalResolverResource : StatescriptResolverResource
{
	/// <inheritdoc/>
	public override string ResolverTypeId => "Conditional";

	/// <summary>
	/// Gets or sets the nested resolver providing the boolean condition.
	/// </summary>
	[Export]
	public StatescriptResolverResource? Condition { get; set; }

	/// <summary>
	/// Gets or sets the nested resolver evaluated when the condition is true.
	/// </summary>
	[Export]
	public StatescriptResolverResource? WhenTrue { get; set; }

	/// <summary>
	/// Gets or sets the nested resolver evaluated when the condition is false.
	/// </summary>
	[Export]
	public StatescriptResolverResource? WhenFalse { get; set; }

	/// <summary>
	/// Gets or sets a value indicating whether the condition section is folded in the editor.
	/// </summary>
	[Export]
	public bool ConditionFolded { get; set; }

	/// <summary>
	/// Gets or sets a value indicating whether the when-true section is folded in the editor.
	/// </summary>
	[Export]
	public bool WhenTrueFolded { get; set; }

	/// <summary>
	/// Gets or sets a value indicating whether the when-false section is folded in the editor.
	/// </summary>
	[Export]
	public bool WhenFalseFolded { get; set; }

	/// <inheritdoc/>
	public override void BindInput(Graph graph, ForgeNode runtimeNode, string nodeId, byte index)
	{
		var propertyName = new StringKey($"__conditional_{nodeId}_{index}");

		if (TryBuildObjectResolver(graph, out IObjectResolver? objectResolver) && objectResolver is not null)
		{
			graph.VariableDefinitions.DefineObjectProperty(propertyName, objectResolver);
			runtimeNode.BindInput(index, propertyName);
			return;
		}

		DefineAndBindInputProperty(graph, runtimeNode, $"__conditional_{nodeId}_{index}", index, BuildResolver(graph));
	}

	/// <inheritdoc/>
	public override IPropertyResolver BuildResolver(Graph graph)
	{
		IPropertyResolver conditionResolver = BuildConditionResolver(graph);
		IPropertyResolver whenTrueResolver = WhenTrue?.BuildResolver(graph)
			?? new VariantResolver(default, typeof(int));
		IPropertyResolver whenFalseResolver = WhenFalse?.BuildResolver(graph)
			?? new VariantResolver(default, typeof(int));

		if (whenTrueResolver.ValueType != whenFalseResolver.ValueType)
		{
			Type preferredType = GetPreferredFloatingPointType(whenTrueResolver, whenFalseResolver);
			whenTrueResolver = AdaptResolverForExpectedType(whenTrueResolver, preferredType);
			whenFalseResolver = AdaptResolverForExpectedType(whenFalseResolver, preferredType);
		}

		if (whenTrueResolver.ValueType != whenFalseResolver.ValueType)
		{
			GD.PushError(
				"Statescript: Conditional resolver requires both branches to produce the same value type. Got " +
				$"'{whenTrueResolver.ValueType.Name}' and '{whenFalseResolver.ValueType.Name}'.");
			return new VariantResolver(default, typeof(int));
		}

		return new ConditionalResolver(conditionResolver, whenTrueResolver, whenFalseResolver);
	}

	/// <inheritdoc/>
	public override bool TryBuildObjectResolver(Graph graph, out IObjectResolver? objectResolver)
	{
		objectResolver = null;

		if (WhenTrue is null
			|| WhenFalse is null
			|| !WhenTrue.TryBuildObjectResolver(graph, out IObjectResolver? whenTrueResolver)
			|| whenTrueResolver is null
			|| !WhenFalse.TryBuildObjectResolver(graph, out IObjectResolver? whenFalseResolver)
			|| whenFalseResolver is null)
		{
			return false;
		}

		if (whenTrueResolver.ValueType != whenFalseResolver.ValueType)
		{
			GD.PushError(
				"Statescript: Conditional resolver requires both object branches to produce the same type. Got " +
				$"'{whenTrueResolver.ValueType.Name}' and '{whenFalseResolver.ValueType.Name}'.");
			return false;
		}

		objectResolver = (IObjectResolver)Activator.CreateInstance(
			typeof(ConditionalObjectResolver<>).MakeGenericType(whenTrueResolver.ValueType),
			BuildConditionResolver(graph),
			whenTrueResolver,
			whenFalseResolver)!;

		return true;
	}

	private IPropertyResolver BuildConditionResolver(Graph graph)
	{
		return Condition?.BuildResolver(graph) ?? new VariantResolver(default, typeof(bool));
	}
}
