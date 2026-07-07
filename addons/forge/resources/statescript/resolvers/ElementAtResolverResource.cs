// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;

namespace Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;

/// <summary>
/// Resolver resource that reads the element at a given index of a nested value-typed array source. For entity arrays
/// use <see cref="EntityElementAtResolverResource"/>.
/// </summary>
[Tool]
[GlobalClass]
public partial class ElementAtResolverResource : ArrayReductionResolverResourceBase
{
	/// <inheritdoc/>
	public override string ResolverTypeId => "ElementAt";

	/// <inheritdoc/>
	public override string PropertyNamePrefix => "elementat";

	/// <summary>
	/// Gets or sets the nested resolver providing the zero-based element index. Must resolve to a numeric type.
	/// </summary>
	[Export]
	public StatescriptResolverResource? Index { get; set; }

	/// <summary>
	/// Gets or sets a value indicating whether the index section is folded in the editor.
	/// </summary>
	[Export]
	public bool IndexFolded { get; set; } = true;

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

		if (sourceObjectArray is not null)
		{
			GD.PushError(
				"Statescript: Element At resolver requires a value-typed array source. Use Entity At for entity " +
				"arrays.");
			return new VariantResolver(default, typeof(int));
		}

		IPropertyResolver index = ArrayResolverResourceUtilities.BuildNumericOperandResolver(
			Index,
			graph,
			ResolverTypeId,
			"index",
			0);

		return new ElementAtResolver(sourceValueArray!, index);
	}
}
