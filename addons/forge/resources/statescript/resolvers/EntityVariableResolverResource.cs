// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;

namespace Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;

[Tool]
[GlobalClass]
public partial class EntityVariableResolverResource : EntityResolverResourceBase
{
	public override string ResolverTypeId => "EntityVariable";

	[Export]
	public string VariableName { get; set; } = string.Empty;

	[Export]
	public VariableScope Scope { get; set; }

	[Export]
	public string SharedVariableSetPath { get; set; } = string.Empty;

	public override IEntityResolver BuildEntityResolver(Graph graph)
	{
		return new EntityVariableResolver(new StringKey(VariableName), Scope);
	}
}
