// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;

namespace Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;

[Tool]
[GlobalClass]
public partial class TargetEntityResolverResource : EntityResolverResourceBase
{
	public override string ResolverTypeId => "TargetEntity";

	public override IEntityResolver BuildEntityResolver(Graph graph)
	{
		return new TargetEntityResolver();
	}
}
