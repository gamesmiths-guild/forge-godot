// Copyright © Gamesmiths Guild.

#if TOOLS
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Statescript;

/// <summary>
/// Visual GraphNode representation for a single Statescript node in the editor.
/// Supports both built-in node types (Entry/Exit) and dynamically discovered concrete types.
/// </summary>
[Tool]
public partial class StatescriptGraphNode : GraphNode
{
	// Node type colors.
	private static readonly Color _entryExitColor = new(0x2a4a8dff);
	private static readonly Color _actionColor = new(0x3a7856ff);
	private static readonly Color _conditionColor = new(0x99811fff);
	private static readonly Color _stateColor = new(0xa52c38ff);

	private static readonly Color _eventColor = new(0xabb2bfff);
	private static readonly Color _subgraphColor = new(0xc678ddff);

	/// <summary>
	/// Gets the underlying node resource.
	/// </summary>
	public StatescriptNode? NodeResource { get; private set; }

	/// <summary>
	/// Initializes this graph node from a <see cref="StatescriptNode"/>.
	/// </summary>
	/// <param name="resource">The node resource to visualize.</param>
	public void Initialize(StatescriptNode resource)
	{
		NodeResource = resource;
		Name = resource.NodeId;
		Title = resource.Title;
		PositionOffset = resource.PositionOffset;

		ClearSlots();

		// For Entry/Exit nodes or nodes without a runtime type, use the fixed layout.
		if (resource.NodeType is StatescriptNodeType.Entry or StatescriptNodeType.Exit
			|| string.IsNullOrEmpty(resource.RuntimeTypeName))
		{
			SetupNodeByType(resource.NodeType);
			return;
		}

		// For concrete types, use reflection-based port discovery.
		StatescriptNodeDiscovery.NodeTypeInfo? typeInfo =
			StatescriptNodeDiscovery.FindByRuntimeTypeName(resource.RuntimeTypeName);

		if (typeInfo is not null)
		{
			SetupFromTypeInfo(typeInfo);
		}
		else
		{
			// Fallback to default layout if the type can't be resolved.
			SetupNodeByType(resource.NodeType);
		}
	}

	private void SetupFromTypeInfo(StatescriptNodeDiscovery.NodeTypeInfo typeInfo)
	{
		var maxSlots = System.Math.Max(typeInfo.InputPortLabels.Length, typeInfo.OutputPortLabels.Length);

		for (var slot = 0; slot < maxSlots; slot++)
		{
			var hBox = new HBoxContainer();
			hBox.AddThemeConstantOverride("separation", 16);
			AddChild(hBox);

			// Left side: input port label.
			if (slot < typeInfo.InputPortLabels.Length)
			{
				var inputLabel = new Label { Text = typeInfo.InputPortLabels[slot] };
				hBox.AddChild(inputLabel);
				SetSlotEnabledLeft(slot, true);
				SetSlotColorLeft(slot, _eventColor);
			}
			else
			{
				// Spacer to keep alignment.
				var spacer = new Control();
				hBox.AddChild(spacer);
			}

			// Right side: output port label.
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

		// Apply title bar color based on category.
		Color titleColor = typeInfo.NodeType switch
		{
			StatescriptNodeType.Action => _actionColor,
			StatescriptNodeType.Condition => _conditionColor,
			StatescriptNodeType.State => _stateColor,
			StatescriptNodeType.Entry => throw new System.NotImplementedException(),
			StatescriptNodeType.Exit => throw new System.NotImplementedException(),
			_ => _entryExitColor,
		};

		ApplyTitleBarColor(titleColor);
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

		// Input port.
		var inputLabel = new Label { Text = "Condition" };
		hBox.AddChild(inputLabel);
		SetSlotEnabledLeft(0, true);
		SetSlotColorLeft(0, _eventColor);

		// True output port.
		var trueLabel = new Label
		{
			Text = "True",
			HorizontalAlignment = HorizontalAlignment.Right,
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
		};
		hBox.AddChild(trueLabel);
		SetSlotEnabledRight(0, true);
		SetSlotColorRight(0, _eventColor);

		// False output port.
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

		// Input port.
		var inputLabel = new Label { Text = "Begin" };
		hBox1.AddChild(inputLabel);
		SetSlotEnabledLeft(0, true);
		SetSlotColorLeft(0, _eventColor);

		var hBox2 = new HBoxContainer();
		AddChild(hBox2);

		// Abort port.
		var abortLabel = new Label { Text = "Abort" };
		hBox2.AddThemeConstantOverride("separation", 16);
		hBox2.AddChild(abortLabel);
		SetSlotEnabledLeft(0, true);
		SetSlotColorLeft(0, _eventColor);

		// OnActivate output port.
		var activateLabel = new Label
		{
			Text = "OnActivate",
			HorizontalAlignment = HorizontalAlignment.Right,
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
		};
		hBox1.AddChild(activateLabel);
		SetSlotEnabledRight(0, true);
		SetSlotColorRight(0, _eventColor);

		// OnDeactivate output port.
		var deactivateLabel = new Label
		{
			Text = "OnDeactivate",
			HorizontalAlignment = HorizontalAlignment.Right,
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
		};
		hBox2.AddChild(deactivateLabel);
		SetSlotEnabledRight(1, true);
		SetSlotColorRight(1, _eventColor);

		// OnAbort output port.
		var abortOutputLabel = new Label
		{
			Text = "OnAbort",
			HorizontalAlignment = HorizontalAlignment.Right,
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
		};
		AddChild(abortOutputLabel);
		SetSlotEnabledRight(2, true);
		SetSlotColorRight(2, _eventColor);

		// Subgraph output port.
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
