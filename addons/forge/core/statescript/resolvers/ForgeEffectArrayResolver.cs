// Copyright © Gamesmiths Guild.

using System.Collections.Generic;
using Gamesmiths.Forge.Effects;
using Gamesmiths.Forge.Godot.Resources;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;

namespace Gamesmiths.Forge.Godot.Core.Statescript.Resolvers;

/// <summary>
/// Lazily builds an array of <see cref="Effect"/> instances from <see cref="ForgeEffectData"/> resources plus optional
/// level and ownership resolvers.
/// </summary>
/// <remarks>
/// Deferring <see cref="ForgeEffectData.GetEffectData"/> to resolve time keeps graph building (including editor-time
/// builds such as connection loop detection) from eagerly materializing effect data, which is unsafe before the runtime
/// managers are available.
/// </remarks>
/// <param name="effectResources">The <see cref="ForgeEffectData"/> resources to build the effects from.</param>
/// <param name="levelResolver">Optional resolver used for the effect level.</param>
/// <param name="ownershipResolver">Optional resolver used for the effect ownership.</param>
internal sealed class ForgeEffectArrayResolver(
	IReadOnlyList<ForgeEffectData> effectResources,
	IPropertyResolver? levelResolver = null,
	IObjectResolver<EffectOwnership>? ownershipResolver = null) : ObjectArrayResolver<Effect>
{
	private readonly IReadOnlyList<ForgeEffectData> _effectResources = effectResources;
	private readonly IPropertyResolver? _levelResolver = levelResolver;
	private readonly IObjectResolver<EffectOwnership>? _ownershipResolver = ownershipResolver;

	public override Effect[] ResolveArray(GraphContext graphContext)
	{
		var effectData = new EffectData[_effectResources.Count];

		for (int i = 0; i < _effectResources.Count; i++)
		{
			effectData[i] = _effectResources[i].GetEffectData();
		}

		return new EffectArrayFromDataResolver(effectData, _levelResolver, _ownershipResolver)
			.ResolveArray(graphContext);
	}
}
