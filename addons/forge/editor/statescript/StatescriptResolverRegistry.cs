// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Gamesmiths.Forge.Core;

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
		return [.. _factories.Where(factory => IsCompatibleFactory(factory, expectedType))];
	}

	public static int GetDefaultFactoryIndex(List<Func<NodeEditorProperty>> factories, Type expectedType, bool isArray)
	{
		for (int i = 0; i < factories.Count; i++)
		{
			string resolverTypeId = GetResolverTypeId(factories[i]);

			if (!isArray && expectedType == typeof(IForgeEntity) && resolverTypeId == "OwnerEntity")
			{
				return i;
			}

			if (isArray)
			{
				if (resolverTypeId == "Variant")
				{
					return i;
				}

				if (resolverTypeId == "ArrayVariable")
				{
					return i;
				}
			}
			else if (resolverTypeId == "Variant")
			{
				return i;
			}
		}

		return 0;
	}

	public static string GetDisplayName(Func<NodeEditorProperty> factory)
	{
		return UseTemporaryEditor(factory, static editor => editor.DisplayName);
	}

	public static string GetResolverTypeId(Func<NodeEditorProperty> factory)
	{
		return UseTemporaryEditor(factory, static editor => editor.ResolverTypeId);
	}

	public static bool IsCompatibleFactory(Func<NodeEditorProperty> factory, Type expectedType)
	{
		return UseTemporaryEditor(factory, editor => editor.IsCompatibleWith(expectedType));
	}

	private static TResult UseTemporaryEditor<TResult>(
		Func<NodeEditorProperty> factory,
		Func<NodeEditorProperty, TResult> selector)
	{
		NodeEditorProperty editor = factory();

		try
		{
			return selector(editor);
		}
		finally
		{
			if (global::Godot.GodotObject.IsInstanceValid(editor))
			{
				editor.Free();
			}
		}
	}
}
#endif
