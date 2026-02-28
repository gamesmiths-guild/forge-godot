// Copyright © Gamesmiths Guild.

#if TOOLS
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Gamesmiths.Forge.Godot.Editor.Statescript.NodeEditors;

namespace Gamesmiths.Forge.Godot.Editor.Statescript;

/// <summary>
/// Registry of <see cref="CustomNodeEditor"/> implementations. When a custom editor is registered for a node's
/// <c>RuntimeTypeName</c>, it overrides the default property rendering in <see cref="StatescriptGraphNode"/>.
/// </summary>
/// <remarks>
/// Follows the same factory-based pattern as <see cref="StatescriptResolverRegistry"/>.
/// </remarks>
internal static class CustomNodeEditorRegistry
{
	private static readonly Dictionary<string, CustomNodeEditor> _editors = [];

	static CustomNodeEditorRegistry()
	{
		Register(new SetVariableNodeEditor());
	}

	/// <summary>
	/// Registers a custom node editor. If an editor for the same <see cref="CustomNodeEditor.HandledRuntimeTypeName"/>
	/// is already registered, it is replaced.
	/// </summary>
	/// <param name="editor">The custom node editor to register.</param>
	public static void Register(CustomNodeEditor editor)
	{
		_editors[editor.HandledRuntimeTypeName] = editor;
	}

	/// <summary>
	/// Tries to find a registered custom node editor for the given runtime type name.
	/// </summary>
	/// <param name="runtimeTypeName">The runtime type name of the node.</param>
	/// <param name="editor">The matching editor, or <see langword="null"/> if none is registered.</param>
	/// <returns><see langword="true"/> if a custom editor was found.</returns>
	public static bool TryGet(string runtimeTypeName, [NotNullWhen(true)] out CustomNodeEditor? editor)
	{
		return _editors.TryGetValue(runtimeTypeName, out editor);
	}
}
#endif
