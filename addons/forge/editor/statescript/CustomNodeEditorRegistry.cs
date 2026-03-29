// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;

namespace Gamesmiths.Forge.Godot.Editor.Statescript;

/// <summary>
/// Registry of <see cref="CustomNodeEditor"/> implementations. Custom node editors are discovered automatically via
/// reflection. Any concrete subclass of <see cref="CustomNodeEditor"/> in the executing assembly is registered and
/// overrides the default property rendering for its handled node type.
/// </summary>
internal static class CustomNodeEditorRegistry
{
	private static readonly Dictionary<string, Func<CustomNodeEditor>> _factories = [];

	static CustomNodeEditorRegistry()
	{
		Type[] allTypes = Assembly.GetExecutingAssembly().GetTypes();

		foreach (Type type in allTypes.Where(
			x => x.IsSubclassOf(typeof(CustomNodeEditor)) && !x.IsAbstract))
		{
			Type captured = type;
			using var temp = (CustomNodeEditor)Activator.CreateInstance(captured)!;
			_factories[temp.HandledRuntimeTypeName] = () => (CustomNodeEditor)Activator.CreateInstance(captured)!;
		}
	}

	/// <summary>
	/// Tries to create a new custom node editor for the given runtime type name.
	/// </summary>
	/// <param name="runtimeTypeName">The runtime type name of the node.</param>
	/// <param name="editor">The newly created editor, or <see langword="null"/> if none is registered.</param>
	/// <returns><see langword="true"/> if a custom editor was created.</returns>
	public static bool TryCreate(string runtimeTypeName, [NotNullWhen(true)] out CustomNodeEditor? editor)
	{
		if (_factories.TryGetValue(runtimeTypeName, out Func<CustomNodeEditor>? factory))
		{
			editor = factory();
			return true;
		}

		editor = null;
		return false;
	}
}
#endif
