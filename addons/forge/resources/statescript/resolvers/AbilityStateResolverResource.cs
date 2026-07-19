// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Abilities;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;

using ForgeNode = Gamesmiths.Forge.Statescript.Node;

namespace Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;

/// <summary>
/// Resolver resource that reads a state flag (is active, is inhibited, is valid) from an ability. Defaults to the
/// ability driving the graph; set an ability source to inspect a different ability.
/// </summary>
[Tool]
[GlobalClass]
public partial class AbilityStateResolverResource : StatescriptResolverResource
{
	/// <inheritdoc/>
	public override string ResolverTypeId => "AbilityState";

	/// <summary>
	/// Gets or sets which state flag to read.
	/// </summary>
	[Export]
	public AbilityStateType StateType { get; set; }

	/// <summary>
	/// Gets or sets the optional nested resolver providing the ability handle to inspect.
	/// </summary>
	[Export]
	public StatescriptResolverResource? Ability { get; set; }

	/// <inheritdoc/>
	public override void BindInput(Graph graph, ForgeNode runtimeNode, string nodeId, byte index)
	{
		DefineAndBindInputProperty(
			graph,
			runtimeNode,
			$"__abilitystate_{nodeId}_{index}",
			index,
			BuildResolver(graph));
	}

	/// <inheritdoc/>
	public override IPropertyResolver BuildResolver(Graph graph)
	{
		IObjectResolver<AbilityHandle>? handleResolver = AbilityResolverResourceUtilities.BuildHandleResolver(
			Ability,
			graph);

		return new AbilityStateResolver(StateType, handleResolver);
	}
}
