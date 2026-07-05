// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;

namespace Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;

/// <summary>
/// Resolver resource that finds the zero-based index of the first occurrence of a given value in a nested value-typed
/// array source, or -1 when absent. For entity arrays use <see cref="ObjectIndexOfResolverResource"/>.
/// </summary>
[Tool]
[GlobalClass]
public partial class IndexOfResolverResource : ArrayReductionResolverResourceBase
{
	/// <inheritdoc/>
	public override string ResolverTypeId => "IndexOf";

	/// <inheritdoc/>
	public override string PropertyNamePrefix => "indexof";

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
			return new VariantResolver(new Variant128(-1), typeof(int));
		}

		if (sourceObjectArray is not null)
		{
			GD.PushError(
				"Statescript: Index Of resolver requires a value-typed array source. Use Entity Index Of for entity " +
				"arrays.");
			return new VariantResolver(new Variant128(-1), typeof(int));
		}

		if (Value is null)
		{
			GD.PushError("Statescript: Index Of resolver is missing a value to search for.");
			return new VariantResolver(new Variant128(-1), typeof(int));
		}

		IPropertyResolver value = Value.BuildResolver(graph);

		if (value.ValueType != sourceValueArray!.ElementType)
		{
			GD.PushError(
				$"Statescript: Index Of resolver value produces '{value.ValueType}', which does not match the source " +
				$"element type '{sourceValueArray.ElementType}'.");
			return new VariantResolver(new Variant128(-1), typeof(int));
		}

		return new IndexOfResolver(sourceValueArray, value);
	}
}
