// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using System.Collections.Generic;
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Godot;
using GodotCollections = Godot.Collections;

namespace Gamesmiths.Forge.Godot.Editor.Statescript;

/// <summary>
/// Base class for custom node property editors. Implementations override the default input-property / output-variable
/// sections rendered by <see cref="StatescriptGraphNode"/> for specific node types. Analogous to Godot's
/// <c>EditorInspectorPlugin</c> pattern.
/// </summary>
/// <remarks>
/// <para>
/// If a <see cref="CustomNodeEditor"/> is registered for a node's <c>RuntimeTypeName</c>, its
/// <see cref="BuildPropertySections"/> method is called instead of the default property rendering. The base class
/// provides helper methods that mirror the default behavior so that custom editors can reuse them selectively.
/// </para>
/// <para>
/// Because this class extends <see cref="RefCounted"/>, signal handlers defined on subclasses can be connected
/// directly to Godot signals (e.g. <c>dropdown.ItemSelected += OnItemSelected</c>) without needing wrapper nodes
/// or workarounds for serialization.
/// </para>
/// </remarks>
[Tool]
internal abstract partial class CustomNodeEditor : RefCounted, ISerializationListener
{
	[NonSerialized]
	private StatescriptGraphNode? _graphNode;

	[NonSerialized]
	private StatescriptGraph? _graph;

	[NonSerialized]
	private StatescriptNode? _nodeResource;

	[NonSerialized]
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
	/// Gets the undo/redo manager, if available.
	/// </summary>
	protected EditorUndoRedoManager? UndoRedo => _graphNode?.GetUndoRedo();

	/// <inheritdoc/>
	public void OnBeforeSerialize()
	{
		Unbind();
	}

	/// <inheritdoc/>
	public void OnAfterDeserialize()
	{
	}

