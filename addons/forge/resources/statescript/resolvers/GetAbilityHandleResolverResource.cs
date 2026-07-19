// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Abilities;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Godot.Core.Statescript.Resolvers;
using Gamesmiths.Forge.Godot.Resources.Abilities;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;

using ForgeNode = Gamesmiths.Forge.Statescript.Node;

namespace Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;

/// <summary>
/// Resolver resource that looks up the <see cref="AbilityHandle"/> of a granted ability on an entity — the entry
/// point for cross-ability queries like "the cooldown of my other ability".
/// </summary>
/// <remarks>
/// The ability data is materialized lazily on first resolve, so editor-time graph builds never touch the runtime
/// managers.
/// </remarks>
[Tool]
[GlobalClass]
public partial class GetAbilityHandleResolverResource : StatescriptResolverResource
{
	/// <inheritdoc/>
	public override string ResolverTypeId => "GetAbilityHandle";

	/// <summary>
	/// Gets or sets the ability data identifying the granted ability.
	/// </summary>
	[Export]
	public ForgeAbilityData? AbilityData { get; set; }

	/// <summary>
	/// Gets or sets which entity should be inspected. Defaults to owner when omitted.
	/// </summary>
	[Export]
	public EntityResolverResourceBase? EntityResolver { get; set; }

	/// <summary>
	/// Gets or sets the optional entity used to filter the lookup by granting source. When unset, the lookup matches
	/// the ability regardless of its granting source.
	/// </summary>
	[Export]
	public EntityResolverResourceBase? SourceResolver { get; set; }

	/// <inheritdoc/>
	public override void BindInput(Graph graph, ForgeNode runtimeNode, string nodeId, byte index)
	{
		if (!TryBuildObjectResolver(graph, out IObjectResolver? objectResolver) || objectResolver is null)
		{
			return;
		}

		var propertyName = new StringKey($"__getabilityhandle_{nodeId}_{index}");
		graph.VariableDefinitions.DefineObjectProperty(propertyName, objectResolver);
		runtimeNode.BindInput(index, propertyName);
	}

	/// <inheritdoc/>
	public override bool TryBuildObjectResolver(Graph graph, out IObjectResolver? objectResolver)
	{
		objectResolver = null;

		if (AbilityData is null)
		{
			GD.PushError("Statescript: Get Ability Handle resolver requires an ability data resource.");
			return false;
		}

		ForgeAbilityData abilityDataResource = AbilityData;
		IEntityResolver? entityResolver = EntityResolver?.BuildEntityResolver(graph);
		IEntityResolver? sourceResolver = SourceResolver?.BuildEntityResolver(graph);

		objectResolver = new LazyObjectResolver<AbilityHandle>(
			() => new GetAbilityHandleResolver(abilityDataResource.GetAbilityData(), entityResolver, sourceResolver));

		return true;
	}
}
