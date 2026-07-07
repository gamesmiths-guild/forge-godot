// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;

namespace Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;

/// <summary>
/// Resolver resource that counts the elements of a nested array source, optionally counting only the elements matching
/// a nested boolean predicate.
/// </summary>
[Tool]
[GlobalClass]
public partial class CountResolverResource : ArrayReductionResolverResourceBase
{
	/// <inheritdoc/>
	public override string ResolverTypeId => "Count";

	/// <inheritdoc/>
	public override string PropertyNamePrefix => "count";

	/// <summary>
	/// Gets or sets the optional nested predicate resolver evaluated per element. Must resolve to
	/// <see langword="bool"/>.
	/// </summary>
	[Export]
	public StatescriptResolverResource? Predicate { get; set; }

	/// <summary>
	/// Gets or sets a value indicating whether the predicate section is folded in the editor.
	/// </summary>
	[Export]
	public bool PredicateFolded { get; set; } = true;

	/// <inheritdoc/>
	public override IPropertyResolver BuildResolver(Graph graph)
	{
		if (!TryResolveSource(
			graph,
			out IArrayPropertyResolver? sourceValueArray,
			out IObjectArrayResolver? sourceObjectArray))
		{
			return new VariantResolver(default, typeof(int));
		}

		IPropertyResolver? predicate = ArrayResolverResourceUtilities.BuildOptionalPredicateResolver(
			Predicate,
			graph,
			ResolverTypeId);

		return sourceObjectArray is not null
			? new CountResolver(sourceObjectArray, predicate)
			: new CountResolver(sourceValueArray!, predicate);
	}
}
