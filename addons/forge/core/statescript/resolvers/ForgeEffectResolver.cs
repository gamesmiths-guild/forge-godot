// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Effects;
using Gamesmiths.Forge.Godot.Resources;
using Gamesmiths.Forge.Statescript;
using Gamesmiths.Forge.Statescript.Properties;

namespace Gamesmiths.Forge.Godot.Core.Statescript.Resolvers;

/// <summary>
/// Lazily builds an <see cref="Effect"/> from a <see cref="ForgeEffectData"/> resource plus optional level and
/// ownership resolvers.
/// </summary>
/// <remarks>
/// Deferring <see cref="ForgeEffectData.GetEffectData"/> to resolve time keeps graph building (including editor-time
/// builds such as connection loop detection) from eagerly materializing effect data, which is unsafe before the runtime
/// managers are available.
/// </remarks>
/// <param name="effectResource">The <see cref="ForgeEffectData"/> resource to build the effect from.</param>
/// <param name="levelResolver">Optional resolver used for the effect level.</param>
/// <param name="ownershipResolver">Optional resolver used for the effect ownership.</param>
internal sealed class ForgeEffectResolver(
	ForgeEffectData effectResource,
	IPropertyResolver? levelResolver = null,
	IObjectResolver<EffectOwnership>? ownershipResolver = null) : ObjectResolver<Effect>
{
	private readonly ForgeEffectData _effectResource = effectResource;
	private readonly IPropertyResolver? _levelResolver = levelResolver;
	private readonly IObjectResolver<EffectOwnership>? _ownershipResolver = ownershipResolver;

	public override Effect Resolve(GraphContext graphContext)
	{
		return new EffectFromDataResolver(_effectResource.GetEffectData(), _levelResolver, _ownershipResolver)
			.Resolve(graphContext);
	}
}
