// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Effects;
using Gamesmiths.Forge.Godot.Resources;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;

namespace Gamesmiths.Forge.Godot.Core.Statescript.Resolvers;

/// <summary>
/// Lazily resolves a <see cref="ForgeEffectData"/> resource into runtime <see cref="EffectData"/>.
/// </summary>
/// <param name="effectResource">The <see cref="ForgeEffectData"/> resource to resolve.</param>
internal sealed class ForgeEffectDataResolver(ForgeEffectData effectResource) : ObjectResolver<EffectData>
{
	private readonly ForgeEffectData _effectResource = effectResource;

	public override EffectData Resolve(GraphContext graphContext)
	{
		return _effectResource.GetEffectData();
	}
}
