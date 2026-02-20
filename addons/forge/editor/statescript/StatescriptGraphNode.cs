// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using System.Collections.Generic;
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
	private static readonly Color _entryExitColor = new(0x2a4a8dff);
	private static readonly Color _actionColor = new(0x3a7856ff);
	private static readonly Color _conditionColor = new(0x99811fff);
	private static readonly Color _stateColor = new(0xa52c38ff);
	private static readonly Color _eventColor = new(0xabb2bfff);
	private static readonly Color _subgraphColor = new(0xc678ddff);
	private static readonly Color _inputPropertyColor = new(0x61afefff);
	private static readonly Color _outputVariableColor = new(0xe5c07bff);

	private StatescriptNodeDiscovery.NodeTypeInfo? _typeInfo;
	private StatescriptGraph? _graph;

	private readonly Dictionary<(StatescriptPropertyDirection, int), IStatescriptResolverEditor> _activeResolverEditors
		= [];

	/// <summary>
	/// Raised when a property binding has been modified in the UI.
	/// </summary>
	public event Action? PropertyBindingChanged;

	/// <summary>
	/// Gets the underlying node resource.
	/// </summary>
	public StatescriptNode? NodeResource { get; private set; }

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

		ClearSlots();

		if (resource.NodeType is StatescriptNodeType.Entry or StatescriptNodeType.Exit
			|| string.IsNullOrEmpty(resource.RuntimeTypeName))
		{
			SetupNodeByType(resource.NodeType);
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
	}

	private static string GetResolverTypeId(StatescriptResolverResource resolver)
	{
		return resolver switch
		{
			VariableResolverResource => "Variable",
			VariantResolverResource => "Variant",
			_ => string.Empty,
		};
	}

	private static void ClearContainer(Control container)
	{
		foreach (Node child in container.GetChildren())
		{
			child.QueueFree();
		}
	}

	private void SetupFromTypeInfo(StatescriptNodeDiscovery.NodeTypeInfo typeInfo)
	{
		// Port slots.
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

		if (typeInfo.InputPropertiesInfo.Length > 0)
		{
			AddPropertySectionDivider("Input Properties", _inputPropertyColor);

			for (var i = 0; i < typeInfo.InputPropertiesInfo.Length; i++)
			{
				AddInputPropertyRow(typeInfo.InputPropertiesInfo[i], i);
			}
		}

		if (typeInfo.OutputVariablesInfo.Length > 0)
		{
			AddPropertySectionDivider("Output Variables", _outputVariableColor);

			for (var i = 0; i < typeInfo.OutputVariablesInfo.Length; i++)
			{
				AddOutputVariableRow(typeInfo.OutputVariablesInfo[i], i);
			}
		}

		Color titleColor = typeInfo.NodeType switch
		{
			StatescriptNodeType.Action => _actionColor,
			StatescriptNodeType.Condition => _conditionColor,
			StatescriptNodeType.State => _stateColor,
			StatescriptNodeType.Entry => throw new NotImplementedException(),
			StatescriptNodeType.Exit => throw new NotImplementedException(),
			_ => _entryExitColor,
		};

		ApplyTitleBarColor(titleColor);
	}

	private void AddPropertySectionDivider(string sectionTitle, Color color)
	{
		var divider = new HSeparator { CustomMinimumSize = new Vector2(0, 4) };
		AddChild(divider);

		var sectionLabel = new Label
		{
			Text = sectionTitle,
			HorizontalAlignment = HorizontalAlignment.Center,
		};

		sectionLabel.AddThemeColorOverride("font_color", color);
		sectionLabel.AddThemeFontSizeOverride("font_size", 11);
		AddChild(sectionLabel);
	}

	private void AddInputPropertyRow(StatescriptNodeDiscovery.InputPropertyInfo propInfo, int index)
	{
		if (NodeResource is null)
		{
			return;
		}

		var container = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		AddChild(container);

		var headerRow = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		container.AddChild(headerRow);

		var nameLabel = new Label
		{
			Text = propInfo.Label,
			CustomMinimumSize = new Vector2(60, 0),
		};

		nameLabel.AddThemeColorOverride("font_color", _inputPropertyColor);
		headerRow.AddChild(nameLabel);

		// Build the list of compatible resolvers from the registry.
		List<IStatescriptResolverEditor> resolvers =
			StatescriptResolverRegistry.GetCompatibleResolvers(propInfo.ExpectedType);

		// Resolver type dropdown: "(None)" + one entry per compatible resolver.
		var resolverDropdown = new OptionButton
		{
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			CustomMinimumSize = new Vector2(80, 0),
		};

		resolverDropdown.AddItem("(None)");

		foreach (IStatescriptResolverEditor resolver in resolvers)
		{
			resolverDropdown.AddItem(resolver.DisplayName);
		}

		// Determine which resolver is currently selected from the existing binding.
		StatescriptNodeProperty? binding = FindBinding(StatescriptPropertyDirection.Input, index);
		var selectedIndex = 0;

		if (binding?.Resolver is not null)
		{
			for (var i = 0; i < resolvers.Count; i++)
			{
				if (resolvers[i].ResolverTypeId == GetResolverTypeId(binding.Resolver))
				{
					selectedIndex = i + 1;
					break;
				}
			}
		}

		resolverDropdown.Selected = selectedIndex;
		headerRow.AddChild(resolverDropdown);

		// Container for the resolver-specific configuration UI.
		var resolverUIContainer = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		container.AddChild(resolverUIContainer);

		// Show the resolver UI if one is already bound.
		if (selectedIndex > 0)
		{
			ShowResolverEditorUI(
				resolvers[selectedIndex - 1],
				binding,
				propInfo.ExpectedType,
				resolverUIContainer,
				StatescriptPropertyDirection.Input,
				index);
		}

		resolverDropdown.ItemSelected += x =>
		{
			ClearContainer(resolverUIContainer);
			_activeResolverEditors.Remove((StatescriptPropertyDirection.Input, index));

			if (NodeResource is null)
			{
				return;
			}

			if (x == 0)
			{
				// "(None)" selected — clear the binding.
				RemoveBinding(StatescriptPropertyDirection.Input, index);
				PropertyBindingChanged?.Invoke();
				return;
			}

			IStatescriptResolverEditor resolverEditor = resolvers[(int)x - 1];

			ShowResolverEditorUI(
				resolverEditor,
				null,
				propInfo.ExpectedType,
				resolverUIContainer,
				StatescriptPropertyDirection.Input,
				index);

			// Save immediately so the resource stays in sync.
			SaveResolverEditor(resolverEditor, StatescriptPropertyDirection.Input, index);
			PropertyBindingChanged?.Invoke();
		};
	}

	private void AddOutputVariableRow(StatescriptNodeDiscovery.OutputVariableInfo varInfo, int index)
	{
		if (NodeResource is null || _graph is null)
		{
			return;
		}

		var hBox = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		AddChild(hBox);

		var nameLabel = new Label
		{
			Text = varInfo.Label,
			CustomMinimumSize = new Vector2(60, 0),
		};

		nameLabel.AddThemeColorOverride("font_color", _outputVariableColor);
		hBox.AddChild(nameLabel);

		// Output variables always bind to a graph variable (VariableResolverEditor).
		var variableDropdown = new OptionButton { SizeFlagsHorizontal = SizeFlags.ExpandFill };

		variableDropdown.AddItem("(None)");

		foreach (StatescriptGraphVariable v in _graph.Variables)
		{
			variableDropdown.AddItem(v.VariableName);
		}

		// Restore selection from existing binding.
		StatescriptNodeProperty? binding = FindBinding(StatescriptPropertyDirection.Output, index);
		var selectedIndex = 0;

		if (binding?.Resolver is VariableResolverResource varRes
			&& !string.IsNullOrEmpty(varRes.VariableName))
		{
			for (var i = 0; i < _graph.Variables.Count; i++)
			{
				if (_graph.Variables[i].VariableName == varRes.VariableName)
				{
					selectedIndex = i + 1;
					break;
				}
			}
		}

		variableDropdown.Selected = selectedIndex;

		variableDropdown.ItemSelected += x =>
		{
			if (NodeResource is null || _graph is null)
			{
				return;
			}

			if (x == 0)
			{
				RemoveBinding(StatescriptPropertyDirection.Output, index);
			}
			else
			{
				var variableName = _graph.Variables[(int)x - 1].VariableName;
				EnsureBinding(StatescriptPropertyDirection.Output, index).Resolver = new VariableResolverResource { VariableName = variableName };
			}

			PropertyBindingChanged?.Invoke();
		};

		hBox.AddChild(variableDropdown);
	}

	private void ShowResolverEditorUI(
		IStatescriptResolverEditor resolverEditor,
		StatescriptNodeProperty? existingBinding,
		Type expectedType,
		VBoxContainer container,
		StatescriptPropertyDirection direction,
		int propertyIndex)
	{
		if (_graph is null)
		{
			return;
		}

		Control? editorControl = resolverEditor.CreateEditorUI(
			_graph,
			existingBinding,
			expectedType,
			() =>
			{
				SaveResolverEditor(resolverEditor, direction, propertyIndex);
				PropertyBindingChanged?.Invoke();
			});

		if (editorControl is not null)
		{
			container.AddChild(editorControl);
		}

		_activeResolverEditors[(direction, propertyIndex)] = resolverEditor;
	}

	private void SaveResolverEditor(
		IStatescriptResolverEditor resolverEditor,
		StatescriptPropertyDirection direction,
		int propertyIndex)
	{
		if (NodeResource is null)
		{
			return;
		}

		StatescriptNodeProperty binding = EnsureBinding(direction, propertyIndex);
		resolverEditor.SaveTo(binding);
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

	private void SetupNodeByType(StatescriptNodeType nodeType)
	{
		switch (nodeType)
		{
			case StatescriptNodeType.Entry:
				SetupEntryNode();
				break;
			case StatescriptNodeType.Exit:
				SetupExitNode();
				break;
			case StatescriptNodeType.Action:
				SetupActionNode();
				break;
			case StatescriptNodeType.Condition:
				SetupConditionNode();
				break;
			case StatescriptNodeType.State:
				SetupStateNode();
				break;
		}
	}

	private void SetupEntryNode()
	{
		var label = new Label { Text = "Start" };
		AddChild(label);
		SetSlotEnabledRight(0, true);
		SetSlotColorRight(0, _eventColor);

		ApplyTitleBarColor(_entryExitColor);
	}

	private void SetupExitNode()
	{
		var label = new Label { Text = "End" };
		AddChild(label);
		SetSlotEnabledLeft(0, true);
		SetSlotColorLeft(0, _eventColor);

		ApplyTitleBarColor(_entryExitColor);
	}

	private void SetupActionNode()
	{
		var label = new Label { Text = "Execute" };
		AddChild(label);
		SetSlotEnabledLeft(0, true);
		SetSlotColorLeft(0, _eventColor);
		SetSlotEnabledRight(0, true);
		SetSlotColorRight(0, _eventColor);

		ApplyTitleBarColor(_actionColor);
	}

	private void SetupConditionNode()
	{
		var hBox = new HBoxContainer();
		hBox.AddThemeConstantOverride("separation", 16);
		AddChild(hBox);

		var inputLabel = new Label { Text = "Condition" };
		hBox.AddChild(inputLabel);
		SetSlotEnabledLeft(0, true);
		SetSlotColorLeft(0, _eventColor);

		var trueLabel = new Label
		{
			Text = "True",
			HorizontalAlignment = HorizontalAlignment.Right,
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
		};

		hBox.AddChild(trueLabel);
		SetSlotEnabledRight(0, true);
		SetSlotColorRight(0, _eventColor);

		var falseLabel = new Label
		{
			Text = "False",
			HorizontalAlignment = HorizontalAlignment.Right,
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
		};

		AddChild(falseLabel);
		SetSlotEnabledRight(1, true);
		SetSlotColorRight(1, _eventColor);
		ApplyTitleBarColor(_conditionColor);
	}

	private void SetupStateNode()
	{
		var hBox1 = new HBoxContainer();
		hBox1.AddThemeConstantOverride("separation", 16);
		AddChild(hBox1);

		var inputLabel = new Label { Text = "Begin" };
		hBox1.AddChild(inputLabel);
		SetSlotEnabledLeft(0, true);
		SetSlotColorLeft(0, _eventColor);

		var activateLabel = new Label
		{
			Text = "OnActivate",
			HorizontalAlignment = HorizontalAlignment.Right,
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
		};

		hBox1.AddChild(activateLabel);
		SetSlotEnabledRight(0, true);
		SetSlotColorRight(0, _eventColor);

		var hBox2 = new HBoxContainer();
		hBox2.AddThemeConstantOverride("separation", 16);
		AddChild(hBox2);

		var abortLabel = new Label { Text = "Abort" };
		hBox2.AddChild(abortLabel);
		SetSlotEnabledLeft(1, true);
		SetSlotColorLeft(1, _eventColor);

		var deactivateLabel = new Label
		{
			Text = "OnDeactivate",
			HorizontalAlignment = HorizontalAlignment.Right,
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
		};

		hBox2.AddChild(deactivateLabel);
		SetSlotEnabledRight(1, true);
		SetSlotColorRight(1, _eventColor);

		var abortOutputLabel = new Label
		{
			Text = "OnAbort",
			HorizontalAlignment = HorizontalAlignment.Right,
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
		};

		AddChild(abortOutputLabel);
		SetSlotEnabledRight(2, true);
		SetSlotColorRight(2, _eventColor);

		var subgraphLabel = new Label
		{
			Text = "Subgraph",
			HorizontalAlignment = HorizontalAlignment.Right,
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
		};

		AddChild(subgraphLabel);
		SetSlotEnabledRight(3, true);
		SetSlotColorRight(3, _subgraphColor);

		ApplyTitleBarColor(_stateColor);
	}

	private void ClearSlots()
	{
		foreach (Node child in GetChildren())
		{
			child.QueueFree();
		}
	}

	private void ApplyTitleBarColor(Color color)
	{
		var titleBarStyleBox = new StyleBoxFlat
		{
			BgColor = color,
			ContentMarginLeft = 12,
			ContentMarginRight = 12,
			ContentMarginTop = 6,
			ContentMarginBottom = 6,
			CornerRadiusTopLeft = 4,
			CornerRadiusTopRight = 4,
		};

		AddThemeStyleboxOverride("titlebar", titleBarStyleBox);

		var selectedTitleBarStyleBox = (StyleBoxFlat)titleBarStyleBox.Duplicate();
		selectedTitleBarStyleBox.BgColor = color.Lightened(0.2f);
		AddThemeStyleboxOverride("titlebar_selected", selectedTitleBarStyleBox);
	}
}
#endif
