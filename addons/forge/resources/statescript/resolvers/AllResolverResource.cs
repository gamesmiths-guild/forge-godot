// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;

namespace Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;

/// <summary>
/// Resolver resource that checks whether every element of a nested array source satisfies a nested boolean predicate
/// (e.g. "are all targets dead?"). Empty arrays resolve to <see langword="true"/>.
/// </summary>
[Tool]
[GlobalClass]
public partial class AllResolverResource : ArrayReductionResolverResourceBase
{
	/// <inheritdoc/>
	public override string ResolverTypeId => "All";

	/// <inheritdoc/>
	public override string PropertyNamePrefix => "all";

	/// <summary>
	/// Gets or sets the nested predicate resolver evaluated per element. Must resolve to <see langword="bool"/>.
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
			return new VariantResolver(new Variant128(true), typeof(bool));
		}

		IPropertyResolver? predicate = ArrayResolverResourceUtilities.BuildOptionalPredicateResolver(
			Predicate,
			graph,
			ResolverTypeId);

		if (predicate is null)
		{
			GD.PushError("Statescript: All resolver is missing a predicate; resolving to true.");
			return new VariantResolver(new Variant128(true), typeof(bool));
		}

		return sourceObjectArray is not null
			? new AllResolver(sourceObjectArray, predicate)
			: new AllResolver(sourceValueArray!, predicate);
	}
}
