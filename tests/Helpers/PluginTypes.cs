// Copyright © Gamesmiths Guild.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Gamesmiths.Forge.Godot.Tests.Helpers;

/// <summary>
/// Reflection over the plugin assembly, for the conformance suites that assert a property across every type in a
/// family rather than hand-writing one test per type.
/// </summary>
/// <remarks>
/// Tests build into the plugin's own assembly, so <see cref="Assembly.GetExecutingAssembly"/> is the assembly under
/// test - the same one <c>StatescriptResolverRegistry</c> scans to populate the editor's dropdowns.
/// </remarks>
internal static class PluginTypes
{
	/// <summary>
	/// Gets every type in the plugin assembly, tolerating types whose dependencies fail to load.
	/// </summary>
	public static IReadOnlyList<Type> All { get; } = LoadAll();

	/// <summary>
	/// Gets the concrete, instantiable subclasses of <paramref name="baseType"/>, excluding the base itself.
	/// </summary>
	/// <param name="baseType">The base type to search for.</param>
	/// <returns>The concrete subclasses, ordered by name so test output is stable.</returns>
	public static IEnumerable<Type> ConcreteSubclassesOf(Type baseType)
	{
		return All
			.Where(type => type.IsSubclassOf(baseType) && !type.IsAbstract && !type.IsGenericTypeDefinition)
			.OrderBy(type => type.FullName, StringComparer.Ordinal);
	}

	/// <summary>
	/// Resolves a type by its full name within the plugin assembly.
	/// </summary>
	/// <param name="fullName">The full name of the type.</param>
	/// <returns>The resolved type.</returns>
	/// <remarks>
	/// Data-driven cases carry type names rather than <see cref="Type"/> instances: the name is what makes the test
	/// case readable in a report, and it keeps the data source free of anything that would need the Godot runtime.
	/// </remarks>
	public static Type Resolve(string fullName)
	{
		return All.FirstOrDefault(type => type.FullName == fullName)
			?? throw new InvalidOperationException($"No type named '{fullName}' in the plugin assembly.");
	}

	private static Type[] LoadAll()
	{
		try
		{
			return Assembly.GetExecutingAssembly().GetTypes();
		}
		catch (ReflectionTypeLoadException ex)
		{
			return [.. ex.Types.Where(type => type is not null).Select(type => type!)];
		}
	}
}
