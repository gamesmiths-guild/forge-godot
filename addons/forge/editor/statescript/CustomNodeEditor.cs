// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using System.Collections.Generic;
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Statescript;

/// <summary>
/// Base class for custom node property editors. Implementations override the default input-property / output-variable
/// sections rendered by <see cref="StatescriptGraphNode"/> for specific node types. Analogous to Godot's
/// <c>EditorInspectorPlugin</c> pattern.
/// </summary>
/// <remarks>
/// If a <see cref="CustomNodeEditor"/> is registered for a node's <c>RuntimeTypeName</c>, its
/// <see cref="BuildPropertySections"/> method is called instead of the default property rendering. The base class
/// provides helper methods that mirror the default behavior so that custom editors can reuse them selectively.
/// </remarks>
internal abstract class CustomNodeEditor
{
	private StatescriptGraphNode? _graphNode;
	private StatescriptGraph? _graph;
	private StatescriptNode? _nodeResource;
	private Dictionary<PropertySlotKey, NodeEditorProperty>? _activeResolverEditors;

	/// <summary>
	/// Gets the runtime type name this editor handles (e.g.,
	/// <c>"Gamesmiths.Forge.Statescript.Nodes.Action.SetVariableNode"</c>).
	/// </summary>
	public abstract string HandledRuntimeTypeName { get; }

	/// <summary>
	/// Builds the custom input-property and output-variable sections for the node.
	/// </summary>
	/// <param name="typeInfo">Discovered metadata about the node type.</param>
	public abstract void BuildPropertySections(StatescriptNodeDiscovery.NodeTypeInfo typeInfo);

	/// <summary>
	/// Gets the input property section color.
	/// </summary>
	protected static Color InputPropertyColor { get; } = new(0x61afefff);

	/// <summary>
	/// Gets the output variable section color.
	/// </summary>
	protected static Color OutputVariableColor { get; } = new(0xe5c07bff);

	/// <summary>
	/// Gets the active resolver editors dictionary.
	/// </summary>
	protected Dictionary<PropertySlotKey, NodeEditorProperty> ActiveResolverEditors => _activeResolverEditors!;

	/// <summary>
	/// Gets the owning graph resource.
	/// </summary>
	protected StatescriptGraph Graph => _graph!;

	/// <summary>
	/// Gets the node resource.
	/// </summary>
	protected StatescriptNode NodeResource => _nodeResource!;

	/// <summary>
	/// Stores references needed by helper methods. Called once before <see cref="BuildPropertySections"/>.
	/// </summary>
	/// <param name="graphNode">The graph node this editor is bound to.</param>
	/// <param name="graph">The graph resource this node belongs to.</param>
	/// <param name="nodeResource">The node resource being edited.</param>
	/// <param name="activeResolverEditors">A dictionary of active resolver editors.</param>
	internal void Bind(
		StatescriptGraphNode graphNode,
		StatescriptGraph graph,
		StatescriptNode nodeResource,
		Dictionary<PropertySlotKey, NodeEditorProperty> activeResolverEditors)
	{
		_graphNode = graphNode;
		_graph = graph;
		_nodeResource = nodeResource;
		_activeResolverEditors = activeResolverEditors;
	}

	/// <summary>
	/// Clears all children from a container control.
	/// </summary>
	/// <param name="container">The container control to clear.</param>
	protected static void ClearContainer(Control container)
	{
		foreach (Node child in container.GetChildren())
		{
			container.RemoveChild(child);
			child.QueueFree();
		}
	}

	/// <summary>
	/// Adds a foldable section divider to the graph node.
	/// </summary>
	/// <param name="sectionTitle">Title displayed on the divider.</param>
	/// <param name="color">Color of the divider.</param>
	/// <param name="foldKey">Key used to persist the fold state.</param>
	/// <param name="folded">Initial fold state.</param>
	protected FoldableContainer AddPropertySectionDivider(
		string sectionTitle,
		Color color,
		string foldKey,
		bool folded)
	{
		return _graphNode!.AddPropertySectionDividerInternal(sectionTitle, color, foldKey, folded);
	}

