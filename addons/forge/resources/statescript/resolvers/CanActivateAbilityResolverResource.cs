// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Abilities;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;

using ForgeNode = Gamesmiths.Forge.Statescript.Node;

namespace Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;

/// <summary>
/// Resolver resource that checks whether an ability can currently activate (cooldowns, costs, tag requirements).
/// Defaults to the ability driving the graph; set an ability source to inspect a different ability.
/// </summary>
[Tool]
[GlobalClass]
public partial class CanActivateAbilityResolverResource : StatescriptResolverResource
{
	/// <inheritdoc/>
	public override string ResolverTypeId => "CanActivateAbility";

	/// <summary>
	/// Gets or sets the optional entity used as the activation target for target tag requirement checks.
	/// </summary>
	[Export]
	public StatescriptResolverResource? Target { get; set; }

	/// <summary>
	/// Gets or sets the optional nested resolver providing the ability handle to inspect.
	/// </summary>
	[Export]
	public StatescriptResolverResource? Ability { get; set; }

	/// <inheritdoc/>
	public override void BindInput(Graph graph, ForgeNode runtimeNode, string nodeId, byte index)
	{
		DefineAndBindInputProperty(graph, runtimeNode, $"__canactivate_{nodeId}_{index}", index, BuildResolver(graph));
	}

	/// <inheritdoc/>
	public override IPropertyResolver BuildResolver(Graph graph)
	{
		IObjectResolver<AbilityHandle>? handleResolver = AbilityResolverResourceUtilities.BuildHandleResolver(
			Ability,
			graph);

		return new CanActivateAbilityResolver(EntityOperand.Build(Target, graph), handleResolver);
	}
}
