// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;

namespace Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;

/// <summary>
/// Resolver resource that projects each element of a nested array source through a nested projection resolver,
/// producing a value-typed array (e.g. "the health of each entity"). The projection reads the current element through
/// the element resolvers.
/// </summary>
[Tool]
[GlobalClass]
public partial class SelectResolverResource : ArrayTransformResolverResourceBase
{
	/// <inheritdoc/>
	public override string ResolverTypeId => "Select";

	/// <inheritdoc/>
	public override string PropertyNamePrefix => "select";

	/// <summary>
	/// Gets or sets the nested projection resolver evaluated per element.
	/// </summary>
	[Export]
	public StatescriptResolverResource? Projection { get; set; }

	/// <summary>
	/// Gets or sets a value indicating whether the projection section is folded in the editor.
	/// </summary>
	[Export]
	public bool ProjectionFolded { get; set; } = true;

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

		IPropertyResolver projection;

		if (Projection is null)
		{
			GD.PushError("Statescript: Select resolver is missing a projection; producing zeros.");
			projection = new VariantResolver(default, typeof(int));
		}
		else
		{
			projection = Projection.BuildResolver(graph);
		}

		valueArrayResolver = sourceObjectArray is not null
			? new SelectResolver(sourceObjectArray, projection)
			: new SelectResolver(sourceValueArray!, projection);
		return true;
	}
}
