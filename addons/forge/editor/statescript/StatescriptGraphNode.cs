// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Godot;
using GodotCollections = Godot.Collections;

namespace Gamesmiths.Forge.Godot.Editor.Statescript;

/// <summary>
/// Visual GraphNode representation for a single Statescript node in the editor.
/// Supports both built-in node types (Entry/Exit) and dynamically discovered concrete types.
/// </summary>
[Tool]
public partial class StatescriptGraphNode : GraphNode, ISerializationListener
{
	// Internal because the dock's replay path writes these straight to the resource when no visual exists.
	internal const string FoldInputKey = "_fold_input";
	internal const string FoldOutputKey = "_fold_output";
	internal const string FoldInputPropertyKeyPrefix = "_fold_input_property_";
	internal const string CustomWidthKey = "_custom_width";

	private static readonly Color _entryColor = new(0x2a4a8dff);
	private static readonly Color _exitColor = new(0x8a549aff);
	private static readonly Color _actionColor = new(0x3a7856ff);
	private static readonly Color _conditionColor = new(0x99811fff);
	private static readonly Color _stateColor = new(0xa52c38ff);
	private static readonly Color _flowColor = new(0x2c7a8cff);
	private static readonly Color _eventColor = new(0xabb2bfff);
	private static readonly Color _subgraphColor = new(0xc678ddff);
	private static readonly Color _inputPropertyColor = new(0x61afefff);
	private static readonly Color _outputVariableColor = new(0xe5c07bff);

	private readonly Dictionary<PropertySlotKey, NodeEditorProperty> _activeResolverEditors = [];
	private readonly Dictionary<FoldableContainer, string> _foldableKeys = [];
	private readonly Dictionary<PropertySlotKey, InputPropertyFoldableContext> _inputPropertyFoldables = [];
	private readonly Dictionary<int, PendingInputConfig> _pendingInputConfigs = [];

	private PendingInputConfig? _pendingLayoutConfig;

	private int[] _visualToRuntimeOutputPortMap = [];
	private int[] _runtimeToVisualOutputPortMap = [];
	private StatescriptNodeDiscovery.NodeTypeInfo? _typeInfo;
	private StatescriptGraph? _graph;
	private EditorUndoRedoManager? _undoRedo;
	private StatescriptGraphEditorDock? _replayHost;
	private CustomNodeEditor? _activeCustomEditor;
	private bool _resizeConnected;
	private bool _refittingSize;
	private float _widthBeforeResize;
	private string? _highlightedVariableName;
	private string? _highlightedSharedVariableSetPath;
	private string? _highlightedSharedVariableName;
	private bool _isHighlighted;
	private bool _highlightStylesApplied;

	/// <summary>
	/// Raised when a property binding has been modified in the UI.
	/// </summary>
	public event Action? PropertyBindingChanged;

	/// <summary>
	/// Gets the underlying node resource.
	/// </summary>
	public StatescriptNode? NodeResource { get; private set; }

	/// <summary>
	/// Gets the undo/redo manager only when a replay host and graph are wired, since per-node actions must be
	/// registered on the host.
	/// </summary>
	private EditorUndoRedoManager? ReplayableUndoRedo =>
		_replayHost is not null && _graph is not null ? _undoRedo : null;

	public int VisualToRuntimeOutputPort(int visualPort)
	{
		return visualPort >= 0 && visualPort < _visualToRuntimeOutputPortMap.Length
			? _visualToRuntimeOutputPortMap[visualPort]
			: visualPort;
	}

	public int RuntimeToVisualOutputPort(int runtimePort)
	{
		return runtimePort >= 0 && runtimePort < _runtimeToVisualOutputPortMap.Length
			? _runtimeToVisualOutputPortMap[runtimePort]
			: runtimePort;
	}

	/// <summary>
	/// Sets the <see cref="EditorUndoRedoManager"/> used for undo/redo support.
	/// </summary>
	/// <param name="undoRedo">The undo/redo manager from the editor plugin.</param>
	public void SetUndoRedo(EditorUndoRedoManager? undoRedo)
	{
		_undoRedo = undoRedo;
	}

	/// <summary>
	/// Sets the dock that hosts this node's undo/redo replay callbacks.
	/// </summary>
	/// <remarks>
	/// Actions must never be registered on this visual: it is freed whenever the node is deleted, the tab closes, or
	/// cached visuals are invalidated, and Godot silently skips do/undo operations whose target object is gone, which
	/// broke redo across node re-creation. The dock outlives every visual.
	/// </remarks>
	/// <param name="replayHost">The dock owning this visual.</param>
	public void SetReplayHost(StatescriptGraphEditorDock replayHost)
	{
		_replayHost = replayHost;
	}

	/// <summary>
	/// Gets the <see cref="EditorUndoRedoManager"/> used for undo/redo support.
	/// </summary>
	/// <returns>The undo/redo manager, or null if not set.</returns>
	public EditorUndoRedoManager? GetUndoRedo()
	{
		return _undoRedo;
	}

	/// <summary>
	/// Updates the highlight state based on the given variable name.
	/// </summary>
	/// <param name="variableName">The variable name to highlight, or null to clear.</param>
	public void SetHighlightedVariable(string? variableName)
	{
		_highlightedVariableName = variableName;
		RefreshHighlightState();
	}

	public void SetHighlightedSharedVariable(string? sharedVariableSetPath, string? variableName)
	{
		_highlightedSharedVariableSetPath = sharedVariableSetPath;
		_highlightedSharedVariableName = variableName;
		RefreshHighlightState();
	}

