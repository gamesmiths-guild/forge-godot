// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Statescript;

/// <summary>
/// Visual GraphNode representation for a single Statescript node in the editor.
/// Supports both built-in node types (Entry/Exit) and dynamically discovered concrete types.
/// </summary>
[Tool]
public partial class StatescriptGraphNode : GraphNode
{
	private const string FoldInputKey = "_fold_input";
	private const string FoldOutputKey = "_fold_output";
	private const string CustomWidthKey = "_custom_width";

	private static readonly Color _entryColor = new(0x2a4a8dff);
	private static readonly Color _exitColor = new(0x8a549aff);
	private static readonly Color _actionColor = new(0x3a7856ff);
	private static readonly Color _conditionColor = new(0x99811fff);
	private static readonly Color _stateColor = new(0xa52c38ff);
	private static readonly Color _eventColor = new(0xabb2bfff);
	private static readonly Color _subgraphColor = new(0xc678ddff);
	private static readonly Color _inputPropertyColor = new(0x61afefff);
	private static readonly Color _outputVariableColor = new(0xe5c07bff);
	private static readonly Color _highlightColor = new(0x56b6c2ff);

	private readonly Dictionary<PropertySlotKey, NodeEditorProperty> _activeResolverEditors = [];

	private StatescriptNodeDiscovery.NodeTypeInfo? _typeInfo;
	private StatescriptGraph? _graph;
	private EditorUndoRedoManager? _undoRedo;
	private bool _resizeConnected;
	private float _widthBeforeResize;
	private string? _highlightedVariableName;
	private bool _isHighlighted;

	/// <summary>
	/// Raised when a property binding has been modified in the UI.
	/// </summary>
	public event Action? PropertyBindingChanged;

	/// <summary>
	/// Gets the underlying node resource.
	/// </summary>
	public StatescriptNode? NodeResource { get; private set; }

	/// <summary>
	/// Sets the <see cref="EditorUndoRedoManager"/> used for undo/redo support.
	/// </summary>
	/// <param name="undoRedo">The undo/redo manager from the editor plugin.</param>
	public void SetUndoRedo(EditorUndoRedoManager? undoRedo)
	{
		_undoRedo = undoRedo;
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
		_isHighlighted = !string.IsNullOrEmpty(variableName) && ReferencesVariable(variableName!);
		ApplyHighlightBorder();
		UpdateChildHighlights();
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
		_activeResolverEditors.Clear();

		Name = resource.NodeId;
		Title = resource.Title;
		PositionOffset = resource.PositionOffset;
		CustomMinimumSize = new Vector2(240, 0);
		Resizable = true;

		RestoreCustomWidth();

		if (!_resizeConnected)
		{
			_widthBeforeResize = CustomMinimumSize.X;
			ResizeRequest += OnResizeRequest;
			ResizeEnd += OnResizeEnd;
			_resizeConnected = true;
		}

		ClearSlots();

		if (resource.NodeType is StatescriptNodeType.Entry or StatescriptNodeType.Exit
			|| string.IsNullOrEmpty(resource.RuntimeTypeName))
		{
			SetupNodeByType(resource.NodeType);
			ApplyBottomPadding();
			return;
		}

		_typeInfo = StatescriptNodeDiscovery.FindByRuntimeTypeName(resource.RuntimeTypeName);
		if (_typeInfo is not null)
		{
			SetupFromTypeInfo(_typeInfo);
		}
		else
		{
			SetupNodeByType(resource.NodeType);
		}

		ApplyBottomPadding();
	}

	internal FoldableContainer AddPropertySectionDividerInternal(
		string sectionTitle,
		Color color,
		string foldKey,
		bool folded)
	{
		return AddPropertySectionDivider(sectionTitle, color, foldKey, folded);
	}

	internal void AddInputPropertyRowInternal(
		StatescriptNodeDiscovery.InputPropertyInfo propInfo,
		int index,
		Control container)
	{
		AddInputPropertyRow(propInfo, index, container);
	}

	internal void AddOutputVariableRowInternal(
		StatescriptNodeDiscovery.OutputVariableInfo varInfo,
		int index,
		FoldableContainer container)
	{
		AddOutputVariableRow(varInfo, index, container);
	}

