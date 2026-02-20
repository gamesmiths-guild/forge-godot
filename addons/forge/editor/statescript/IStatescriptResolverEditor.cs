// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Statescript;

/// <summary>
/// Interface for editor-side property resolver UI. Each resolver type provides a way to configure how a node input
/// property gets its value at runtime.
/// </summary>
internal interface IStatescriptResolverEditor
{
	/// <summary>
	/// Gets the display name of this resolver type (e.g., "Variable", "Constant", "Attribute").
	/// </summary>
	string DisplayName { get; }

	/// <summary>
	/// Gets the CLR type that this resolver produces.
	/// </summary>
	Type ValueType { get; }

	/// <summary>
	/// Gets the resolver type identifier string used for serialization.
	/// </summary>
	string ResolverTypeId { get; }

	/// <summary>
	/// Checks whether this resolver is compatible with the given expected type.
	/// </summary>
	/// <param name="expectedType">The type expected by the node's input property.</param>
	/// <returns><see langword="true"/> if this resolver can provide a value of the expected type.</returns>
	bool IsCompatibleWith(Type expectedType);

	/// <summary>
	/// Creates the editor UI for configuring this resolver's data. The returned control is added inside the
	/// graph node below the resolver dropdown.
	/// </summary>
	/// <param name="graph">The current graph resource (for accessing variables, etc.).</param>
	/// <param name="property">The existing property binding to restore state from, or null for a new binding.</param>
	/// <param name="expectedType">The type expected by the node's input property.</param>
	/// <param name="onChanged">Callback invoked when the resolver configuration changes.</param>
	/// <returns>A control to display in the node, or null if no additional UI is needed.</returns>
	Control? CreateEditorUI(
		StatescriptGraph graph,
		StatescriptNodeProperty? property,
		Type expectedType,
		Action onChanged);

	/// <summary>
	/// Writes the current resolver configuration to the given property binding resource.
	/// </summary>
	/// <param name="property">The property binding to write to.</param>
	void SaveTo(StatescriptNodeProperty property);
}
#endif
