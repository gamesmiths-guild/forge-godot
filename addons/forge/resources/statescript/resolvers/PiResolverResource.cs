// Copyright © Gamesmiths Guild.

using System;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;

namespace Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;

[Tool]
[GlobalClass]
public partial class PiResolverResource : TypedConstantResolverResourceBase
{
	public override string ResolverTypeId => "Pi";

	protected override string PropertyNamePrefix => "__pi";

	protected override IPropertyResolver CreateResolver(Type valueType)
	{
		return new PiResolver(valueType);
	}
}
