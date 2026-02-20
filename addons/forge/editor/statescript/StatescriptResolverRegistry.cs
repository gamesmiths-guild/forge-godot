// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using System.Collections.Generic;
using Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers;

namespace Gamesmiths.Forge.Godot.Editor.Statescript;

/// <summary>
/// Registry of available <see cref="IStatescriptResolverEditor"/> implementations. New resolver editors are registered
/// here and automatically become available in node input property dropdowns.
/// </summary>
internal static class StatescriptResolverRegistry
{
	private static readonly List<Func<IStatescriptResolverEditor>> _factories = [];

	static StatescriptResolverRegistry()
	{
		// Register built-in resolvers.
		Register(() => new VariableResolverEditor());
		Register(() => new VariantResolverEditor());
	}

	/// <summary>
	/// Registers a resolver editor factory. The factory is called to create a new instance each time the resolver is
	/// used on a node property.
	/// </summary>
	/// <param name="factory">A factory function that creates a new resolver editor instance.</param>
	public static void Register(Func<IStatescriptResolverEditor> factory)
	{
		_factories.Add(factory);
	}

	/// <summary>
	/// Gets all resolver editors that are compatible with the given expected type.
	/// </summary>
	/// <param name="expectedType">The type expected by the node input property.</param>
	/// <returns>A list of compatible resolver editor instances.</returns>
	public static List<IStatescriptResolverEditor> GetCompatibleResolvers(Type expectedType)
	{
		var result = new List<IStatescriptResolverEditor>();
		foreach (Func<IStatescriptResolverEditor> factory in _factories)
		{
			IStatescriptResolverEditor resolver = factory();
			if (resolver.IsCompatibleWith(expectedType))
			{
				result.Add(resolver);
			}
		}

		return result;
	}

	/// <summary>
	/// Creates a resolver editor instance by its type identifier.
	/// </summary>
	/// <param name="resolverTypeId">The resolver type identifier string.</param>
	/// <returns>The resolver editor instance, or null if not found.</returns>
	public static IStatescriptResolverEditor? CreateByTypeId(string resolverTypeId)
	{
		foreach (Func<IStatescriptResolverEditor> factory in _factories)
		{
			IStatescriptResolverEditor resolver = factory();
			if (resolver.ResolverTypeId == resolverTypeId)
			{
				return resolver;
			}
		}

		return null;
	}
}
#endif
