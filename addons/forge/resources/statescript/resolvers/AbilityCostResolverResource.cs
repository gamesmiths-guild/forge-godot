// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Abilities;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;

using ForgeNode = Gamesmiths.Forge.Statescript.Node;

namespace Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;

/// <summary>
/// Resolver resource that reads the evaluated cost of an ability for a specific attribute. A mana cost of 5 resolves
/// as -5. Defaults to the ability driving the graph; set an ability source to inspect a different ability.
/// </summary>
[Tool]
[GlobalClass]
public partial class AbilityCostResolverResource : StatescriptResolverResource
{
	/// <inheritdoc/>
	public override string ResolverTypeId => "AbilityCost";

	/// <summary>
	/// Gets or sets the attribute set class name.
	/// </summary>
	[Export]
	public string AttributeSetClass { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the attribute name within the attribute set.
	/// </summary>
	[Export]
	public string AttributeName { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the optional nested resolver providing the ability handle to inspect.
	/// </summary>
	[Export]
	public StatescriptResolverResource? Ability { get; set; }

	/// <inheritdoc/>
	public override void BindInput(Graph graph, ForgeNode runtimeNode, string nodeId, byte index)
	{
		DefineAndBindInputProperty(graph, runtimeNode, $"__abilitycost_{nodeId}_{index}", index, BuildResolver(graph));
	}

	/// <inheritdoc/>
	public override IPropertyResolver BuildResolver(Graph graph)
	{
		if (string.IsNullOrEmpty(AttributeSetClass) || string.IsNullOrEmpty(AttributeName))
		{
			GD.PushError("Statescript: Ability Cost resolver requires an attribute.");
			return new VariantResolver(default, typeof(int));
		}

		IObjectResolver<AbilityHandle>? handleResolver = AbilityResolverResourceUtilities.BuildHandleResolver(
			Ability,
			graph);

		return new AbilityCostResolver(new StringKey($"{AttributeSetClass}.{AttributeName}"), handleResolver);
	}
}