	/// <summary>
	/// Initializes this visual node from a resource, optionally within the context of a graph.
	/// </summary>
	/// <param name="resource">The node resource to display.</param>
	/// <param name="graph">The owning graph resource (needed for variable dropdowns).</param>
	public void Initialize(StatescriptNode resource, StatescriptGraph? graph = null)
	{
		NodeResource = resource;
		_graph = graph;

		_activeCustomEditor?.Unbind();
		_activeCustomEditor = null;
		_activeResolverEditors.Clear();
		_foldableKeys.Clear();
		_inputPropertyFoldables.Clear();

		_inputPropertyContexts.Clear();

		Name = resource.NodeId;
		Title = resource.Title;
		PositionOffset = resource.PositionOffset;
		CustomMinimumSize = new Vector2(260, 0);
		Resizable = true;

		RestoreCustomWidth();

		if (!_resizeConnected)
		{
			_widthBeforeResize = CustomMinimumSize.X;
			ResizeRequest += OnResizeRequest;
			ResizeEnd += OnResizeEnd;

			// Re-fit whenever the combined minimum changes (section or nested-resolver collapse/expand, rebuilds,
			// content edits). This signal fires after the minimum is recomputed, so the node reliably shrinks back to
			// its floor instead of staying stretched at a stale, still-expanded width.
			MinimumSizeChanged += OnNodeMinimumSizeChanged;
			_resizeConnected = true;
		}

		ClearSlots();

		if (resource.NodeType is StatescriptNodeType.Entry or StatescriptNodeType.Exit
			|| string.IsNullOrEmpty(resource.RuntimeTypeName))
		{
			SetupNodeByType(resource.NodeType);
			ApplyBottomPadding();
			RefreshHighlightState();
			return;
		}

		_typeInfo = StatescriptNodeDiscovery.FindForNode(resource);
		if (_typeInfo is not null)
		{
			SetupFromTypeInfo(_typeInfo);
		}
		else
		{
			SetupNodeByType(resource.NodeType);
		}

		ApplyBottomPadding();
		RefreshHighlightState();
	}

	public void OnBeforeSerialize()
	{
		_inputPropertyContexts.Clear();
		_foldableKeys.Clear();
		_inputPropertyFoldables.Clear();
		_pendingInputConfigs.Clear();
		_pendingLayoutConfig = null;

		_activeCustomEditor?.Unbind();
		_activeCustomEditor = null;

		foreach (KeyValuePair<PropertySlotKey, NodeEditorProperty> kvp in
			_activeResolverEditors.Where(kvp => IsInstanceValid(kvp.Value)))
		{
			kvp.Value.ClearCallbacks();
		}

		_activeResolverEditors.Clear();
		PropertyBindingChanged = null;
	}

	public void OnAfterDeserialize()
	{
	}

	/// <inheritdoc/>
	public override void _Notification(int what)
	{
		base._Notification(what);

		// The collapsed-row label/pill split is measured from the title-font metrics and the row width. Both can change
		// without a resize event: the editor theme (hence the title font) is applied after the node is first built when
		// the Statescript tab is the active tab on editor startup, and the row width settles when the node is re-shown
		// (switching back to the tab). Re-run the width sync in those cases. It only recomputes CustomMinimumSize (no
		// theme overrides are re-applied), so responding to NotificationThemeChanged here does not re-enter.
		if (what == NotificationThemeChanged || what == NotificationVisibilityChanged)
		{
			InlineConstantSummaryFormatter.RequestBadgeWidthSync(this);
		}
	}

	internal FoldableContainer AddPropertySectionDividerInternal(
		string sectionTitle,
		Color color,
		string foldKey,
		bool folded)
	{
		return AddPropertySectionDivider(sectionTitle, color, foldKey, folded);
	}

	internal void AddNodeBodyContentInternal(Control content)
	{
		AddChild(content);
	}

	internal void AddInputPropertyRowInternal(
		StatescriptNodeDiscovery.InputPropertyInfo propInfo,
		int index,
		Control container,
		string? shapeCustomDataKey = null,
		string? preferredDefaultResolverTypeId = null,
		Variant? defaultConstantValue = null)
	{
		AddInputPropertyRow(
			propInfo,
			index,
			container,
			shapeCustomDataKey,
			preferredDefaultResolverTypeId,
			defaultConstantValue);
	}

	internal void AddOutputVariableRowInternal(
		StatescriptNodeDiscovery.OutputVariableInfo varInfo,
		int index,
		Control container)
	{
		AddOutputVariableRow(varInfo, index, container);
	}

	internal bool GetFoldStateInternal(string key)
	{
		return GetFoldState(key);
	}

	internal bool GetFoldStateInternal(string key, bool defaultValue)
	{
		return GetFoldState(key, defaultValue);
	}

	internal void PersistFoldStateInternal(string key, bool folded)
	{
		PersistFoldState(key, folded);
	}

	internal void SetNodeConfigWithUndoInternal(string key, Variant value, string actionName, bool rebuildOnChange)
	{
		SetNodeConfigWithUndo(key, value, actionName, rebuildOnChange);
	}

	/// <summary>
	/// Applies a node configuration change on this live visual during a replay.
	/// </summary>
	/// <remarks>
	/// This and the sibling <c>Apply*Internal</c> methods are called by <c>StatescriptGraphEditorDock.NodeReplay</c>,
	/// which owns the replay scope. They are never registered with the undo manager directly; see
	/// <see cref="SetReplayHost"/>.
	/// </remarks>
	/// <param name="key">The CustomData key to restore.</param>
	/// <param name="value">The value to restore, or a Nil variant to drop the key entirely.</param>
	internal void ApplyNodeConfigInternal(string key, Variant value)
	{
		ApplyNodeConfigCore(key, value);
	}

	/// <summary>
	/// Applies a width change on this live visual during a replay.
	/// </summary>
	/// <param name="width">The width to restore.</param>
	internal void ApplyCustomWidthInternal(float width)
	{
		ApplyCustomWidthCore(width);
	}

	/// <summary>
	/// Applies a resolver binding change on this live visual during a replay.
	/// </summary>
	/// <param name="directionInt">The direction of the property, as an int.</param>
	/// <param name="propertyIndex">The index of the property.</param>
	/// <param name="resolverVariant">The resolver to bind, or Nil to clear the binding.</param>
	internal void ApplyResolverBindingInternal(int directionInt, int propertyIndex, Variant resolverVariant)
	{
		ApplyResolverBindingCore(directionInt, propertyIndex, resolverVariant);
	}

	/// <summary>
	/// Applies an input-property config change on this live visual during a replay.
	/// </summary>
	/// <param name="customData">The CustomData entries to write.</param>
	/// <param name="propertyIndex">The input slot being configured.</param>
	/// <param name="resolverVariant">The resolver to bind, or Nil to clear the binding.</param>
	internal void ApplyInputPropertyConfigInternal(
		GodotCollections.Dictionary customData,
		int propertyIndex,
		Variant resolverVariant)
	{
		ApplyInputPropertyConfigCore(customData, propertyIndex, resolverVariant);
	}

