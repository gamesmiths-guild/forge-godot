// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Gamesmiths.Forge.Godot.Editor.Statescript.NodeEditors;

namespace Gamesmiths.Forge.Godot.Editor.Statescript;

/// <summary>
/// Registry of <see cref="CustomNodeEditor"/> implementations. When a custom editor is registered for a node's
/// <c>RuntimeTypeName</c>, it overrides the default property rendering in <see cref="StatescriptGraphNode"/>.
/// </summary>
/// <remarks>
/// Follows the same factory-based pattern as <see cref="StatescriptResolverRegistry"/>. Each graph node gets its own
/// <see cref="CustomNodeEditor"/> instance, so subclasses can freely store per-node state.
/// </remarks>
internal static class CustomNodeEditorRegistry
{
	private static readonly Dictionary<string, Func<CustomNodeEditor>> _factories = [];

	static CustomNodeEditorRegistry()
	{
		Register(() => new SetVariableNodeEditor());
	}

	/// <summary>
	/// Registers a custom node editor factory. If a factory for the same
	/// <see cref="CustomNodeEditor.HandledRuntimeTypeName"/> is already registered, it is replaced.
	/// </summary>
	/// <param name="factory">A factory function that creates a new custom node editor instance.</param>
	public static void Register(Func<CustomNodeEditor> factory)
	{
		using CustomNodeEditor temp = factory();
		_factories[temp.HandledRuntimeTypeName] = factory;
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
