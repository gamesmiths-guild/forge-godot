// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Godot.Core;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;

namespace Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;

[Tool]
[GlobalClass]
public partial class RandomResolverResource : BinaryNestedResolverResourceBase
{
	[Export]
	public StatescriptVariableType ValueType { get; set; } = StatescriptVariableType.Int;

	[Export]
	public bool MinFolded { get; set; }

	[Export]
	public bool MaxFolded { get; set; }

	public override string ResolverTypeId => "Random";

	protected override string PropertyNamePrefix => "__random";

	protected override IPropertyResolver CreateResolver(
		IPropertyResolver leftResolver,
		IPropertyResolver rightResolver,
		Graph graph)
	{
		using var random = new ForgeRandom();
		return new RandomResolver(random, leftResolver, rightResolver);
	}
}
