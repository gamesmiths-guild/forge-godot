// Copyright © Gamesmiths Guild.

using System.Collections.Generic;
using Gamesmiths.Forge.Effects;
using Gamesmiths.Forge.Godot.Resources;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;

namespace Gamesmiths.Forge.Godot.Core.Statescript.Resolvers;

/// <summary>
/// Lazily resolves an authored list of <see cref="ForgeEffectData"/> resources into runtime <see cref="EffectData"/>
/// values.
/// </summary>
/// <param name="effectResources">The list of <see cref="ForgeEffectData"/> resources to resolve.</param>
internal sealed class ForgeEffectDataArrayResolver(IReadOnlyList<ForgeEffectData> effectResources)
	: ObjectArrayResolver<EffectData>
{
	private readonly IReadOnlyList<ForgeEffectData> _effectResources = effectResources;

	public override EffectData[] ResolveArray(GraphContext graphContext)
	{
		var values = new EffectData[_effectResources.Count];

		for (int i = 0; i < _effectResources.Count; i++)
		{
			values[i] = _effectResources[i].GetEffectData();
		}

		return values;
	}
}
