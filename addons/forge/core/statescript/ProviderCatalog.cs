// Copyright © Gamesmiths Guild.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Godot;

namespace Gamesmiths.Forge.Godot.Core.Statescript;

/// <summary>
/// Discovery and caching shared by the statescript provider registries. Every non-abstract implementation of
/// <typeparamref name="TProvider"/> with a parameterless constructor is discovered automatically across all loaded
/// assemblies, so providers defined in game code are found whether they live in the Godot project itself or in a class
/// library it references, matching how custom nodes and attribute sets are discovered.
/// </summary>
/// <typeparam name="TProvider">The provider interface to discover implementations of.</typeparam>
/// <remarks>
/// <para>Provider instances are cached and shared, so implementations must be stateless: a provider builds a fresh
/// value from the arguments it is handed on every call and should hold no mutable per-call state.</para>
/// <para>The scan is cached and repeated only when the set of loaded assemblies has grown, so an assembly loaded after
/// the first lookup still contributes its providers without paying for a scan on every access.</para>
/// </remarks>
public sealed class ProviderCatalog<TProvider>
	where TProvider : class
{
	private List<ProviderEntry<TProvider>> _all = [];
	private Dictionary<string, TProvider> _byIdentifier = [];
	private int _scannedAssemblyCount;

	/// <summary>
	/// Gets all discovered providers.
	/// </summary>
	public IReadOnlyList<ProviderEntry<TProvider>> All
	{
		get
		{
			EnsureScanned();
			return _all;
		}
	}

	/// <summary>
	/// Tries to get a provider by the identifier stored in a resource.
	/// </summary>
	/// <param name="identifier">The provider identifier (full name or simple name).</param>
	/// <param name="provider">The matching provider when found.</param>
	/// <returns><see langword="true"/> when a provider is registered for the identifier.</returns>
	public bool TryGet(string identifier, out TProvider provider)
	{
		return _byIdentifier.TryGetValue(ResolveIdentifier(identifier), out provider!);
	}

	/// <summary>
	/// Resolves a stored identifier to the canonical identifier of a discovered provider, tolerating values stored as a
	/// simple type name instead of the full name.
	/// </summary>
	/// <param name="identifier">The stored identifier.</param>
	/// <returns>The canonical identifier when a match is found; otherwise the original value.</returns>
	public string ResolveIdentifier(string identifier)
	{
		if (string.IsNullOrEmpty(identifier))
		{
			return string.Empty;
		}

		EnsureScanned();

		if (_byIdentifier.ContainsKey(identifier))
		{
			return identifier;
		}

		foreach (ProviderEntry<TProvider> entry in _all)
		{
			if (entry.DisplayName == identifier)
			{
				return entry.Identifier;
			}
		}

		return identifier;
	}

	private static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
	{
		try
		{
			return assembly.GetTypes();
		}
		catch (ReflectionTypeLoadException ex)
		{
			return ex.Types.Where(type => type is not null)!;
		}
	}

	private static bool IsCandidate(Type type)
	{
		return !type.IsAbstract
			&& !type.IsInterface
			&& !type.IsGenericTypeDefinition
			&& typeof(TProvider).IsAssignableFrom(type)
			&& type.GetConstructor(Type.EmptyTypes) is not null;
	}

	private static bool TryCreate(Type type, out TProvider? provider)
	{
		try
		{
			provider = (TProvider)Activator.CreateInstance(type)!;
			return true;
		}
		catch (TargetInvocationException ex)
		{
			// One provider throwing in its constructor must not take the whole catalog down with it.
			GD.PushError(
				$"Statescript: Provider '{type.FullName}' threw during construction and was skipped: " +
				$"{ex.InnerException?.Message ?? ex.Message}");
		}
		catch (MemberAccessException)
		{
			GD.PushError(
				$"Statescript: Provider '{type.FullName}' was skipped because its constructor is not accessible.");
		}

		provider = null;
		return false;
	}

	private void EnsureScanned()
	{
		Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();

		// Assemblies are never unloaded in the editor or a running game, so a growing count is the only way the
		// discoverable set can change.
		if (assemblies.Length == _scannedAssemblyCount)
		{
			return;
		}

		var all = new List<ProviderEntry<TProvider>>();
		var byIdentifier = new Dictionary<string, TProvider>();

		foreach (Assembly assembly in assemblies)
		{
			foreach (Type type in GetLoadableTypes(assembly))
			{
				if (!IsCandidate(type) || !TryCreate(type, out TProvider? provider))
				{
					continue;
				}

				string identifier = ProviderIdentifiers.For(type);
				all.Add(new ProviderEntry<TProvider>(identifier, type.Name, provider!));
				byIdentifier[identifier] = provider!;
			}
		}

		// Swap in complete collections, then record the scan, so a reader never observes a half-built catalog.
		_all = all;
		_byIdentifier = byIdentifier;
		_scannedAssemblyCount = assemblies.Length;
	}
}