	/// <summary>
	/// Renders a standard input-property row (resolver dropdown + editor UI).
	/// </summary>
	/// <param name="propInfo">Metadata about the input property.</param>
	/// <param name="index">Index of the input property.</param>
	/// <param name="container">Container to add the input property row to.</param>
	protected void AddInputPropertyRow(
		StatescriptNodeDiscovery.InputPropertyInfo propInfo,
		int index,
		Control container)
	{
		_graphNode!.AddInputPropertyRowInternal(propInfo, index, container);
	}

	/// <summary>
	/// Renders a standard output-variable row (variable dropdown).
	/// </summary>
	/// <param name="varInfo">Metadata about the output variable.</param>
	/// <param name="index">Index of the output variable.</param>
	/// <param name="container">Container to add the output variable row to.</param>
	protected void AddOutputVariableRow(
		StatescriptNodeDiscovery.OutputVariableInfo varInfo,
		int index,
		FoldableContainer container)
	{
		_graphNode!.AddOutputVariableRowInternal(varInfo, index, container);
	}

	/// <summary>
	/// Gets the persisted fold state for a given key.
	/// </summary>
	/// <param name="key">The key used to persist the fold state.</param>
	protected bool GetFoldState(string key)
	{
		return _graphNode!.GetFoldStateInternal(key);
	}

	/// <summary>
	/// Finds an existing property binding by direction and index.
	/// </summary>
	/// <param name="direction">The direction of the property (input or output).</param>
	/// <param name="propertyIndex">The index of the property.</param>
	protected StatescriptNodeProperty? FindBinding(
		StatescriptPropertyDirection direction,
		int propertyIndex)
	{
		return _graphNode!.FindBindingInternal(direction, propertyIndex);
	}

	/// <summary>
	/// Ensures a property binding exists for the given direction and index, creating one if needed.
	/// </summary>
	/// <param name="direction">The direction of the property (input or output).</param>
	/// <param name="propertyIndex">The index of the property.</param>
	protected StatescriptNodeProperty EnsureBinding(
		StatescriptPropertyDirection direction,
		int propertyIndex)
	{
		return _graphNode!.EnsureBindingInternal(direction, propertyIndex);
	}

	/// <summary>
	/// Removes a property binding by direction and index.
	/// </summary>
	/// <param name="direction">The direction of the property (input or output).</param>
	/// <param name="propertyIndex">The index of the property.</param>
	protected void RemoveBinding(
		StatescriptPropertyDirection direction,
		int propertyIndex)
	{
		_graphNode!.RemoveBindingInternal(direction, propertyIndex);
	}

	/// <summary>
	/// Shows a resolver editor inside the given container.
	/// </summary>
	/// <param name="factory">A factory function to create the resolver editor.</param>
	/// <param name="existingBinding">The existing binding, if any.</param>
	/// <param name="expectedType">The expected type for the resolver editor.</param>
	/// <param name="container">The container to add the resolver editor to.</param>
	/// <param name="direction">The direction of the property (input or output).</param>
	/// <param name="propertyIndex">The index of the property.</param>
	/// <param name="isArray">Whether the input expects an array of values.</param>
	protected void ShowResolverEditorUI(
		Func<NodeEditorProperty> factory,
		StatescriptNodeProperty? existingBinding,
		Type expectedType,
		VBoxContainer container,
		StatescriptPropertyDirection direction,
		int propertyIndex,
		bool isArray = false)
	{
		_graphNode!.ShowResolverEditorUIInternal(
			factory,
			existingBinding,
			expectedType,
			container,
			direction,
			propertyIndex,
			isArray);
	}

	/// <summary>
	/// Requests the owning graph node to recalculate its size.
	/// </summary>
	protected void ResetSize()
	{
		_graphNode!.ResetSize();
	}

	/// <summary>
	/// Raises the <see cref="StatescriptGraphNode.PropertyBindingChanged"/> event.
	/// </summary>
	protected void RaisePropertyBindingChanged()
	{
		_graphNode!.RaisePropertyBindingChangedInternal();
	}
}
#endif
