// Copyright © Gamesmiths Guild.

namespace Gamesmiths.Forge.Godot.Core.Statescript;

/// <summary>
/// Describes a provider discovered by a <see cref="ProviderCatalog{TProvider}"/>, for editor display and runtime
/// lookup.
/// </summary>
/// <typeparam name="TProvider">The provider interface being catalogued.</typeparam>
/// <param name="Identifier">The stable identifier stored in resources.</param>
/// <param name="DisplayName">The human-readable name shown in the editor dropdown.</param>
/// <param name="Provider">The cached provider instance.</param>
public sealed record ProviderEntry<TProvider>(string Identifier, string DisplayName, TProvider Provider);
