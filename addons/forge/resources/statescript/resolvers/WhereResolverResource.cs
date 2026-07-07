// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;

namespace Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;

/// <summary>
/// Resolver resource that keeps the elements of a nested array source matching a nested boolean predicate. The
/// predicate reads the current element through the element resolvers.
/// </summary>
[Tool]
[GlobalClass]
public partial class WhereResolverResource : ArrayTransformResolverResourceBase
{
	/// <inheritdoc/>
	public override string ResolverTypeId => "Where";

	/// <inheritdoc/>
	public override string PropertyNamePrefix => "where";

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

		IPropertyResolver predicate = BuildPredicateResolver(graph);

		if (sourceObjectArray is not null)
		{
			objectArrayResolver = ArrayResolverResourceUtilities.CreateObjectArrayOperation(
				typeof(ObjectWhereResolver<>),
				sourceObjectArray,
				predicate);
			return true;
		}

		valueArrayResolver = new WhereResolver(sourceValueArray!, predicate);
		return true;
	}

	private IPropertyResolver BuildPredicateResolver(Graph graph)
	{
		if (Predicate is null)
		{
			GD.PushError("Statescript: Where resolver is missing a predicate; keeping all elements.");
			return new VariantResolver(new Variant128(true), typeof(bool));
		}

		IPropertyResolver predicate = Predicate.BuildResolver(graph);

		if (predicate.ValueType != typeof(bool))
		{
			GD.PushError(
				$"Statescript: Where resolver predicate must resolve to bool. Got '{predicate.ValueType}'. Keeping " +
				"all elements.");
			return new VariantResolver(new Variant128(true), typeof(bool));
		}

		return predicate;
	}
}
