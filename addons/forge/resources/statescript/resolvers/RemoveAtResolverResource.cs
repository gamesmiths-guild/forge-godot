// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;

namespace Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;

/// <summary>
/// Resolver resource that removes the element at a given index from a nested array source. Out-of-range indices keep
/// the array unchanged.
/// </summary>
[Tool]
[GlobalClass]
public partial class RemoveAtResolverResource : ArrayTransformResolverResourceBase
{
	/// <inheritdoc/>
	public override string ResolverTypeId => "RemoveAt";

	/// <inheritdoc/>
	public override string PropertyNamePrefix => "removeat";

	/// <summary>
	/// Gets or sets the nested resolver providing the zero-based index to remove. Must resolve to a numeric type.
	/// </summary>
	[Export]
	public StatescriptResolverResource? Index { get; set; }

	/// <summary>
	/// Gets or sets a value indicating whether the index section is folded in the editor.
	/// </summary>
	[Export]
	public bool IndexFolded { get; set; } = true;

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

		IPropertyResolver index = ArrayResolverResourceUtilities.BuildNumericOperandResolver(
			Index,
			graph,
			ResolverTypeId,
			"index",
			-1);

		if (sourceObjectArray is not null)
		{
			objectArrayResolver = ArrayResolverResourceUtilities.CreateObjectArrayOperation(
				typeof(ObjectRemoveAtResolver<>),
				sourceObjectArray,
				index);
			return true;
		}

		valueArrayResolver = new RemoveAtResolver(sourceValueArray!, index);
		return true;
	}
}
