// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Gamesmiths.Forge.Godot.Editor.Statescript;

/// <summary>
/// Registry of available <see cref="NodeEditorProperty"/> implementations. Resolver editors are discovered
/// automatically via reflection. Any concrete subclass of <see cref="NodeEditorProperty"/> in the executing assembly is
/// registered and becomes available in node input property dropdowns.
/// </summary>
internal static class StatescriptResolverRegistry
{
	private static readonly List<Func<NodeEditorProperty>> _factories = [];

	static StatescriptResolverRegistry()
	{
		Type[] allTypes = Assembly.GetExecutingAssembly().GetTypes();

		foreach (Type type in allTypes.Where(
			x => x.IsSubclassOf(typeof(NodeEditorProperty)) && !x.IsAbstract))
		{
			Type captured = type;
			_factories.Add(() => (NodeEditorProperty)Activator.CreateInstance(captured)!);
		}
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
