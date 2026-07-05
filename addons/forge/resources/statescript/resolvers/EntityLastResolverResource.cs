// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;

namespace Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;

/// <summary>
/// Resolver resource that reads the last entity of a nested entity array source. Usable anywhere an entity resolver
/// is expected (attributes, tag queries, validity checks).
/// </summary>
[Tool]
[GlobalClass]
public partial class EntityLastResolverResource : EntityResolverResourceBase
{
	/// <inheritdoc/>
	public override string ResolverTypeId => "EntityLast";

	/// <summary>
	/// Gets or sets the nested resolver providing the source entity array.
	/// </summary>
	[Export]
	public StatescriptResolverResource? Source { get; set; }

	/// <summary>
	/// Gets or sets a value indicating whether the source section is folded in the editor.
	/// </summary>
	[Export]
	public bool SourceFolded { get; set; } = true;

	/// <inheritdoc/>
	public override IEntityResolver BuildEntityResolver(Graph graph)
	{
		if (!ArrayResolverResourceUtilities.TryResolveEntityArraySource(
			Source,
			ResolverTypeId,
			graph,
			out IObjectArrayResolver<IForgeEntity>? entityArray))
		{
			return new EntityVariableResolver(new StringKey(string.Empty));
		}

		return new EntityLastResolver(entityArray!);
	}
}
