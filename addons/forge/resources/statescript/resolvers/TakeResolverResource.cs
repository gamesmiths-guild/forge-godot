// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;

namespace Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;

/// <summary>
/// Resolver resource that keeps the first N elements of a nested array source.
/// </summary>
[Tool]
[GlobalClass]
public partial class TakeResolverResource : ArrayTransformResolverResourceBase
{
	/// <inheritdoc/>
	public override string ResolverTypeId => "Take";

	/// <inheritdoc/>
	public override string PropertyNamePrefix => "take";

	/// <summary>
	/// Gets or sets the nested resolver providing the number of elements to keep. Must resolve to a numeric type.
	/// </summary>
	[Export]
	public StatescriptResolverResource? Count { get; set; }

	/// <summary>
	/// Gets or sets a value indicating whether the count section is folded in the editor.
	/// </summary>
	[Export]
	public bool CountFolded { get; set; } = true;

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

		IPropertyResolver count = ArrayResolverResourceUtilities.BuildNumericOperandResolver(
			Count,
			graph,
			ResolverTypeId,
			"count",
			1);

		if (sourceObjectArray is not null)
		{
			objectArrayResolver = ArrayResolverResourceUtilities.CreateObjectArrayOperation(
				typeof(ObjectTakeResolver<>),
				sourceObjectArray,
				count);
			return true;
		}

		valueArrayResolver = new TakeResolver(sourceValueArray!, count);
		return true;
	}
}