	/// <summary>
	/// Applies a port-layout config change on this live visual during a replay.
	/// </summary>
	/// <param name="customData">The CustomData entries to write.</param>
	/// <param name="connectionsToRemove">Connections to detach before the node is laid out again.</param>
	/// <param name="connectionsToAdd">Connections to attach after the node has been laid out.</param>
	internal void ApplyLayoutConfigInternal(
		GodotCollections.Dictionary customData,
		GodotCollections.Array<StatescriptConnection> connectionsToRemove,
		GodotCollections.Array<StatescriptConnection> connectionsToAdd)
	{
		ApplyLayoutConfigCore(customData, connectionsToRemove, connectionsToAdd);
	}

	internal StatescriptNodeProperty? FindBindingInternal(
		StatescriptPropertyDirection direction,
		int propertyIndex)
	{
		return FindBinding(direction, propertyIndex);
	}

	internal StatescriptNodeProperty EnsureBindingInternal(
		StatescriptPropertyDirection direction,
		int propertyIndex)
	{
		return EnsureBinding(direction, propertyIndex);
	}

	internal void RemoveBindingInternal(
		StatescriptPropertyDirection direction,
		int propertyIndex)
	{
		RemoveBinding(direction, propertyIndex);
	}

	internal void RecordResolverBindingChangeInternal(
		StatescriptPropertyDirection direction,
		int propertyIndex,
		StatescriptResolverResource? oldResolver,
		StatescriptResolverResource? newResolver,
		string actionName)
	{
		if (NodeResource is null)
		{
			return;
		}

		string nodeId = NodeResource.NodeId;

		EditorUndoRedoUtils.Record(
			ReplayableUndoRedo,
			actionName,
			_graph,
			undo =>
			{
				undo.AddDoMethod(
					_replayHost!,
					StatescriptGraphEditorDock.MethodName.ReplayResolverBinding,
					_graph!,
					nodeId,
					(int)direction,
					propertyIndex,
					Variant.From(newResolver));
				undo.AddUndoMethod(
					_replayHost!,
					StatescriptGraphEditorDock.MethodName.ReplayResolverBinding,
					_graph!,
					nodeId,
					(int)direction,
					propertyIndex,
					Variant.From(oldResolver));
			});
	}

	internal void ShowResolverEditorUIInternal(
		Func<NodeEditorProperty> factory,
		StatescriptNodeProperty? existingBinding,
		Type expectedType,
		VBoxContainer container,
		StatescriptPropertyDirection direction,
		int propertyIndex,
		bool isArray = false)
	{
		ShowResolverEditorUI(factory, existingBinding, expectedType, container, direction, propertyIndex, isArray);
	}

	internal void RaisePropertyBindingChangedInternal()
	{
		PropertyBindingChanged?.Invoke();
	}

	internal void NotifyGraphResourceChangedInternal()
	{
		NotifyGraphResourceChanged();
	}

	internal void UpdateInputPropertyFoldableTitlesInternal()
	{
		UpdateInputPropertyFoldableTitles();
	}

	/// <summary>
	/// Requests an input-property type/shape change for a single slot with full undo/redo support.
	/// </summary>
	/// <remarks>
	/// This is the single entry point used both by standard shape dropdowns (wired automatically in
	/// <see cref="AddInputPropertyRow"/>) and by custom dropdowns in node editors (via
	/// <see cref="CustomNodeEditor.ChangeInputPropertyConfig"/>). The actual mutation is deferred so the dropdown that
	/// emitted the change is not freed mid-signal by the node rebuild, and repeated changes to the same slot within a
	/// frame are coalesced into a single undo action.
	/// </remarks>
	/// <param name="propertyIndex">The input slot whose configuration changes.</param>
	/// <param name="customData">The CustomData entries to store (e.g. value type and/or array shape).</param>
	/// <param name="actionName">The undo/redo action label.</param>
	internal void ChangeInputPropertyConfigInternal(
		int propertyIndex,
		GodotCollections.Dictionary customData,
		string actionName)
	{
		if (NodeResource is null)
		{
			return;
		}

		// The flush records, and being deferred it would run after the replay scope closed, committing a stray action.
		if (EditorUndoRedoUtils.IsReplaying)
		{
			return;
		}

		// Merge into any change already queued for this slot this frame so none is lost before the deferred flush.
		if (_pendingInputConfigs.TryGetValue(propertyIndex, out PendingInputConfig? pending))
		{
			foreach (KeyValuePair<Variant, Variant> entry in customData)
			{
				pending.CustomData[entry.Key] = entry.Value;
			}

			return;
		}

		_pendingInputConfigs[propertyIndex] = new PendingInputConfig(customData, actionName);
		CallDeferred(MethodName.FlushInputPropertyConfig, propertyIndex);
	}

	/// <summary>
	/// Requests a change to the node configuration that drives its port layout (a <c>SwitchNode</c>'s case count, a
	/// <c>StateMachineNode</c>'s state count, or the enum those counts follow), with full undo/redo support.
	/// </summary>
	/// <remarks>
	/// Connections attached to ports the new layout no longer has are removed as part of the same undoable action, and
	/// restored on undo, so shrinking a node never leaves the graph holding connections to ports that are gone. The
	/// mutation is deferred so the control that emitted the change is not freed mid-signal by the node rebuild.
	/// </remarks>
	/// <param name="customData">The CustomData entries to store (the constructor argument driving the layout, plus any
	/// editor-only settings that go with it).</param>
	/// <param name="actionName">The undo/redo action label.</param>
	internal void ChangeNodeLayoutConfigInternal(GodotCollections.Dictionary customData, string actionName)
	{
		if (NodeResource is null)
		{
			return;
		}

		// See ChangeInputPropertyConfigInternal: a replay-driven rebuild must not queue a recording flush.
		if (EditorUndoRedoUtils.IsReplaying)
		{
			return;
		}

		// Merge into any change already queued this frame so none is lost before the deferred flush.
		if (_pendingLayoutConfig is not null)
		{
			foreach (KeyValuePair<Variant, Variant> entry in customData)
			{
				_pendingLayoutConfig.CustomData[entry.Key] = entry.Value;
			}

			return;
		}

		_pendingLayoutConfig = new PendingInputConfig(customData, actionName);
		CallDeferred(MethodName.FlushLayoutConfig);
	}

