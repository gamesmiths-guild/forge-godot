// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using System.Collections.Generic;
using Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers;

namespace Gamesmiths.Forge.Godot.Editor.Statescript;

/// <summary>
/// Registry of available <see cref="NodeEditorProperty"/> implementations. New resolver editors are registered here and
/// automatically become available in node input property dropdowns.
/// </summary>
internal static class StatescriptResolverRegistry
{
	private static readonly List<Func<NodeEditorProperty>> _factories = [];

	static StatescriptResolverRegistry()
	{
		// Register built-in resolvers.
		Register(() => new VariantResolverEditor());
		Register(() => new VariableResolverEditor());
		Register(() => new AttributeResolverEditor());
		Register(() => new TagResolverEditor());
		Register(() => new ComparisonResolverEditor());
	}

	/// <summary>
	/// Registers a resolver editor factory.
	/// </summary>
	/// <param name="factory">A factory function that creates a new resolver editor instance.</param>
	public static void Register(Func<NodeEditorProperty> factory)
	{
		_factories.Add(factory);
	}

	/// <summary>
	/// Gets factory functions for all resolver editors compatible with the given expected type.
	/// </summary>
	/// <param name="expectedType">The type expected by the node input property.</param>
	/// <returns>A list of compatible resolver editor factories.</returns>
	public static List<Func<NodeEditorProperty>> GetCompatibleFactories(Type expectedType)
	{
		var result = new List<Func<NodeEditorProperty>>();

		foreach (Func<NodeEditorProperty> factory in _factories)
		{
			// Create a temporary instance to check compatibility, then dispose it.
			using NodeEditorProperty temp = factory();

			if (temp.IsCompatibleWith(expectedType))
			{
				result.Add(factory);
			}
		}

		return result;
	}
}
#endif
