// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;

namespace Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;

[Tool]
[GlobalClass]
public partial class QuaternionFromAxisAngleResolverResource : BinaryNestedResolverResourceBase
{
	public override string ResolverTypeId => "QuaternionFromAxisAngle";

	protected override string PropertyNamePrefix => "__quataxisangle";

	protected override IPropertyResolver CreateResolver(
		IPropertyResolver leftResolver,
		IPropertyResolver rightResolver,
		Graph graph)
	{
		return new QuaternionFromAxisAngleResolver(leftResolver, rightResolver);
	}
}
