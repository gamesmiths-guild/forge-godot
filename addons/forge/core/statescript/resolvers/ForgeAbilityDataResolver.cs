// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Abilities;
using Gamesmiths.Forge.Godot.Resources.Abilities;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;

namespace Gamesmiths.Forge.Godot.Core.Statescript.Resolvers;

/// <summary>
/// Lazily materializes an <see cref="AbilityData"/> from a <see cref="ForgeAbilityData"/> resource. Materialization
/// happens on first resolve so editor-time graph builds never touch the runtime managers.
/// </summary>
/// <param name="abilityResource">The ability data resource to materialize.</param>
internal sealed class ForgeAbilityDataResolver(ForgeAbilityData abilityResource) : ObjectResolver<AbilityData>
{
	private readonly ForgeAbilityData _abilityResource = abilityResource;

	private AbilityData? _abilityData;

	public override AbilityData Resolve(GraphContext graphContext)
	{
		_abilityData ??= _abilityResource.GetAbilityData();
		return _abilityData.Value;
	}
}
