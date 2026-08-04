// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;

namespace Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;

[Tool]
[GlobalClass]
public partial class QuaternionConcatenateResolverResource : BinaryNestedResolverResourceBase
{
	public override string ResolverTypeId => "QuaternionConcatenate";

	protected override string PropertyNamePrefix => "__quatConcat";

	protected override IPropertyResolver CreateResolver(
		IPropertyResolver leftResolver,
		IPropertyResolver rightResolver,
		Graph graph)
	{
		return new QuaternionConcatenateResolver(leftResolver, rightResolver);
	}
}
