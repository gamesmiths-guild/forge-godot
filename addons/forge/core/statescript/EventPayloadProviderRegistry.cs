// Copyright © Gamesmiths Guild.

using System;
using System.Collections.Generic;
using Gamesmiths.Forge.Statescript.Providers;

namespace Gamesmiths.Forge.Godot.Core.Statescript;

/// <summary>
/// Registry of <see cref="IEventPayloadProvider"/> implementations, so new event payload providers can be selected on
/// the event nodes without modifying the plugin or editor.
/// </summary>
/// <remarks>
/// Discovery, caching and identifier resolution are shared with the other provider registries through
/// <see cref="ProviderCatalog{TProvider}"/>; see it for the discovery rules and the stateless-provider requirement.
/// </remarks>
public static class EventPayloadProviderRegistry
{
	private static readonly ProviderCatalog<IEventPayloadProvider> _catalog = new();

	/// <summary>
	/// Gets all registered event payload providers.
	/// </summary>
	public static IReadOnlyList<ProviderEntry<IEventPayloadProvider>> All => _catalog.All;

	/// <summary>
	/// Gets the stable identifier stored in resources for the given provider type.
	/// </summary>
	/// <param name="type">The provider type.</param>
	/// <returns>The provider identifier (its full name, or simple name when unavailable).</returns>
	public static string GetIdentifier(Type type)
	{
		return ProviderIdentifiers.For(type);
	}

	/// <summary>
	/// Tries to get a provider by the identifier stored in a resource.
	/// </summary>
	/// <param name="identifier">The provider identifier (full name or simple name).</param>
	/// <param name="provider">The matching provider when found.</param>
	/// <returns><see langword="true"/> when a provider is registered for the identifier.</returns>
	public static bool TryGet(string identifier, out IEventPayloadProvider provider)
	{
		return _catalog.TryGet(identifier, out provider);
	}

	/// <summary>
	/// Resolves a stored identifier to the canonical identifier of a registered provider, tolerating values stored as a
	/// simple type name instead of the full name.
	/// </summary>
	/// <param name="identifier">The stored identifier.</param>
	/// <returns>The canonical identifier when a match is found; otherwise the original value.</returns>
	public static string ResolveIdentifier(string identifier)
	{
		return _catalog.ResolveIdentifier(identifier);
	}
}
