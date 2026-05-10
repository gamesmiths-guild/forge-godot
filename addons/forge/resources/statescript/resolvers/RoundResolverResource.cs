// Copyright © Gamesmiths Guild.

using System;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;

namespace Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;

[Tool]
[GlobalClass]
public partial class RoundResolverResource : UnaryNestedResolverResourceBase
{
	[Export]
	public int Digits { get; set; }

	[Export]
	public MidpointRounding Mode { get; set; } = MidpointRounding.ToEven;

	public override string ResolverTypeId => "Round";

	protected override string PropertyNamePrefix => "__round";

	protected override IPropertyResolver CreateResolver(IPropertyResolver operandResolver, Graph graph)
	{
		operandResolver = PromoteIntegralResolverToFloatingPoint(
			operandResolver,
			GetPreferredFloatingPointType(operandResolver));
		return new RoundResolver(operandResolver, Digits, Mode);
	}
}
