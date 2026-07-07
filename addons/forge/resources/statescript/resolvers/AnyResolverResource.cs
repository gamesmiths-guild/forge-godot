// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;

namespace Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;

/// <summary>
/// Resolver resource that checks whether a nested array source produces any elements, optionally testing them against
/// a nested boolean predicate (e.g. "is any enemy in range?").
/// </summary>
[Tool]
[GlobalClass]
public partial class AnyResolverResource : ArrayReductionResolverResourceBase
{
	/// <inheritdoc/>
	public override string ResolverTypeId => "Any";

	/// <inheritdoc/>
	public override string PropertyNamePrefix => "any";

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
			return new VariantResolver(new Variant128(false), typeof(bool));
		}

		IPropertyResolver? predicate = ArrayResolverResourceUtilities.BuildOptionalPredicateResolver(
			Predicate,
			graph,
			ResolverTypeId);

		return sourceObjectArray is not null
			? new AnyResolver(sourceObjectArray, predicate)
			: new AnyResolver(sourceValueArray!, predicate);
	}
}
