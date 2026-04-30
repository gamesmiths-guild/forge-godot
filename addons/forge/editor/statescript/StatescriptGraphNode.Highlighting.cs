// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Statescript;

public partial class StatescriptGraphNode
{
	private static bool ResolverReferencesVariable(StatescriptResolverResource? resolver, string variableName)
	{
		if (resolver is null || string.IsNullOrEmpty(variableName))
		{
			return false;
		}

		var visited = new HashSet<nint>();
		return ResolverReferencesVariableRecursive(resolver, variableName, visited);
	}

	private static bool ResolverReferencesVariableRecursive(
		StatescriptResolverResource resolver,
		string variableName,
		HashSet<nint> visited)
	{
		nint instanceId = (nint)resolver.GetInstanceId();
		if (!visited.Add(instanceId))
		{
			return false;
		}

		if (resolver is VariableResolverResource variableResolver
			&& string.Equals(variableResolver.VariableName, variableName, StringComparison.Ordinal))
		{
			return true;
		}

		foreach (PropertyInfo property in resolver.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public))
		{
			if (!property.CanRead || !typeof(StatescriptResolverResource).IsAssignableFrom(property.PropertyType))
			{
				continue;
			}

			if (property.GetValue(resolver) is StatescriptResolverResource nestedResolver
				&& ResolverReferencesVariableRecursive(nestedResolver, variableName, visited))
			{
				return true;
			}
		}

		return false;
	}

	private bool ReferencesVariable(string variableName)
	{
		if (NodeResource is null)
		{
			return false;
		}

		return NodeResource.PropertyBindings.Any(binding => ResolverReferencesVariable(binding.Resolver, variableName));
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
			else if (child is FoldableContainer foldable)
			{
				HighlightFoldableSummaryBadgeIfPresent(foldable);
			}
			else if (child is PanelContainer badge && badge.HasMeta("forge_inline_summary_badge_kind"))
			{
				HighlightSummaryBadgeIfMatches(badge);
			}

			UpdateHighlightsRecursive(child);
		}
	}

	private void HighlightFoldableSummaryBadgeIfPresent(FoldableContainer foldable)
	{
		if (!foldable.HasMeta("forge_inline_summary_badge"))
		{
			return;
		}

		if (foldable.GetMeta("forge_inline_summary_badge").Obj is PanelContainer badge
			&& IsInstanceValid(badge))
		{
			HighlightSummaryBadgeIfMatches(badge);
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

	private void HighlightSummaryBadgeIfMatches(PanelContainer badge)
	{
		if (badge.GetNodeOrNull<Label>("Row/Text") is not Label textLabel
			  || badge.GetNodeOrNull<Label>("Row/Icon") is not Label iconLabel)
		{
			return;
		}

		if (!badge.HasMeta("forge_inline_summary_badge_base_stylebox")
			&& badge.GetThemeStylebox("panel") is StyleBoxFlat baseStyle)
		{
			badge.SetMeta("forge_inline_summary_badge_base_stylebox", Variant.From(baseStyle.Duplicate()));
		}

		if (!badge.HasMeta("forge_inline_summary_badge_base_stylebox")
			|| badge.GetMeta("forge_inline_summary_badge_base_stylebox").Obj is not StyleBoxFlat storedBase)
		{
			return;
		}

		string propagatedVariableName = badge.HasMeta("forge_inline_summary_badge_highlight_variable")
			? badge.GetMeta("forge_inline_summary_badge_highlight_variable").AsString()
			: string.Empty;

		bool isMatch = !string.IsNullOrEmpty(_highlightedVariableName)
			&& (textLabel.Text == _highlightedVariableName
				|| propagatedVariableName == _highlightedVariableName);

		badge.SetMeta(
			"forge_inline_summary_badge_selected_variable",
			Variant.From(_highlightedVariableName ?? string.Empty));

		var style = (StyleBoxFlat)storedBase.Duplicate();
		if (isMatch)
		{
			style.BorderWidthLeft = Math.Max(style.BorderWidthLeft, 2);
			style.BorderWidthTop = Math.Max(style.BorderWidthTop, 2);
			style.BorderWidthRight = Math.Max(style.BorderWidthRight, 2);
			style.BorderWidthBottom = Math.Max(style.BorderWidthBottom, 2);
			style.BorderColor = _highlightColor;
			style.BgColor = _highlightColor;
			iconLabel.AddThemeColorOverride("font_color", Colors.Black);
			textLabel.AddThemeColorOverride("font_color", Colors.Black);
		}
		else
		{
			iconLabel.RemoveThemeColorOverride("font_color");
			textLabel.RemoveThemeColorOverride("font_color");
		}

		badge.AddThemeStyleboxOverride("panel", style);
	}
}
#endif
