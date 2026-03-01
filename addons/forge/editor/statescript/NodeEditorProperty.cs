// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Statescript;

/// <summary>
/// Base class for all Statescript property resolver editor controls. Extends <see cref="PanelContainer"/> so it can be
/// added directly to the graph node UI and participates in the Godot scene tree (lifecycle, disposal, etc.).
/// </summary>
[Tool]
internal abstract partial class NodeEditorProperty : PanelContainer
{
	/// <summary>
	/// Gets the display name shown in the resolver type dropdown (e.g., "Variable", "Constant", "Attribute").
	/// </summary>
	public abstract string DisplayName { get; }

	/// <summary>
	/// Gets the CLR type that this resolver produces.
	/// </summary>
	public abstract Type ValueType { get; }

	/// <summary>
	/// Gets the resolver type identifier string used for matching against serialized resources.
	/// </summary>
	public abstract string ResolverTypeId { get; }

	/// <summary>
	/// Checks whether this resolver is compatible with the given expected type.
	/// </summary>
	/// <param name="expectedType">The type expected by the node's input property.</param>
	/// <returns><see langword="true"/> if this resolver can provide a value of the expected type.</returns>
	public abstract bool IsCompatibleWith(Type expectedType);

	/// <summary>
	/// Initializes the resolver editor UI. Called once after the control is created.
	/// </summary>
	/// <param name="graph">The current graph resource (for accessing variables, etc.).</param>
	/// <param name="property">The existing property binding to restore state from, or null for a new binding.</param>
	/// <param name="expectedType">The type expected by the node's input property.</param>
	/// <param name="onChanged">Callback invoked when the resolver configuration changes.</param>
	/// <param name="isArray">Whether the input expects an array of values.</param>
	public abstract void Setup(
		StatescriptGraph graph,
		StatescriptNodeProperty? property,
		Type expectedType,
		Action onChanged,
		bool isArray);

	/// <summary>
	/// Writes the current resolver configuration to the given property binding resource.
	/// </summary>
	/// <param name="property">The property binding to write to.</param>
	public abstract void SaveTo(StatescriptNodeProperty property);

	/// <summary>
	/// Raised when the editor's layout size has changed (e.g. nested resolver swap, foldable toggle)
	/// so that the owning <see cref="GraphNode"/> can call <see cref="Control.ResetSize"/>.
	/// </summary>
	public event Action? LayoutSizeChanged;

	/// <summary>
	/// Notifies listeners that the editor layout has changed size.
	/// </summary>
	protected void RaiseLayoutSizeChanged()
	{
		LayoutSizeChanged?.Invoke();
	}
}
#endif
