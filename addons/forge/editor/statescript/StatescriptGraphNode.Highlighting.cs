// Copyright © Gamesmiths Guild.

#if TOOLS
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Statescript;

public partial class StatescriptGraphNode
{
	private bool ReferencesVariable(string variableName)
	{
		if (NodeResource is null)
		{
			return false;
		}

		foreach (StatescriptNodeProperty binding in NodeResource.PropertyBindings)
		{
			if (binding.Resolver is VariableResolverResource varRes
				&& varRes.VariableName == variableName)
			{
				return true;
			}
		}

		return false;
	}

	private void ApplyHighlightBorder()
	{
		if (_isHighlighted)
		{
			if (GetThemeStylebox("panel") is not StyleBoxFlat baseStyle)
			{
				return;
			}

			var highlightStyle = (StyleBoxFlat)baseStyle.Duplicate();

			highlightStyle.BorderColor = _highlightColor;
			highlightStyle.BorderWidthTop = 2;
			highlightStyle.BorderWidthBottom = 2;
			highlightStyle.BorderWidthLeft = 2;
			highlightStyle.BorderWidthRight = 2;

			highlightStyle.BgColor = baseStyle.BgColor.Lerp(_highlightColor, 0.15f);

			AddThemeStyleboxOverride("panel", highlightStyle);
			AddThemeStyleboxOverride("panel_selected", highlightStyle);
		}
		else
		{
			RemoveThemeStyleboxOverride("panel");
			RemoveThemeStyleboxOverride("panel_selected");

			ApplyBottomPadding();
		}
	}

	private void UpdateChildHighlights()
	{
		UpdateHighlightsRecursive(this);
	}

	private void UpdateHighlightsRecursive(Node parent)
	{
		foreach (Node child in parent.GetChildren())
		{
			if (child is OptionButton optionButton)
			{
				HighlightOptionButtonIfMatches(optionButton);
			}
			else if (child is Label label)
			{
				HighlightLabelIfMatches(label);
			}

			UpdateHighlightsRecursive(child);
		}
	}

	private void HighlightOptionButtonIfMatches(OptionButton dropdown)
	{
		if (!dropdown.HasMeta("is_variable_dropdown"))
		{
			return;
		}

		if (string.IsNullOrEmpty(_highlightedVariableName))
		{
			dropdown.RemoveThemeStyleboxOverride("normal");
			return;
		}

		var selectedIdx = dropdown.Selected;
		if (selectedIdx < 0)
		{
			dropdown.RemoveThemeStyleboxOverride("normal");
			return;
		}

		var selectedText = dropdown.GetItemText(selectedIdx);
		if (selectedText == _highlightedVariableName)
		{
			if (dropdown.GetThemeStylebox("normal") is not StyleBoxFlat baseStyle)
			{
				return;
			}

			var highlightStyle = (StyleBoxFlat)baseStyle.Duplicate();

			highlightStyle.BgColor = baseStyle.BgColor.Lerp(_highlightColor, 0.25f);

			dropdown.AddThemeStyleboxOverride("normal", highlightStyle);
		}
		else
		{
			dropdown.RemoveThemeStyleboxOverride("normal");
		}
	}

	private void HighlightLabelIfMatches(Label label)
	{
		if (string.IsNullOrEmpty(_highlightedVariableName))
		{
			if (label.HasMeta("is_highlight_colored"))
			{
				label.RemoveThemeColorOverride("font_color");
				label.RemoveMeta("is_highlight_colored");
			}

			return;
		}

		if (label.Text == _highlightedVariableName)
		{
			label.AddThemeColorOverride("font_color", _highlightColor);
			label.SetMeta("is_highlight_colored", true);
		}
		else if (label.HasMeta("is_highlight_colored"))
		{
			label.RemoveThemeColorOverride("font_color");
			label.RemoveMeta("is_highlight_colored");
		}
	}
}
#endif