	/// <summary>
	/// Stores references needed by helper methods. Called once after the instance is created.
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
	/// Clears all references stored by <see cref="Bind"/>. Called before the owning graph node is freed or serialized
	/// to prevent accessing disposed objects.
	/// </summary>
	internal virtual void Unbind()
	{
		_graphNode = null;
		_graph = null;
		_nodeResource = null;
		_activeResolverEditors = null;
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
	/// <param name="shapeCustomDataKey">
	/// When provided, enables the row's single/array shape dropdown and wires it to undo-tracked
	/// type/shape changes that persist the new shape under this CustomData key. When null the dropdown is disabled.
	/// </param>
	/// <param name="preferredDefaultResolverTypeId">The preferred default resolver type ID.</param>
	protected void AddInputPropertyRow(
		StatescriptNodeDiscovery.InputPropertyInfo propInfo,
		int index,
		Control container,
		string? shapeCustomDataKey = null,
		string? preferredDefaultResolverTypeId = null)
	{
		_graphNode!.AddInputPropertyRowInternal(
			propInfo,
			index,
			container,
			shapeCustomDataKey,
			preferredDefaultResolverTypeId);
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
	/// Renders an output-variable row as a foldable whose title carries a variable badge with the selected name when
	/// collapsed, matching the built-in nodes' visual (for example <c>SetVariableNode</c>). The caller supplies the
	/// selectable graph-variable names and is notified when the selection changes so it can update the node's binding.
	/// </summary>
	/// <param name="container">The container to add the row to.</param>
	/// <param name="label">The output's display label (without a trailing colon).</param>
	/// <param name="foldKey">The per-node key used to persist this row's fold state.</param>
	/// <param name="candidateVariableNames">The selectable graph-variable names; a <c>(None)</c> entry is prepended.
	/// </param>
	/// <param name="selectedVariableName">The currently bound variable name, or <see langword="null"/> for none.
	/// </param>
	/// <param name="onSelectionChanged">Invoked with the newly selected variable name (<see langword="null"/> for
	/// none).
	/// </param>
	protected void AddOutputVariableBadgeRow(
		VBoxContainer container,
		string label,
		string foldKey,
		IReadOnlyList<string> candidateVariableNames,
		string? selectedVariableName,
		Action<string?> onSelectionChanged)
	{
		string title = $"{label}:";

		var foldable = new FoldableContainer
		{
			Title = title,
			Folded = GetFoldState(foldKey, true),
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
		};
		container.AddChild(foldable);

		var dropdown = new OptionButton { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
		dropdown.SetMeta("is_variable_dropdown", true);
		dropdown.AddItem("(None)");

		int selectedIndex = 0;
		for (int i = 0; i < candidateVariableNames.Count; i++)
		{
			dropdown.AddItem(candidateVariableNames[i]);

			if (!string.IsNullOrEmpty(selectedVariableName) && candidateVariableNames[i] == selectedVariableName)
			{
				selectedIndex = i + 1;
			}
		}

		dropdown.Selected = selectedIndex;
		foldable.AddChild(dropdown);

		void UpdateBadge()
		{
			string selectedName = dropdown.Selected > 0 ? dropdown.GetItemText(dropdown.Selected) : string.Empty;
			InlineConstantSummaryFormatter.ApplyFoldableTitle(
				title,
				foldable,
				string.IsNullOrEmpty(selectedName) ? "(None)" : selectedName,
				InlineSummaryBadgeKind.Variable,
				highlightedVariableName: selectedName);
		}

		foldable.FoldingChanged += folded =>
		{
			SetFoldStateWithUndo(foldKey, folded);
			UpdateBadge();
			RaisePropertyBindingChanged();
			ResetSize();
		};

		dropdown.ItemSelected += selected =>
		{
			onSelectionChanged(selected > 0 ? dropdown.GetItemText((int)selected) : null);
			UpdateBadge();
		};

		UpdateBadge();
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
	/// Gets the persisted fold state for a given key, with a custom default when unset.
	/// </summary>
	/// <param name="key">The key used to persist the fold state.</param>
	/// <param name="defaultValue">The default fold state when no persisted value exists.</param>
	protected bool GetFoldState(string key, bool defaultValue)
	{
		return _graphNode!.GetFoldStateInternal(key, defaultValue);
	}

	/// <summary>
	/// Persists a fold state change with undo support.
	/// </summary>
	/// <param name="key">The key used to persist the fold state.</param>
	/// <param name="folded">The new folded state.</param>
	protected void SetFoldStateWithUndo(string key, bool folded)
	{
		_graphNode!.SetFoldStateWithUndoInternal(key, folded);
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
	/// Refreshes standard input-property foldable summaries.
	/// </summary>
	protected void RefreshInputPropertyFoldableTitles()
	{
		_graphNode!.UpdateInputPropertyFoldableTitlesInternal();
	}

	/// <summary>
	/// Raises the <see cref="StatescriptGraphNode.PropertyBindingChanged"/> event.
	/// </summary>
	protected void RaisePropertyBindingChanged()
	{
		_graphNode!.RaisePropertyBindingChangedInternal();
	}

	/// <summary>
	/// Marks the underlying node and graph resources as changed so Godot persists the latest editor mutations.
	/// </summary>
	protected void NotifyGraphResourceChanged()
	{
		_graphNode!.NotifyGraphResourceChangedInternal();
	}

	/// <summary>
	/// Records an undo/redo action for changing a resolver binding, then rebuilds the node.
	/// </summary>
	/// <param name="direction">The direction of the property.</param>
	/// <param name="propertyIndex">The index of the property.</param>
	/// <param name="oldResolver">The previous resolver resource.</param>
	/// <param name="newResolver">The new resolver resource.</param>
	/// <param name="actionName">The name for the undo/redo action.</param>
	protected void RecordResolverBindingChange(
		StatescriptPropertyDirection direction,
		int propertyIndex,
		StatescriptResolverResource? oldResolver,
		StatescriptResolverResource? newResolver,
		string actionName = "Change Node Property")
	{
		_graphNode!.RecordResolverBindingChangeInternal(
			direction,
			propertyIndex,
			oldResolver,
			newResolver,
			actionName);
	}

	/// <summary>
	/// Changes the type/shape configuration of an input-property slot with full undo/redo support.
	/// </summary>
	/// <remarks>
	/// Custom dropdowns (such as a value-type picker) call this directly; the standard single/array shape dropdown
	/// created by <see cref="AddInputPropertyRow"/> is wired to the same engine automatically. The given CustomData
	/// entries are persisted, the slot's resolver binding is reset, and the node is rebuilt — all as one undoable
	/// action. No per-node bookkeeping is required.
	/// </remarks>
	/// <param name="propertyIndex">The input slot whose configuration changes.</param>
	/// <param name="customData">The CustomData entries to store (e.g. value type and/or array shape).</param>
	/// <param name="actionName">The undo/redo action label.</param>
	protected void ChangeInputPropertyConfig(
		int propertyIndex,
		GodotCollections.Dictionary customData,
		string actionName)
	{
		_graphNode!.ChangeInputPropertyConfigInternal(propertyIndex, customData, actionName);
	}
}
#endif
