// Copyright © Gamesmiths Guild.

using Gamesmiths.Forge.Core;

namespace Gamesmiths.Forge.Example;

/// <summary>
/// The payload of the event a lethal hit raises on its target: who dealt it.
/// </summary>
/// <param name="Fragger">The entity whose effect dealt the hit.</param>
public readonly record struct Frag(IForgeEntity? Fragger);
