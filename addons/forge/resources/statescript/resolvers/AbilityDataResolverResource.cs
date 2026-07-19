// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Godot.Core.Statescript.Resolvers;
using Gamesmiths.Forge.Godot.Resources.Abilities;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;

using ForgeNode = Gamesmiths.Forge.Statescript.Node;

namespace Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;

/// <summary>
/// Resolver resource that provides an <see cref="AbilityData"/> value from a <see cref="ForgeAbilityData"/> resource,
/// for ability grant and lookup node inputs.
/// </summary>
/// <remarks>
/// The ability data is materialized lazily on first resolve, so editor-time graph builds never touch the runtime
/// managers.
/// </remarks>
[Tool]
[GlobalClass]
public partial class AbilityDataResolverResource : StatescriptResolverResource
{
	/// <inheritdoc/>
	public override string ResolverTypeId => "AbilityData";

	/// <summary>
	/// Gets or sets the ability data resource to provide.
	/// </summary>
	[Export]
	public ForgeAbilityData? AbilityData { get; set; }

	/// <inheritdoc/>
	public override void BindInput(Graph graph, ForgeNode runtimeNode, string nodeId, byte index)
	{
		if (!TryBuildObjectResolver(graph, out IObjectResolver? objectResolver) || objectResolver is null)
		{
			return;
		}

		var propertyName = new StringKey($"__abilitydata_{nodeId}_{index}");
		graph.VariableDefinitions.DefineObjectProperty(propertyName, objectResolver);
		runtimeNode.BindInput(index, propertyName);
	}

	/// <inheritdoc/>
	public override bool TryBuildObjectResolver(Graph graph, out IObjectResolver? objectResolver)
	{
		objectResolver = null;

		if (AbilityData is null)
		{
			GD.PushError("Statescript: Ability Data resolver requires an ability data resource.");
			return false;
		}

		// ForgeAbilityDataResolver already defers GetAbilityData() to its first resolve, so it is safe to build here.
		objectResolver = new ForgeAbilityDataResolver(AbilityData);

		return true;
	}
}
