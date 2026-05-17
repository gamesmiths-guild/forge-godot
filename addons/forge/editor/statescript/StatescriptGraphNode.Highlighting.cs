// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Gamesmiths.Forge.Statescript;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Statescript;

public partial class StatescriptGraphNode
{
	private readonly Dictionary<OptionButton, StyleBoxFlat> _baseDropdownStyles = [];

	private StyleBoxFlat? _basePanelStyle;
	private StyleBoxFlat? _baseSelectedPanelStyle;

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
			&& variableResolver.Scope == VariableScope.Graph
			&& string.Equals(variableResolver.VariableName, variableName, StringComparison.Ordinal))
		{
			return true;
		}

		if (resolver is ArrayVariableResolverResource arrayVariableResolver
			&& arrayVariableResolver.Scope == VariableScope.Graph
			&& string.Equals(arrayVariableResolver.VariableName, variableName, StringComparison.Ordinal))
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

	private static bool ResolverReferencesSharedVariable(
		StatescriptResolverResource? resolver,
		string sharedVariableSetPath,
		string variableName)
	{
		if (resolver is null
			|| string.IsNullOrEmpty(sharedVariableSetPath)
			|| string.IsNullOrEmpty(variableName))
		{
			return false;
		}

		var visited = new HashSet<nint>();
		return ResolverReferencesSharedVariableRecursive(resolver, sharedVariableSetPath, variableName, visited);
	}

	private static bool ResolverReferencesSharedVariableRecursive(
		StatescriptResolverResource resolver,
		string sharedVariableSetPath,
		string variableName,
		HashSet<nint> visited)
	{
		nint instanceId = (nint)resolver.GetInstanceId();
		if (!visited.Add(instanceId))
		{
			return false;
		}

		if (resolver is VariableResolverResource variableResolver
			&& variableResolver.Scope == VariableScope.Shared
			&& string.Equals(variableResolver.SharedVariableSetPath, sharedVariableSetPath, StringComparison.Ordinal)
			&& string.Equals(variableResolver.VariableName, variableName, StringComparison.Ordinal))
		{
			return true;
		}

		if (resolver is ArrayVariableResolverResource arrayVariableResolver
			&& arrayVariableResolver.Scope == VariableScope.Shared
			&& string.Equals(
				arrayVariableResolver.SharedVariableSetPath,
				sharedVariableSetPath,
				StringComparison.Ordinal)
			&& string.Equals(
				arrayVariableResolver.VariableName,
				variableName,
				StringComparison.Ordinal))
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
				&& ResolverReferencesSharedVariableRecursive(
					nestedResolver,
					sharedVariableSetPath,
					variableName,
					visited))
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

	private bool ReferencesSharedVariable(string? sharedVariableSetPath, string? variableName)
	{
		if (NodeResource is null
			|| string.IsNullOrEmpty(sharedVariableSetPath)
			|| string.IsNullOrEmpty(variableName))
		{
			return false;
		}

		return NodeResource.PropertyBindings.Any(binding =>
			ResolverReferencesSharedVariable(binding.Resolver, sharedVariableSetPath, variableName));
	}

	private void ApplyHighlightBorder()
	{
		EnsureBasePanelStylesStored();

		if (_basePanelStyle is null || _baseSelectedPanelStyle is null)
		{
			return;
		}

		if (_isHighlighted)
		{
			StyleBoxFlat baseStyle = _basePanelStyle;
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
			AddThemeStyleboxOverride("panel", (StyleBoxFlat)_basePanelStyle.Duplicate());
			AddThemeStyleboxOverride("panel_selected", (StyleBoxFlat)_baseSelectedPanelStyle.Duplicate());
		}
	}

	private void UpdateChildHighlights()
	{
		UpdateHighlightsRecursive(this);
	}

	private void UpdateHighlightsRecursive(global::Godot.Node parent)
	{
		foreach (global::Godot.Node child in parent.GetChildren())
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
		if (!InlineConstantSummaryFormatter.TryGetSummaryBadgeForHighlighting(foldable, out PanelContainer? badge))
		{
			return;
		}

		HighlightSummaryBadgeIfMatches(badge);
	}

	private void HighlightOptionButtonIfMatches(OptionButton dropdown)
	{
		EnsureBaseDropdownStyleStored(dropdown);

		if (!dropdown.HasMeta("is_variable_dropdown") && !dropdown.HasMeta("is_shared_variable_dropdown"))
		{
			return;
		}

		if (!_baseDropdownStyles.TryGetValue(dropdown, out StyleBoxFlat? baseStyle))
		{
			return;
		}

		if (string.IsNullOrEmpty(_highlightedVariableName)
			&& (string.IsNullOrEmpty(_highlightedSharedVariableSetPath)
				|| string.IsNullOrEmpty(_highlightedSharedVariableName)))
		{
			dropdown.AddThemeStyleboxOverride("normal", (StyleBoxFlat)baseStyle.Duplicate());
			return;
		}

		int selectedIdx = dropdown.Selected;
		if (selectedIdx < 0)
		{
			dropdown.AddThemeStyleboxOverride("normal", (StyleBoxFlat)baseStyle.Duplicate());
			return;
		}

		string selectedText = dropdown.GetItemText(selectedIdx);
		bool isSharedVariableDropdown = dropdown.HasMeta("is_shared_variable_dropdown");
		string dropdownSetPath = dropdown.HasMeta("shared_variable_set_path")
			? dropdown.GetMeta("shared_variable_set_path").AsString()
			: string.Empty;

		bool isMatch = isSharedVariableDropdown
			? !string.IsNullOrEmpty(_highlightedSharedVariableSetPath)
				&& !string.IsNullOrEmpty(_highlightedSharedVariableName)
				&& selectedText == _highlightedSharedVariableName
				&& dropdownSetPath == _highlightedSharedVariableSetPath
			: selectedText == _highlightedVariableName;

		if (isMatch)
		{
			var highlightStyle = (StyleBoxFlat)baseStyle.Duplicate();
			highlightStyle.BgColor = baseStyle.BgColor.Lerp(_highlightColor, 0.25f);
			dropdown.AddThemeStyleboxOverride("normal", highlightStyle);
		}
		else
		{
			dropdown.AddThemeStyleboxOverride("normal", (StyleBoxFlat)baseStyle.Duplicate());
		}
	}

	private void EnsureBasePanelStylesStored()
	{
		if (_basePanelStyle is null && GetThemeStylebox("panel") is StyleBoxFlat panelStyle)
		{
			_basePanelStyle = (StyleBoxFlat)panelStyle.Duplicate();
		}

		if (_baseSelectedPanelStyle is null
			&& GetThemeStylebox("panel_selected") is StyleBoxFlat selectedPanelStyle)
		{
			_baseSelectedPanelStyle = (StyleBoxFlat)selectedPanelStyle.Duplicate();
		}
	}

	private void EnsureBaseDropdownStyleStored(OptionButton dropdown)
	{
		if (_baseDropdownStyles.ContainsKey(dropdown))
		{
			return;
		}

		if (dropdown.GetThemeStylebox("normal") is StyleBoxFlat baseStyle)
		{
			_baseDropdownStyles[dropdown] = (StyleBoxFlat)baseStyle.Duplicate();
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
		string propagatedSharedVariableName = badge.HasMeta("forge_inline_summary_badge_highlight_shared_variable")
			? badge.GetMeta("forge_inline_summary_badge_highlight_shared_variable").AsString()
			: string.Empty;
		string propagatedSharedVariableSetPath = badge.HasMeta("forge_inline_summary_badge_highlight_shared_set_path")
			? badge.GetMeta("forge_inline_summary_badge_highlight_shared_set_path").AsString()
			: string.Empty;

		bool isMatch = !string.IsNullOrEmpty(_highlightedVariableName)
			&& (textLabel.Text == _highlightedVariableName
				|| propagatedVariableName == _highlightedVariableName);

		isMatch = isMatch
			|| (!string.IsNullOrEmpty(_highlightedSharedVariableSetPath)
				&& !string.IsNullOrEmpty(_highlightedSharedVariableName)
				&& (textLabel.Text == _highlightedSharedVariableName
					|| propagatedSharedVariableName == _highlightedSharedVariableName)
				&& propagatedSharedVariableSetPath == _highlightedSharedVariableSetPath);

		badge.SetMeta(
			"forge_inline_summary_badge_selected_variable",
			Variant.From(_highlightedVariableName ?? string.Empty));
		badge.SetMeta(
			"forge_inline_summary_badge_selected_shared_set_path",
			Variant.From(_highlightedSharedVariableSetPath ?? string.Empty));
		badge.SetMeta(
			"forge_inline_summary_badge_selected_shared_variable",
			Variant.From(_highlightedSharedVariableName ?? string.Empty));

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