	private static string GetResolverTypeId(StatescriptResolverResource resolver)
	{
		return resolver.ResolverTypeId;
	}

	private static void ClearContainer(Control container)
	{
		foreach (Node child in container.GetChildren())
		{
			container.RemoveChild(child);
			child.QueueFree();
		}
	}

	private static string GetInputPropertyFoldKey(int propertyIndex)
	{
		return $"{FoldInputPropertyKeyPrefix}{propertyIndex}";
	}

	private static bool VariantEquals(Variant a, Variant b)
	{
		if (a.VariantType != b.VariantType)
		{
			return false;
		}

		return a.VariantType switch
		{
			Variant.Type.Bool => a.AsBool() == b.AsBool(),
			Variant.Type.Int => a.AsInt64() == b.AsInt64(),
			_ => a.AsString() == b.AsString(),
		};
	}

	private void SetupFromTypeInfo(StatescriptNodeDiscovery.NodeTypeInfo typeInfo)
	{
		// The custom editor is created before the port rows so it can rename ports (an enum-driven Switch or State
		// Machine labels its ports with the enum's member names). Its property sections are still built afterwards, so
		// they keep rendering below the ports.
		if (CustomNodeEditorRegistry.TryCreate(typeInfo.RuntimeTypeName, out CustomNodeEditor? customEditor))
		{
			Debug.Assert(_graph is not null, "Graph context is required for custom node editors.");
			Debug.Assert(NodeResource is not null, "Node resource is required for custom node editors.");

			_activeCustomEditor = customEditor;
			customEditor.Bind(this, _graph, NodeResource, _activeResolverEditors);
		}
		else
		{
			_activeCustomEditor = null;
		}

		BuildOutputPortMappings(typeInfo);
		int maxSlots = Math.Max(typeInfo.InputPortLabels.Length, _visualToRuntimeOutputPortMap.Length);

		for (int slot = 0; slot < maxSlots; slot++)
		{
			var hBox = new HBoxContainer();
			hBox.AddThemeConstantOverride("separation", 16);
			AddChild(hBox);

			if (slot < typeInfo.InputPortLabels.Length)
			{
				var inputLabel = new Label
				{
					Text = typeInfo.InputPortLabels[slot],
				};

				hBox.AddChild(inputLabel);
				SetSlotEnabledLeft(slot, true);
				SetSlotColorLeft(slot, _eventColor);
			}
			else
			{
				var spacer = new Control();
				hBox.AddChild(spacer);
			}

			if (slot < _visualToRuntimeOutputPortMap.Length)
			{
				int runtimeOutputSlot = VisualToRuntimeOutputPort(slot);
				var outputLabel = new Label
				{
					Text = _activeCustomEditor?.GetOutputPortLabel(runtimeOutputSlot, typeInfo)
						?? typeInfo.OutputPortLabels[runtimeOutputSlot],
					HorizontalAlignment = HorizontalAlignment.Right,
					SizeFlagsHorizontal = SizeFlags.ExpandFill,
				};

				hBox.AddChild(outputLabel);
				SetSlotEnabledRight(slot, true);
				Color portColor = typeInfo.IsSubgraphPort[runtimeOutputSlot] ? _subgraphColor : _eventColor;
				SetSlotColorRight(slot, portColor);
			}
		}

		if (_activeCustomEditor is not null)
		{
			_activeCustomEditor.BuildPropertySections(typeInfo);
		}
		else
		{
			BuildDefaultPropertySections(typeInfo);
		}

		Color titleColor = typeInfo.NodeType switch
		{
			StatescriptNodeType.Action => _actionColor,
			StatescriptNodeType.Condition => _conditionColor,
			StatescriptNodeType.State => _stateColor,
			StatescriptNodeType.Flow => _flowColor,
			StatescriptNodeType.Entry => _entryColor,
			StatescriptNodeType.Exit => _exitColor,
			_ => _entryColor,
		};

		ApplyTitleBarColor(titleColor);
	}

	private void BuildDefaultPropertySections(StatescriptNodeDiscovery.NodeTypeInfo typeInfo)
	{
		if (typeInfo.InputPropertiesInfo.Length > 0)
		{
			bool folded = GetFoldState(FoldInputKey);
			FoldableContainer inputContainer = AddPropertySectionDivider(
				"Input Properties",
				_inputPropertyColor,
				FoldInputKey,
				folded);

			// FoldableContainer fits every child into the same content rect (children overlap, last drawn on top), so
			// rows must be stacked inside a single VBoxContainer child instead of being added to the section directly.
			var inputRoot = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
			inputContainer.AddChild(inputRoot);

			for (int i = 0; i < typeInfo.InputPropertiesInfo.Length; i++)
			{
				AddInputPropertyRow(typeInfo.InputPropertiesInfo[i], i, inputRoot);
			}
		}

		if (typeInfo.OutputVariablesInfo.Length > 0)
		{
			bool folded = GetFoldState(FoldOutputKey);
			FoldableContainer outputContainer = AddPropertySectionDivider(
				"Output Variables",
				_outputVariableColor,
				FoldOutputKey,
				folded);

			var outputRoot = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
			outputContainer.AddChild(outputRoot);

			for (int i = 0; i < typeInfo.OutputVariablesInfo.Length; i++)
			{
				AddOutputVariableRow(typeInfo.OutputVariablesInfo[i], i, outputRoot);
			}
		}
	}

