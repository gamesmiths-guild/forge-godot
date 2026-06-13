// Copyright © Gamesmiths Guild.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Gamesmiths.Forge.Statescript;

namespace Gamesmiths.Forge.Godot.Core.Statescript;

/// <summary>
/// Registry of <see cref="IEffectContextDataProvider"/> implementations. All non-abstract providers with a
/// parameterless constructor in the project assembly are discovered automatically, including ones defined in game code,
/// so new effect context-data types can be selected on <c>ApplyEffectNode</c>/<c>EffectNode</c> without modifying the
/// plugin or editor.
/// </summary>
/// <remarks>
/// Provider instances are cached and shared. Implementations must be stateless:
/// <see cref="IEffectContextDataProvider"/> builds a fresh value from the supplied <see cref="GraphContext"/> on every
/// call, so providers should not hold mutable per-application state.
/// </remarks>
public static class EffectContextDataProviderRegistry
{
	private static readonly List<ProviderEntry> _all = [];
	private static readonly Dictionary<string, IEffectContextDataProvider> _byIdentifier = [];

	/// <summary>
	/// Gets all registered context-data providers.
	/// </summary>
	public static IReadOnlyList<ProviderEntry> All => _all;

	static EffectContextDataProviderRegistry()
	{
		foreach (Type type in GetLoadableTypes(Assembly.GetExecutingAssembly()))
		{
			if (type.IsAbstract
				|| type.IsInterface
				|| !typeof(IEffectContextDataProvider).IsAssignableFrom(type)
				|| type.GetConstructor(Type.EmptyTypes) is null)
			{
				continue;
			}

			var provider = (IEffectContextDataProvider)Activator.CreateInstance(type)!;
			string identifier = GetIdentifier(type);
			_all.Add(new ProviderEntry(identifier, type.Name, provider));
			_byIdentifier[identifier] = provider;
		}
	}

	/// <summary>
	/// Gets the stable identifier stored in resources for the given provider type.
	/// </summary>
	/// <param name="type">The provider type.</param>
	/// <returns>The provider identifier (its full name, or simple name when unavailable).</returns>
	public static string GetIdentifier(Type type)
	{
		return type.FullName ?? type.Name;
	}

	/// <summary>
	/// Tries to get a provider by the identifier stored in a resource.
	/// </summary>
	/// <param name="identifier">The provider identifier (full name or simple name).</param>
	/// <param name="provider">The matching provider when found.</param>
	/// <returns><see langword="true"/> when a provider is registered for the identifier.</returns>
	public static bool TryGet(string identifier, out IEffectContextDataProvider provider)
	{
		string resolved = ResolveIdentifier(identifier);
		return _byIdentifier.TryGetValue(resolved, out provider!);
	}

	/// <summary>
	/// Resolves a stored identifier to the canonical identifier of a registered provider, tolerating values stored as a
	/// simple type name instead of the full name.
	/// </summary>
	/// <param name="identifier">The stored identifier.</param>
	/// <returns>The canonical identifier when a match is found; otherwise the original value.</returns>
	public static string ResolveIdentifier(string identifier)
	{
		if (string.IsNullOrEmpty(identifier))
		{
			return string.Empty;
		}

		if (_byIdentifier.ContainsKey(identifier))
		{
			return identifier;
		}

		foreach (ProviderEntry entry in _all)
		{
			if (entry.DisplayName == identifier)
			{
				return entry.Identifier;
			}
		}

		return identifier;
	}

	private static Type[] GetLoadableTypes(Assembly assembly)
	{
		try
		{
			return assembly.GetTypes();
		}
		catch (ReflectionTypeLoadException ex)
		{
			return [.. ex.Types.Where(type => type is not null)!];
		}
	}

	/// <summary>
	/// Describes a discovered context-data provider for editor display and runtime lookup.
	/// </summary>
	/// <param name="Identifier">The stable identifier stored in resources.</param>
	/// <param name="DisplayName">The human-readable name shown in the editor dropdown.</param>
	/// <param name="Provider">The cached provider instance.</param>
	public sealed record ProviderEntry(string Identifier, string DisplayName, IEffectContextDataProvider Provider);
}
