// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;

namespace Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;

/// <summary>
/// Resolver resource that sorts the elements of a nested array source by a nested numeric key selector. The key
/// selector reads the current element through the element resolvers (e.g. an attribute of the iterated entity).
/// </summary>
[Tool]
[GlobalClass]
public partial class OrderByResolverResource : ArrayTransformResolverResourceBase
{
	/// <inheritdoc/>
	public override string ResolverTypeId => "OrderBy";

	/// <inheritdoc/>
	public override string PropertyNamePrefix => "orderby";

	/// <summary>
	/// Gets or sets the nested key selector resolver evaluated per element. Must resolve to a numeric type.
	/// </summary>
	[Export]
	public StatescriptResolverResource? KeySelector { get; set; }

	/// <summary>
	/// Gets or sets a value indicating whether the key selector section is folded in the editor.
	/// </summary>
	[Export]
	public bool KeySelectorFolded { get; set; } = true;

	/// <summary>
	/// Gets or sets the ordering to apply.
	/// </summary>
	[Export]
	public SortDirection Direction { get; set; } = SortDirection.Ascending;

	/// <inheritdoc/>
	public override bool TryBuildArrayResolver(
		Graph graph,
		out IArrayPropertyResolver? valueArrayResolver,
		out IObjectArrayResolver? objectArrayResolver)
	{
		valueArrayResolver = null;
		objectArrayResolver = null;

		if (!TryResolveSource(
			graph,
			out IArrayPropertyResolver? sourceValueArray,
			out IObjectArrayResolver? sourceObjectArray))
		{
			return false;
		}

		IPropertyResolver keySelector = BuildKeySelectorResolver(graph);

		if (sourceObjectArray is not null)
		{
			objectArrayResolver = ArrayResolverResourceUtilities.CreateObjectArrayOperation(
				typeof(ObjectOrderByResolver<>),
				sourceObjectArray,
				keySelector,
				Direction);
			return true;
		}

		valueArrayResolver = new OrderByResolver(sourceValueArray!, keySelector, Direction);
		return true;
	}

	private IPropertyResolver BuildKeySelectorResolver(Graph graph)
	{
		if (KeySelector is null)
		{
			GD.PushError("Statescript: OrderBy resolver is missing a key selector; keeping the original order.");
			return new VariantResolver(default, typeof(int));
		}

		IPropertyResolver keySelector = KeySelector.BuildResolver(graph);

		if (!ArrayResolverResourceUtilities.IsNumericValueType(keySelector.ValueType))
		{
			GD.PushError(
				"Statescript: OrderBy resolver key selector must resolve to a numeric type. Got " +
				$"'{keySelector.ValueType}'. Keeping the original order.");
			return new VariantResolver(default, typeof(int));
		}

		return keySelector;
	}
}