	private void BuildOutputPortMappings(StatescriptNodeDiscovery.NodeTypeInfo typeInfo)
	{
		int outputCount = typeInfo.OutputPortLabels.Length;
		_visualToRuntimeOutputPortMap = new int[outputCount];
		_runtimeToVisualOutputPortMap = new int[outputCount];

		int visualIndex = 0;
		for (int runtimeIndex = 0; runtimeIndex < outputCount; runtimeIndex++)
		{
			if (typeInfo.IsSubgraphPort[runtimeIndex])
			{
				continue;
			}

			_visualToRuntimeOutputPortMap[visualIndex] = runtimeIndex;
			_runtimeToVisualOutputPortMap[runtimeIndex] = visualIndex;
			visualIndex++;
		}

		for (int runtimeIndex = 0; runtimeIndex < outputCount; runtimeIndex++)
		{
			if (!typeInfo.IsSubgraphPort[runtimeIndex])
			{
				continue;
			}

			_visualToRuntimeOutputPortMap[visualIndex] = runtimeIndex;
			_runtimeToVisualOutputPortMap[runtimeIndex] = visualIndex;
			visualIndex++;
		}
	}

	private FoldableContainer AddPropertySectionDivider(
		string sectionTitle,
		Color color,
		string foldKey,
		bool folded)
	{
		var divider = new HSeparator { CustomMinimumSize = new Vector2(0, 4) };
		AddChild(divider);

		var sectionContainer = new FoldableContainer
		{
			Title = sectionTitle,
			Folded = folded,
			CustomMinimumSize = new Vector2(192, 0),
		};

		sectionContainer.AddThemeColorOverride("font_color", color);

		_foldableKeys[sectionContainer] = foldKey;
		sectionContainer.FoldingChanged += OnSectionFoldingChanged;

		AddChild(sectionContainer);

		return sectionContainer;
	}

	private void OnSectionFoldingChanged(bool isFolded)
	{
		foreach (KeyValuePair<FoldableContainer, string> kvp in _foldableKeys.Where(kvp => IsInstanceValid(kvp.Key)))
		{
			bool stored = GetFoldState(kvp.Value);
			if (kvp.Key.Folded != stored)
			{
				PersistFoldState(kvp.Value, kvp.Key.Folded);
			}
		}

		UpdateInputPropertyFoldableTitles();
		RefreshHighlightState();

		// Width/height re-fitting is handled by OnNodeMinimumSizeChanged, which fires once the foldable has recomputed
		// its minimum size. Resetting here would measure against the still-expanded width.
	}

	private void OnNodeMinimumSizeChanged()
	{
		if (_refittingSize)
		{
			return;
		}

		_refittingSize = true;

		// Pin the node to its combined minimum: max(CustomMinimumSize.X, content width) for width and the natural
		// height. CustomMinimumSize.X is the floor (the user's custom width, or the default base width). Godot only
		// enforces the lower bound on Size, so collapsing content requires applying the shrink explicitly here, where
		// the freshly recomputed minimum is available.
		Size = GetCombinedMinimumSize();

		_refittingSize = false;
	}

	private void UpdateInputPropertyFoldableTitle(PropertySlotKey key)
	{
		if (!_inputPropertyFoldables.TryGetValue(key, out InputPropertyFoldableContext? context)
			|| !IsInstanceValid(context.Foldable))
		{
			return;
		}

		_activeResolverEditors.TryGetValue(key, out NodeEditorProperty? editor);

		// An optional slot resting on (None) has no resolver editor at all, so there is nothing for the editor-driven
		// badge to summarize. Label it explicitly instead of leaving the collapsed row blank, matching how the
		// output-variable rows badge their own (None).
		if (editor is null
			&& _inputPropertyContexts.TryGetValue(key, out InputPropertyContext? inputContext)
			&& inputContext.PropInfo.IsOptional)
		{
			InlineConstantSummaryFormatter.ApplyFoldableTitle(
				context.BaseTitle,
				context.Foldable,
				NoneResolverItemText,
				InlineSummaryBadgeKind.Resolver);
			return;
		}

		InlineConstantSummaryFormatter.ApplyFoldableTitle(context.BaseTitle, context.Foldable, editor);
	}

	private void UpdateInputPropertyFoldableTitles()
	{
		foreach (PropertySlotKey key in _inputPropertyFoldables.Keys.ToArray())
		{
			UpdateInputPropertyFoldableTitle(key);
		}
	}

	private bool GetFoldState(string key)
	{
		return GetFoldState(key, false);
	}

	private bool GetFoldState(string key, bool defaultValue)
	{
		if (NodeResource is not null && NodeResource.CustomData.TryGetValue(key, out Variant value))
		{
			return value.AsBool();
		}

		return defaultValue;
	}

	private void SetFoldState(string key, bool folded)
	{
		if (NodeResource is null)
		{
			return;
		}

		NodeResource.CustomData[key] = Variant.From(folded);
		NotifyGraphResourceChanged();
	}

	/// <summary>
	/// Persists a section's fold state without recording an undo step, since folding is view state.
	/// </summary>
	/// <param name="key">The CustomData key holding this section's fold state.</param>
	/// <param name="folded">The new folded state.</param>
	private void PersistFoldState(string key, bool folded)
	{
		if (NodeResource is null || GetFoldState(key) == folded)
		{
			return;
		}

		SetFoldState(key, folded);
	}

	private void SetNodeConfigWithUndo(string key, Variant value, string actionName, bool rebuildOnChange)
	{
		if (NodeResource is null)
		{
			return;
		}

		bool had = NodeResource.CustomData.TryGetValue(key, out Variant oldValue);

		if (had && VariantEquals(oldValue, value))
		{
			return;
		}

		NodeResource.CustomData[key] = value;
		NotifyGraphResourceChanged();

		Variant capturedOld = had ? oldValue : default;
		bool hadOld = had;

		string nodeId = NodeResource.NodeId;

		EditorUndoRedoUtils.Record(
			ReplayableUndoRedo,
			actionName,
			_graph,
			undo =>
			{
				undo.AddDoMethod(
					_replayHost!,
					StatescriptGraphEditorDock.MethodName.ReplayNodeConfig,
					_graph!,
					nodeId,
					key,
					value);
				undo.AddUndoMethod(
					_replayHost!,
					StatescriptGraphEditorDock.MethodName.ReplayNodeConfig,
					_graph!,
					nodeId,
					key,
					hadOld ? capturedOld : Variant.From<GodotObject?>(null));
			});

		// Deferred so the control still emitting this change is not freed mid-signal. Replays rebuild unconditionally.
		if (rebuildOnChange)
		{
			Callable.From(RebuildNode).CallDeferred();
		}
	}

