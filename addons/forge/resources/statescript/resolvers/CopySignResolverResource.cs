// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;

namespace Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;

[Tool]
[GlobalClass]
public partial class CopySignResolverResource : BinaryNestedResolverResourceBase
{
	public override string ResolverTypeId => "CopySign";

	protected override string PropertyNamePrefix => "__copysign";

	protected override IPropertyResolver CreateResolver(
		IPropertyResolver leftResolver,
		IPropertyResolver rightResolver,
		Graph graph)
	{
		return new CopySignResolver(leftResolver, rightResolver);
	}
}
