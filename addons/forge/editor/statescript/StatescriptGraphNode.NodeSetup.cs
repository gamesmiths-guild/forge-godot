// Copyright © Gamesmiths Guild.

#if TOOLS
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Godot;
using NodeNetworkKind = Gamesmiths.Forge.Statescript.NodeNetworkKind;

namespace Gamesmiths.Forge.Godot.Editor.Statescript;

public partial class StatescriptGraphNode
{
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
			case StatescriptNodeType.Flow:
				SetupFlowNode();
				break;
		}
	}

	private void SetupEntryNode()
	{
		CustomMinimumSize = new Vector2(100, 0);

		var label = new Label { Text = "Start" };
		AddChild(label);
		SetSlotEnabledRight(0, true);
		SetSlotColorRight(0, _eventColor);

		ApplyTitleBarColor(_entryColor);
	}

	private void SetupExitNode()
	{
		CustomMinimumSize = new Vector2(100, 0);

		var label = new Label { Text = "End" };
		AddChild(label);
		SetSlotEnabledLeft(0, true);
		SetSlotColorLeft(0, _eventColor);

		ApplyTitleBarColor(_exitColor);
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

	// Fallback layout for a flow node whose runtime type could not be resolved (a renamed or removed custom node). The
	// real layout comes from the node instance itself, since flow nodes declare their own ports.
	private void SetupFlowNode()
	{
		var hBox = new HBoxContainer();
		hBox.AddThemeConstantOverride("separation", 16);
		AddChild(hBox);

		var inputLabel = new Label { Text = "Input" };
		hBox.AddChild(inputLabel);
		SetSlotEnabledLeft(0, true);
		SetSlotColorLeft(0, _eventColor);

		var outputLabel = new Label
		{
			Text = "Output",
			HorizontalAlignment = HorizontalAlignment.Right,
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
		};

		hBox.AddChild(outputLabel);
		SetSlotEnabledRight(0, true);
		SetSlotColorRight(0, _eventColor);

		ApplyTitleBarColor(_flowColor);
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
		// Slot state is keyed by child index and outlives the children, so a node that rebuilds with fewer port rows
		// than before would keep drawing the leftover slots next to whatever control now sits at those indices.
		ClearAllSlots();

		foreach (Node child in GetChildren())
		{
			RemoveChild(child);
			child.Free();
		}
	}

	// Simulation is the default and goes unmarked; the other kinds change where and when the node's work runs, which a
	// designer needs to see without opening the node's documentation.
	private void AddNetworkKindBadge(NodeNetworkKind kind)
	{
		if (kind == NodeNetworkKind.Simulation)
		{
			return;
		}

		bool presentation = kind == NodeNetworkKind.Presentation;

		var style = new StyleBoxFlat
		{
			BgColor = presentation ? _presentationColor : _authorityColor,
			ContentMarginLeft = 6,
			ContentMarginRight = 6,
			CornerRadiusTopLeft = 6,
			CornerRadiusTopRight = 6,
			CornerRadiusBottomLeft = 6,
			CornerRadiusBottomRight = 6,
		};

		_networkKindBadge = new Label
		{
			Text = presentation ? "Presentation" : "Authority",
			TooltipText = presentation
				? "Presentation: only shows something. It runs in the presentation pass at the end of the frame, on "
					+ "net change, and never changes gameplay."
				: "Authority: only the server may do this.",
			MouseFilter = MouseFilterEnum.Pass,
			VerticalAlignment = VerticalAlignment.Center,
		};

		_networkKindBadge.AddThemeStyleboxOverride("normal", style);
		_networkKindBadge.AddThemeFontSizeOverride("font_size", 11);
		GetTitlebarHBox().AddChild(_networkKindBadge);
	}

	private void RemoveNetworkKindBadge()
	{
		if (_networkKindBadge is null)
		{
			return;
		}

		if (IsInstanceValid(_networkKindBadge))
		{
			_networkKindBadge.GetParent()?.RemoveChild(_networkKindBadge);
			_networkKindBadge.Free();
		}

		_networkKindBadge = null;
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

	private void ApplyBottomPadding()
	{
		StyleBox? existing = GetThemeStylebox("panel");

		if (existing is not null)
		{
			var panelStyle = (StyleBox)existing.Duplicate();
			panelStyle.ContentMarginBottom = 10;
			AddThemeStyleboxOverride("panel", panelStyle);
		}

		StyleBox? selectedExisting = GetThemeStylebox("panel_selected");

		if (selectedExisting is not null)
		{
			var selectedPanelStyle = (StyleBox)selectedExisting.Duplicate();
			selectedPanelStyle.ContentMarginBottom = 10;
			AddThemeStyleboxOverride("panel_selected", selectedPanelStyle);
		}
	}
}
#endif
