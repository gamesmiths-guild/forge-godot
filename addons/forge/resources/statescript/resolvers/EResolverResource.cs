// Copyright © Gamesmiths Guild.

using System;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;

namespace Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;

[Tool]
[GlobalClass]
public partial class EResolverResource : TypedConstantResolverResourceBase
{
	public override string ResolverTypeId => "E";

	protected override string PropertyNamePrefix => "__e";

	protected override IPropertyResolver CreateResolver(Type valueType)
	{
		return new EResolver(valueType);
	}
}
