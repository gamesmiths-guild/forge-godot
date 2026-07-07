// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;

namespace Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;

/// <summary>
/// Resolver resource that reads the element at a given index of a nested object array source of any registered object
/// type. The index is a nested numeric resolver; out-of-range indices resolve to <see langword="null"/>.
/// </summary>
[Tool]
[GlobalClass]
public partial class ObjectElementAtResolverResource : ObjectArrayAccessResolverResourceBase
{
	/// <inheritdoc/>
	public override string ResolverTypeId => "ObjectElementAt";

	/// <inheritdoc/>
	public override string PropertyNamePrefix => "objectelementat";

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
	public override IObjectResolver BuildObjectAccessResolver(Graph graph, IObjectArrayResolver source)
	{
		IPropertyResolver index = ArrayResolverResourceUtilities.BuildNumericOperandResolver(
			Index,
			graph,
			ResolverTypeId,
			"index",
			0);

		return ArrayResolverResourceUtilities.CreateObjectAccessResolver(
			typeof(ObjectElementAtResolver<>),
			source,
			index);
	}
}
