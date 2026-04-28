// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;

namespace Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;

[Tool]
[GlobalClass]
public partial class LookAtResolverResource : TernaryNestedResolverResourceBase
{
	public override string ResolverTypeId => "LookAt";

	protected override string PropertyNamePrefix => "__lookat";

	protected override IPropertyResolver CreateResolver(
		IPropertyResolver firstResolver,
		IPropertyResolver secondResolver,
		IPropertyResolver thirdResolver,
		Graph graph)
	{
		return new LookAtResolver(firstResolver, secondResolver, thirdResolver);
	}
}
