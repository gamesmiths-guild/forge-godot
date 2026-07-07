// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;

namespace Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;

/// <summary>
/// Resolver resource that checks whether a nested value-typed array source contains a given value. For entity arrays
/// use <see cref="ObjectContainsResolverResource"/>.
/// </summary>
[Tool]
[GlobalClass]
public partial class ContainsResolverResource : ArrayReductionResolverResourceBase
{
	/// <inheritdoc/>
	public override string ResolverTypeId => "Contains";

	/// <inheritdoc/>
	public override string PropertyNamePrefix => "contains";

	/// <summary>
	/// Gets or sets the nested resolver providing the value to search for. Must resolve to the source element type.
	/// </summary>
	[Export]
	public StatescriptResolverResource? Value { get; set; }

	/// <summary>
	/// Gets or sets a value indicating whether the value section is folded in the editor.
	/// </summary>
	[Export]
	public bool ValueFolded { get; set; } = true;

	/// <inheritdoc/>
	public override IPropertyResolver BuildResolver(Graph graph)
	{
		if (!TryResolveSource(
			graph,
			out IArrayPropertyResolver? sourceValueArray,
			out IObjectArrayResolver? sourceObjectArray))
		{
			return new VariantResolver(new Variant128(false), typeof(bool));
		}

		if (sourceObjectArray is not null)
		{
			GD.PushError(
				"Statescript: Contains resolver requires a value-typed array source. Use Contains Entity for entity " +
				"arrays.");
			return new VariantResolver(new Variant128(false), typeof(bool));
		}

		if (Value is null)
		{
			GD.PushError("Statescript: Contains resolver is missing a value to search for.");
			return new VariantResolver(new Variant128(false), typeof(bool));
		}

		IPropertyResolver value = Value.BuildResolver(graph);

		if (value.ValueType != sourceValueArray!.ElementType)
		{
			GD.PushError(
				$"Statescript: Contains resolver value produces '{value.ValueType}', which does not match the source " +
				$"element type '{sourceValueArray.ElementType}'.");
			return new VariantResolver(new Variant128(false), typeof(bool));
		}

		return new ContainsResolver(sourceValueArray, value);
	}
}