	/// <summary>
	/// Applies a node configuration change and rebuilds the node.
	/// </summary>
	/// <remarks>
	/// The rebuild is not optional. The control showing this value reads <c>CustomData</c> only while being built, so
	/// skipping it would restore the data while leaving the old selection on screen, and the next save would then
	/// persist whatever the stale UI shows.
	/// </remarks>
	/// <param name="key">The CustomData key to restore.</param>
	/// <param name="value">The value to restore, or a Nil variant to drop the key entirely.</param>
	private void ApplyNodeConfigCore(string key, Variant value)
	{
		if (NodeResource is null)
		{
			return;
		}

		if (value.VariantType == Variant.Type.Nil)
		{
			NodeResource.CustomData.Remove(key);
		}
		else
		{
			NodeResource.CustomData[key] = value;
		}

		NotifyGraphResourceChanged();
		RebuildNode();
	}

	private void OnResizeRequest(Vector2 newMinSize)
	{
		CustomMinimumSize = new Vector2(newMinSize.X, 0);
		Size = new Vector2(newMinSize.X, 0);
		SaveCustomWidth(newMinSize.X);
	}

	private void OnResizeEnd(Vector2 newSize)
	{
		float newWidth = CustomMinimumSize.X;

		if (NodeResource is not null && !Mathf.IsEqualApprox(_widthBeforeResize, newWidth))
		{
			float oldWidth = _widthBeforeResize;
			string nodeId = NodeResource.NodeId;

			EditorUndoRedoUtils.Record(
				ReplayableUndoRedo,
				"Resize Node",
				_graph,
				undo =>
				{
					undo.AddDoMethod(
						_replayHost!,
						StatescriptGraphEditorDock.MethodName.ReplayNodeWidth,
						_graph!,
						nodeId,
						newWidth);
					undo.AddUndoMethod(
						_replayHost!,
						StatescriptGraphEditorDock.MethodName.ReplayNodeWidth,
						_graph!,
						nodeId,
						oldWidth);
				});
		}

		_widthBeforeResize = newWidth;
	}

	private void ApplyCustomWidthCore(float width)
	{
		CustomMinimumSize = new Vector2(width, 0);
		Size = new Vector2(width, 0);
		SaveCustomWidth(width);
	}

	private void RestoreCustomWidth()
	{
		if (NodeResource is not null
			&& NodeResource.CustomData.TryGetValue(CustomWidthKey, out Variant value))
		{
			float width = (float)value.AsDouble();

			if (width > 0)
			{
				CustomMinimumSize = new Vector2(width, 0);
			}
		}
	}

	private void SaveCustomWidth(float width)
	{
		if (NodeResource is null)
		{
			return;
		}

		NodeResource.CustomData[CustomWidthKey] = Variant.From(width);
		NotifyGraphResourceChanged();
	}

	private void ApplyResolverBindingCore(
		int directionInt,
		int propertyIndex,
		Variant resolverVariant)
	{
		if (NodeResource is null)
		{
			return;
		}

		var direction = (StatescriptPropertyDirection)directionInt;

		if (resolverVariant.AsGodotObject() is not StatescriptResolverResource resolver)
		{
			RemoveBinding(direction, propertyIndex);
		}
		else
		{
			EnsureBinding(direction, propertyIndex).Resolver = resolver;
		}

		EnsurePropertyVisible(direction, propertyIndex);
		NotifyGraphResourceChanged();
		RebuildNode();
	}

	/// <summary>
	/// Expands the section (and the individual input-property foldable) that contains the given slot so an undo/redo or
	/// programmatic change is never hidden inside a collapsed section. This only adjusts persisted fold state; it does
	/// not record its own undo step, so user-driven collapse/expand remains the only fold action on the undo stack.
	/// </summary>
	/// <param name="direction">The direction of the property (input or output).</param>
	/// <param name="propertyIndex">The index of the property.</param>
	private void EnsurePropertyVisible(StatescriptPropertyDirection direction, int propertyIndex)
	{
		if (NodeResource is null)
		{
			return;
		}

		if (direction == StatescriptPropertyDirection.Input)
		{
			SetFoldState(FoldInputKey, false);
			SetFoldState(GetInputPropertyFoldKey(propertyIndex), false);
		}
		else
		{
			SetFoldState(FoldOutputKey, false);
		}
	}

	private void FlushInputPropertyConfig(int propertyIndex)
	{
		if (!_pendingInputConfigs.Remove(propertyIndex, out PendingInputConfig? pending) || NodeResource is null)
		{
			return;
		}

		GodotCollections.Dictionary newData = pending.CustomData;

		// Snapshot the current values of exactly the keys being changed so they can be restored on undo, and detect
		// whether anything actually changes (dropdowns can re-emit the already-selected value).
		var oldData = new GodotCollections.Dictionary();
		bool changed = false;

		foreach (KeyValuePair<Variant, Variant> entry in newData)
		{
			string key = entry.Key.AsString();
			bool had = NodeResource.CustomData.TryGetValue(key, out Variant existing);
			oldData[key] = had ? existing : default;

			if (!had || !VariantEquals(existing, entry.Value))
			{
				changed = true;
			}
		}

		if (!changed)
		{
			return;
		}

		var oldResolver = FindBinding(StatescriptPropertyDirection.Input, propertyIndex)?.Resolver?.Duplicate()
			as StatescriptResolverResource;

		// Changing an input's type/shape invalidates its resolver, so the binding is reset (passing a Nil resolver).
		// The core, not the replay wrapper: as a replay this would suppress the binding the rebuild seeds for the
		// new type.
		ApplyInputPropertyConfigCore(newData, propertyIndex, default);

		string nodeId = NodeResource.NodeId;

		EditorUndoRedoUtils.Record(
			ReplayableUndoRedo,
			pending.ActionName,
			_graph,
			undo =>
			{
				undo.AddDoMethod(
					_replayHost!,
					StatescriptGraphEditorDock.MethodName.ReplayInputPropertyConfig,
					_graph!,
					nodeId,
					newData,
					propertyIndex,
					Variant.From((StatescriptResolverResource?)null));
				undo.AddUndoMethod(
					_replayHost!,
					StatescriptGraphEditorDock.MethodName.ReplayInputPropertyConfig,
					_graph!,
					nodeId,
					oldData,
					propertyIndex,
					Variant.From(oldResolver));
			});

		PropertyBindingChanged?.Invoke();
	}