	internal bool GetFoldStateInternal(string key)
	{
		return GetFoldState(key);
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
		if (_undoRedo is null)
		{
			return;
		}

		_undoRedo.CreateAction(actionName, customContext: _graph);
		_undoRedo.AddDoMethod(
			this,
			MethodName.ApplyResolverBinding,
			(int)direction,
			propertyIndex,
			newResolver ?? new StatescriptResolverResource());
		_undoRedo.AddUndoMethod(
			this,
			MethodName.ApplyResolverBinding,
			(int)direction,
			propertyIndex,
			oldResolver ?? new StatescriptResolverResource());
		_undoRedo.CommitAction(false);
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

	private static string GetResolverTypeId(StatescriptResolverResource resolver)
	{
		return resolver switch
		{
			VariableResolverResource => "Variable",
			VariantResolverResource => "Variant",
			AttributeResolverResource => "Attribute",
			TagResolverResource => "Tag",
			ComparisonResolverResource => "Comparison",
			_ => string.Empty,
		};
	}

	private static void ClearContainer(Control container)
	{
		foreach (Node child in container.GetChildren())
		{
			container.RemoveChild(child);
			child.QueueFree();
		}
	}

	private void SetupFromTypeInfo(StatescriptNodeDiscovery.NodeTypeInfo typeInfo)
	{
		var maxSlots = Math.Max(typeInfo.InputPortLabels.Length, typeInfo.OutputPortLabels.Length);

		for (var slot = 0; slot < maxSlots; slot++)
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

			if (slot < typeInfo.OutputPortLabels.Length)
			{
				var outputLabel = new Label
				{
					Text = typeInfo.OutputPortLabels[slot],
					HorizontalAlignment = HorizontalAlignment.Right,
					SizeFlagsHorizontal = SizeFlags.ExpandFill,
				};

				hBox.AddChild(outputLabel);
				SetSlotEnabledRight(slot, true);
				Color portColor = typeInfo.IsSubgraphPort[slot] ? _subgraphColor : _eventColor;
				SetSlotColorRight(slot, portColor);
			}
		}

		if (CustomNodeEditorRegistry.TryGet(typeInfo.RuntimeTypeName, out CustomNodeEditor? customEditor))
		{
			Debug.Assert(_graph is not null, "Graph context is required for custom node editors.");
			Debug.Assert(NodeResource is not null, "Node resource is required for custom node editors.");

			customEditor.Bind(this, _graph, NodeResource, _activeResolverEditors);
			customEditor.BuildPropertySections(typeInfo);
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
			var folded = GetFoldState(FoldInputKey);
			FoldableContainer inputContainer = AddPropertySectionDivider(
				"Input Properties",
				_inputPropertyColor,
				FoldInputKey,
				folded);

			for (var i = 0; i < typeInfo.InputPropertiesInfo.Length; i++)
			{
				AddInputPropertyRow(typeInfo.InputPropertiesInfo[i], i, inputContainer);
			}
		}

		if (typeInfo.OutputVariablesInfo.Length > 0)
		{
			var folded = GetFoldState(FoldOutputKey);
			FoldableContainer outputContainer = AddPropertySectionDivider(
				"Output Variables",
				_outputVariableColor,
				FoldOutputKey,
				folded);

			for (var i = 0; i < typeInfo.OutputVariablesInfo.Length; i++)
			{
				AddOutputVariableRow(typeInfo.OutputVariablesInfo[i], i, outputContainer);
			}
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
		};

		sectionContainer.AddThemeColorOverride("font_color", color);

		sectionContainer.FoldingChanged += isFolded =>
		{
			SetFoldStateWithUndo(foldKey, isFolded);
			ResetSize();
		};

		AddChild(sectionContainer);

		return sectionContainer;
	}

	private bool GetFoldState(string key)
	{
		if (NodeResource is not null && NodeResource.CustomData.TryGetValue(key, out Variant value))
		{
			return value.AsBool();
		}

		return false;
	}

	private void SetFoldState(string key, bool folded)
	{
		if (NodeResource is null)
		{
			return;
		}

		NodeResource.CustomData[key] = Variant.From(folded);
	}

	private void SetFoldStateWithUndo(string key, bool folded)
	{
		if (NodeResource is null)
		{
			return;
		}

		var oldFolded = GetFoldState(key);

		if (oldFolded == folded)
		{
			return;
		}

		SetFoldState(key, folded);

		if (_undoRedo is not null)
		{
			_undoRedo.CreateAction("Toggle Fold", customContext: _graph);
			_undoRedo.AddDoMethod(
				this,
				MethodName.ApplyFoldState,
				key,
				folded);
			_undoRedo.AddUndoMethod(
				this,
				MethodName.ApplyFoldState,
				key,
				oldFolded);
			_undoRedo.CommitAction(false);
		}
	}

	private void ApplyFoldState(string key, bool folded)
	{
		SetFoldState(key, folded);
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
		var newWidth = CustomMinimumSize.X;

		if (_undoRedo is not null && NodeResource is not null
			&& !Mathf.IsEqualApprox(_widthBeforeResize, newWidth))
		{
			var oldWidth = _widthBeforeResize;

			_undoRedo.CreateAction("Resize Node", customContext: _graph);
			_undoRedo.AddDoMethod(
				this,
				MethodName.ApplyCustomWidth,
				newWidth);
			_undoRedo.AddUndoMethod(
				this,
				MethodName.ApplyCustomWidth,
				oldWidth);
			_undoRedo.CommitAction(false);
		}

		_widthBeforeResize = newWidth;
	}

	private void ApplyCustomWidth(float width)
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
			var width = (float)value.AsDouble();

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
	}

	private void ApplyResolverBinding(
		int directionInt,
		int propertyIndex,
		StatescriptResolverResource resolver)
	{
		if (NodeResource is null)
		{
			return;
		}

		var direction = (StatescriptPropertyDirection)directionInt;
		StatescriptNodeProperty binding = EnsureBinding(direction, propertyIndex);
		binding.Resolver = resolver;
		RebuildNode();
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
		Size = new Vector2(Size.X, 0);
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
		}

		return binding;
	}

	private void RemoveBinding(StatescriptPropertyDirection direction, int propertyIndex)
	{
		if (NodeResource is null)
		{
			return;
		}

		for (var i = NodeResource.PropertyBindings.Count - 1; i >= 0; i--)
		{
			StatescriptNodeProperty binding = NodeResource.PropertyBindings[i];

			if (binding.Direction == direction && binding.PropertyIndex == propertyIndex)
			{
				NodeResource.PropertyBindings.RemoveAt(i);
			}
		}
	}
}

/// <summary>
/// Identifies a property binding slot by direction and index.
/// </summary>
/// <param name="Direction">The direction of the property (input or output).</param>
/// <param name="PropertyIndex">The index of the property within its direction.</param>
internal readonly record struct PropertySlotKey(StatescriptPropertyDirection Direction, int PropertyIndex);
#endif
