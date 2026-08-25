// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;

namespace Gamesmiths.Forge.Godot.Core.Statescript.Resolvers;

/// <summary>
/// Resolves the entity at an authored scene path.
/// </summary>
/// <remarks>
/// <para>Exactly Entity From Node over the Node Path constant, in one row instead of two. It earns the shortcut because
/// naming a character already in the level - a boss, a quest giver, a training dummy - is common enough that spelling
/// it as a nested pair every time is friction rather than composition.</para>
/// <para>The path is resolved from the current scene's root each time, so it follows a scene reload and never holds a
/// reference to a freed node. Both halves keep their own behavior: a path that finds nothing warns once through the
/// path lookup, and the node it does find is searched for its entity in both directions.</para>
/// </remarks>
/// <param name="nodePath">The authored path, resolved from the current scene's root.</param>
internal sealed class EntityAtPathResolver(string nodePath) : ObjectResolver<IForgeEntity>, IEntityResolver
{
	private readonly EntityFromNodeResolver _inner = new(new NodePathResolver(nodePath));

	public override IForgeEntity? Resolve(GraphContext graphContext)
	{
		return _inner.Resolve(graphContext);
	}
}