	private void FlushLayoutConfig()
	{
		if (_pendingLayoutConfig is null || NodeResource is null)
		{
			return;
		}

		PendingInputConfig pending = _pendingLayoutConfig;
		_pendingLayoutConfig = null;

		GodotCollections.Dictionary newData = pending.CustomData;

		// Snapshot the current values of exactly the keys being changed so they can be restored on undo, and detect
		// whether anything actually changes (controls can re-emit the already-selected value).
		var oldData = new GodotCollections.Dictionary();
		bool changed = false;

		foreach (KeyValuePair<Variant, Variant> entry in newData)
		{
			string key = entry.Key.AsString();
			bool had = NodeResource.CustomData.TryGetValue(key, out Variant existing);
			oldData[key] = had ? existing : default;

			if (!had || !VariantEquals(existing, entry.Value))
			{
				changed = true;
			}
		}

		if (!changed)
		{
			return;
		}

		var connectionsToRemove = new GodotCollections.Array<StatescriptConnection>();
		var connectionsToAdd = new GodotCollections.Array<StatescriptConnection>();
		CollectConnectionChanges(newData, connectionsToRemove, connectionsToAdd);

		// The core, not the replay wrapper: as a replay this would suppress the bindings the rebuild seeds.
		ApplyLayoutConfigCore(newData, connectionsToRemove, connectionsToAdd);

		string nodeId = NodeResource.NodeId;

		// Undo is the same operation mirrored: restore the old configuration and swap the two connection sets back.
		EditorUndoRedoUtils.Record(
			ReplayableUndoRedo,
			pending.ActionName,
			_graph,
			undo =>
			{
				undo.AddDoMethod(
					_replayHost!,
					StatescriptGraphEditorDock.MethodName.ReplayLayoutConfig,
					_graph!,
					nodeId,
					newData,
					connectionsToRemove,
					connectionsToAdd);
				undo.AddUndoMethod(
					_replayHost!,
					StatescriptGraphEditorDock.MethodName.ReplayLayoutConfig,
					_graph!,
					nodeId,
					oldData,
					connectionsToAdd,
					connectionsToRemove);
			});

		PropertyBindingChanged?.Invoke();
	}

	/// <summary>
	/// Works out what has to happen to this node's connections for the given configuration change: connections whose
	/// port the new layout does not have are removed, and connections whose port merely moves (a Switch node's Default
	/// port, which always sits after the cases) are replaced by the same connection on its new port.
	/// </summary>
	/// <param name="newData">The CustomData entries about to be written.</param>
	/// <param name="connectionsToRemove">Receives the connections to detach.</param>
	/// <param name="connectionsToAdd">Receives the connections to attach in their place.</param>
	private void CollectConnectionChanges(
		GodotCollections.Dictionary newData,
		GodotCollections.Array<StatescriptConnection> connectionsToRemove,
		GodotCollections.Array<StatescriptConnection> connectionsToAdd)
	{
		if (_graph is null || NodeResource is null || _typeInfo is null)
		{
			return;
		}

		var merged = new GodotCollections.Dictionary<string, Variant>();

		foreach (KeyValuePair<string, Variant> entry in NodeResource.CustomData)
		{
			merged[entry.Key] = entry.Value;
		}

		foreach (KeyValuePair<Variant, Variant> entry in newData)
		{
			string key = entry.Key.AsString();

			if (entry.Value.VariantType == Variant.Type.Nil)
			{
				merged.Remove(key);
				continue;
			}

			merged[key] = entry.Value;
		}

		StatescriptNodeDiscovery.NodeTypeInfo? newTypeInfo =
			StatescriptNodeDiscovery.FindForConfiguration(NodeResource.RuntimeTypeName, merged);

		if (newTypeInfo is null)
		{
			return;
		}

		int inputCount = newTypeInfo.InputPortLabels.Length;
		int outputCount = newTypeInfo.OutputPortLabels.Length;
		string nodeId = NodeResource.NodeId;

		foreach (StatescriptConnection connection in _graph.Connections)
		{
			if (connection.ToNode == nodeId && connection.InputPort >= inputCount)
			{
				connectionsToRemove.Add(connection);
				continue;
			}

			if (connection.FromNode != nodeId)
			{
				continue;
			}

			int newOutputPort = _activeCustomEditor?.RemapOutputPort(connection.OutputPort, _typeInfo, newTypeInfo)
				?? connection.OutputPort;

			if (newOutputPort == connection.OutputPort && newOutputPort < outputCount)
			{
				continue;
			}

			connectionsToRemove.Add(connection);

			if (newOutputPort < 0 || newOutputPort >= outputCount)
			{
				continue;
			}

			connectionsToAdd.Add(new StatescriptConnection
			{
				FromNode = connection.FromNode,
				OutputPort = newOutputPort,
				ToNode = connection.ToNode,
				InputPort = connection.InputPort,
			});
		}
	}

	/// <summary>
	/// Applies a port-layout configuration together with the connection changes it implies. Detaching runs against the
	/// layout in place when the call starts and attaching against the one it leaves behind, which is what lets undo
	/// reuse this method with the two connection sets swapped.
	/// </summary>
	/// <param name="customData">The CustomData entries to write.</param>
	/// <param name="connectionsToRemove">Connections to detach before the node is laid out again.</param>
	/// <param name="connectionsToAdd">Connections to attach after the node has been laid out.</param>
	private void ApplyLayoutConfigCore(
		GodotCollections.Dictionary customData,
		GodotCollections.Array<StatescriptConnection> connectionsToRemove,
		GodotCollections.Array<StatescriptConnection> connectionsToAdd)
	{
		SetVisualConnections(connectionsToRemove, connected: false);

		foreach (StatescriptConnection connection in connectionsToRemove)
		{
			_graph?.Connections.Remove(connection);
		}

		WriteCustomDataEntries(customData);
		NotifyGraphResourceChanged();
		RebuildNode();

		if (_graph is not null)
		{
			foreach (StatescriptConnection connection in connectionsToAdd)
			{
				if (!_graph.Connections.Contains(connection))
				{
					_graph.Connections.Add(connection);
				}
			}
		}

		SetVisualConnections(connectionsToAdd, connected: true);
		NotifyGraphResourceChanged();
	}

