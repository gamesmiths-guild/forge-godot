// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;

namespace Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;

[Tool]
public abstract partial class EntityResolverResourceBase : StatescriptResolverResource
{
	public abstract IEntityResolver BuildEntityResolver(Graph graph);
}
