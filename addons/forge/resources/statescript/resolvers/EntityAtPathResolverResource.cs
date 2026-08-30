// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Godot.Core.Statescript.Resolvers;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;

namespace Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;

/// <summary>
/// Resolver resource that reads the entity at an authored scene path.
/// </summary>
[Tool]
[GlobalClass]
public partial class EntityAtPathResolverResource : EntityResolverResourceBase
{
	/// <inheritdoc/>
	public override string ResolverTypeId => "EntityAtPath";

	/// <summary>
	/// Gets or sets the path to resolve, from the current scene's root.
	/// </summary>
	/// <remarks>
	/// Absolute paths (<c>/root/Main/Boss</c>) and scene-unique names (<c>%Boss</c>) work, the latter for nodes marked
	/// unique in the current scene itself.
	/// </remarks>
	[Export]
	public string NodePath { get; set; } = string.Empty;

	/// <inheritdoc/>
	public override IEntityResolver BuildEntityResolver(Graph graph)
	{
		return new EntityAtPathResolver(NodePath);
	}
}