	/// <summary>
	/// Connects or disconnects the given connections in the owning <see cref="GraphEdit"/>, translating runtime port
	/// indices through each source node's current visual mapping. Does nothing while the node is detached (a background
	/// tab), where the visuals are rebuilt from the resource when the tab is shown again.
	/// </summary>
	/// <param name="connections">The connections to apply.</param>
	/// <param name="connected">Whether to connect or disconnect them.</param>
	private void SetVisualConnections(
		GodotCollections.Array<StatescriptConnection> connections,
		bool connected)
	{
		if (connections.Count == 0 || GetParent() is not GraphEdit graphEdit)
		{
			return;
		}

		foreach (StatescriptConnection connection in connections)
		{
			int visualOutputPort = graphEdit.GetNodeOrNull(connection.FromNode) is StatescriptGraphNode fromNode
				? fromNode.RuntimeToVisualOutputPort(connection.OutputPort)
				: connection.OutputPort;

			if (connected)
			{
				graphEdit.ConnectNode(
					connection.FromNode,
					visualOutputPort,
					connection.ToNode,
					connection.InputPort);
			}
			else
			{
				graphEdit.DisconnectNode(
					connection.FromNode,
					visualOutputPort,
					connection.ToNode,
					connection.InputPort);
			}
		}
	}

	private void WriteCustomDataEntries(GodotCollections.Dictionary customData)
	{
		if (NodeResource is null)
		{
			return;
		}

		foreach (KeyValuePair<Variant, Variant> entry in customData)
		{
			string key = entry.Key.AsString();

			if (entry.Value.VariantType == Variant.Type.Nil)
			{
				NodeResource.CustomData.Remove(key);
				continue;
			}

			NodeResource.CustomData[key] = entry.Value;
		}
	}

	private void ApplyInputPropertyConfigCore(
		GodotCollections.Dictionary customData,
		int propertyIndex,
		Variant resolverVariant)
	{
		if (NodeResource is null)
		{
			return;
		}

		WriteCustomDataEntries(customData);

		if (resolverVariant.VariantType == Variant.Type.Nil)
		{
			RemoveBinding(StatescriptPropertyDirection.Input, propertyIndex);
		}
		else
		{
			EnsureBinding(StatescriptPropertyDirection.Input, propertyIndex).Resolver =
				resolverVariant.AsGodotObject() as StatescriptResolverResource;
		}

		EnsurePropertyVisible(StatescriptPropertyDirection.Input, propertyIndex);
		NotifyGraphResourceChanged();
		RebuildNode();
	}

	private void NotifyGraphResourceChanged()
	{
		NodeResource?.EmitChanged();
		_graph?.EmitChanged();
	}

	private void RebuildNode()
	{
		if (NodeResource is null)
		{
			return;
		}

		EditorUndoRedoManager? savedUndoRedo = _undoRedo;
		Initialize(NodeResource, _graph);
		_undoRedo = savedUndoRedo;

		// Re-fit to the freshly rebuilt content (respecting the floor) rather than preserving the previous width.
		ResetSize();
	}

	private void RefreshHighlightState()
	{
		bool hasActiveSelection = !string.IsNullOrEmpty(_highlightedVariableName)
			|| (!string.IsNullOrEmpty(_highlightedSharedVariableSetPath)
				&& !string.IsNullOrEmpty(_highlightedSharedVariableName));

		// Refreshes run on every binding change across all visible nodes. When no variable is selected and the last
		// pass already cleared the styles, the recursive re-style walk would be a no-op, so skip it entirely.
		if (!hasActiveSelection && !_highlightStylesApplied)
		{
			_isHighlighted = false;
			return;
		}

		_isHighlighted = (!string.IsNullOrEmpty(_highlightedVariableName)
			&& ReferencesVariable(_highlightedVariableName))
			|| ReferencesSharedVariable(_highlightedSharedVariableSetPath, _highlightedSharedVariableName);
		ApplyHighlightBorder();
		UpdateChildHighlights();
		_highlightStylesApplied = hasActiveSelection;
	}

	private StatescriptNodeProperty? FindBinding(
		StatescriptPropertyDirection direction,
		int propertyIndex)
	{
		if (NodeResource is null)
		{
			return null;
		}

		foreach (StatescriptNodeProperty binding in NodeResource.PropertyBindings)
		{
			if (binding.Direction == direction && binding.PropertyIndex == propertyIndex)
			{
				return binding;
			}
		}

		return null;
	}

	private StatescriptNodeProperty EnsureBinding(
		StatescriptPropertyDirection direction,
		int propertyIndex)
	{
		StatescriptNodeProperty? binding = FindBinding(direction, propertyIndex);

		if (binding is null)
		{
			binding = new StatescriptNodeProperty
			{
				Direction = direction,
				PropertyIndex = propertyIndex,
			};

			NodeResource!.PropertyBindings.Add(binding);
			NotifyGraphResourceChanged();
		}

		return binding;
	}

	private void RemoveBinding(StatescriptPropertyDirection direction, int propertyIndex)
	{
		if (NodeResource is null)
		{
			return;
		}

		bool removedAny = false;
		for (int i = NodeResource.PropertyBindings.Count - 1; i >= 0; i--)
		{
			StatescriptNodeProperty binding = NodeResource.PropertyBindings[i];

			if (binding.Direction == direction && binding.PropertyIndex == propertyIndex)
			{
				NodeResource.PropertyBindings.RemoveAt(i);
				removedAny = true;
			}
		}

		if (removedAny)
		{
			NotifyGraphResourceChanged();
		}
	}

	private sealed record PendingInputConfig(GodotCollections.Dictionary CustomData, string ActionName);
}

/// <summary>
/// Identifies a property binding slot by direction and index.
/// </summary>
/// <param name="Direction">The direction of the property (input or output).</param>
/// <param name="PropertyIndex">The index of the property within its direction.</param>
internal readonly record struct PropertySlotKey(StatescriptPropertyDirection Direction, int PropertyIndex);
#endif
