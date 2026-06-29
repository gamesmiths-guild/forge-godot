// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.NodeEditors;

/// <summary>
/// Custom editor for <c>ExpressionNode</c>. Renders the bound condition resolver tree as a single, wrapping,
/// syntax-highlighted formula preview in a box above the standard <c>Input Properties</c> section, then the normal
/// editable <c>Condition</c> input row below it. The preview refreshes live whenever the condition changes.
/// </summary>
[Tool]
internal sealed partial class ExpressionNodeEditor : CustomNodeEditor
{
	private const int ConditionInput = 0;
	private const string InputFoldKey = "_fold_input";

	[NonSerialized]
	private RichTextLabel? _formulaLabel;

	/// <inheritdoc/>
	public override string HandledRuntimeTypeName => "Gamesmiths.Forge.Statescript.Nodes.Condition.ExpressionNode";

	/// <inheritdoc/>
	public override void BuildPropertySections(StatescriptNodeDiscovery.NodeTypeInfo typeInfo)
	{
		BuildFormulaBox();

		if (typeInfo.InputPropertiesInfo.Length > 0)
		{
			FoldableContainer inputContainer = AddPropertySectionDivider(
				"Input Properties",
				InputPropertyColor,
				InputFoldKey,
				GetFoldState(InputFoldKey));

			for (int i = 0; i < typeInfo.InputPropertiesInfo.Length; i++)
			{
				AddInputPropertyRow(typeInfo.InputPropertiesInfo[i], i, inputContainer);
			}
		}

		SetBindingChangedHandler(RefreshFormula);
	}

	/// <inheritdoc/>
	internal override void Unbind()
	{
		base.Unbind();
		_formulaLabel = null;
	}

	private static StyleBoxFlat CreateFormulaStyleBox()
	{
		var style = new StyleBoxFlat
		{
			BgColor = new Color(0x1b1e24ff),
			BorderColor = new Color(0x2c313aff),
		};

		style.SetBorderWidthAll(1);
		style.SetCornerRadiusAll(4);
		return style;
	}

	private void BuildFormulaBox()
	{
		var panel = new PanelContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
		panel.AddThemeStyleboxOverride("panel", CreateFormulaStyleBox());

		var margin = new MarginContainer();
		margin.AddThemeConstantOverride("margin_left", 8);
		margin.AddThemeConstantOverride("margin_right", 8);
		margin.AddThemeConstantOverride("margin_top", 5);
		margin.AddThemeConstantOverride("margin_bottom", 5);
		panel.AddChild(margin);

		_formulaLabel = new RichTextLabel
		{
			BbcodeEnabled = true,
			FitContent = true,
			ScrollActive = false,
			AutowrapMode = TextServer.AutowrapMode.WordSmart,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			CustomMinimumSize = new Vector2(170, 0),
		};
		_formulaLabel.AddThemeFontSizeOverride("normal_font_size", 12);
		_formulaLabel.AddThemeColorOverride("default_color", new Color(0xc8ccd4ff));
		margin.AddChild(_formulaLabel);

		AddNodeBodyContent(panel);
		RefreshFormula();
	}

	private void RefreshFormula()
	{
		if (_formulaLabel is null || !IsInstanceValid(_formulaLabel))
		{
			return;
		}

		StatescriptResolverResource? resolver =
			FindBinding(StatescriptPropertyDirection.Input, ConditionInput)?.Resolver;
		_formulaLabel.Text = ResolverExpressionFormatter.Format(resolver);
		ResetSize();
	}
}
#endif
