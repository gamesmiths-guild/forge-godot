// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Statescript;

internal static class InlineConstantSummaryFormatter
{
	private const string SummaryBadgeMetaKey = "forge_inline_summary_badge";
	private const string SummaryBadgeResizeHookMetaKey = "forge_inline_summary_badge_resize_hook";
	private const string SummaryBadgeKindMetaKey = "forge_inline_summary_badge_kind";
	private const string SummaryBadgeTextMetaKey = "forge_inline_summary_badge_text";
	private const string SummaryBadgeFoldableMetaKey = "forge_inline_summary_badge_foldable";
	private const string SummaryBadgeSelectedVariableMetaKey = "forge_inline_summary_badge_selected_variable";
	private const float MinimumBadgeWidth = 76f;
	private const float FoldableTitleChromeWidth = 30f;
	private const float FoldableTitleBadgeGap = 6f;
	private const float FoldableTitleRightPadding = 8f;

	private static readonly Color _numericIconColor = new(0x3dbcc9ff);
	private static readonly Color _numericBackgroundColor = new(0x3dbcc918);
	private static readonly Color _numericBorderColor = new(0x3dbcc9ff);
	private static readonly Color _vectorIconColor = new(0xd48a3aff);
	private static readonly Color _vectorBackgroundColor = new(0xd48a3a18);
	private static readonly Color _vectorBorderColor = new(0xd48a3aff);
	private static readonly Color _booleanIconColor = new(0xc2a24fff);
	private static readonly Color _booleanBackgroundColor = new(0xc2a24f18);
	private static readonly Color _booleanBorderColor = new(0xc2a24fff);
	private static readonly Color _resolverIconColor = new(0xc06bcfff);
	private static readonly Color _resolverBackgroundColor = new(0xc06bcf18);
	private static readonly Color _resolverBorderColor = new(0xc06bcfff);
	private static readonly Color _variableIconColor = new(0x5d7be0ff);
	private static readonly Color _variableBackgroundColor = new(0x5d7be018);
	private static readonly Color _variableBorderColor = new(0x5d7be0ff);
	private static readonly Color _sharedVariableIconColor = new(0x46a86fff);
	private static readonly Color _sharedVariableBackgroundColor = new(0x46a86f18);
	private static readonly Color _sharedVariableBorderColor = new(0x46a86fff);
	private static readonly Color _enumIconColor = new(0xc0c6d1ff);
	private static readonly Color _enumBackgroundColor = new(0xc0c6d118);
	private static readonly Color _enumBorderColor = new(0xc0c6d1ff);

	public static void ApplyFoldableTitle(
		string baseTitle,
		FoldableContainer foldable,
		NodeEditorProperty? editor)
	{
		EnsureResizeSyncHook(foldable);
		foldable.Title = baseTitle;

		SummaryBadgeData badgeData = GetBadgeData(foldable, editor);
		PanelContainer badge = GetOrCreateSummaryBadge(foldable);
		ConfigureSummaryBadge(badge, badgeData);
		SynchronizeSiblingBadgeWidths(foldable);
	}

	public static void ApplyFoldableTitle(
		string baseTitle,
		FoldableContainer foldable,
		string? summary,
		InlineSummaryBadgeKind badgeKind,
		bool isConstant = false,
		string? highlightedVariableName = null)
	{
		EnsureResizeSyncHook(foldable);
		foldable.Title = baseTitle;

		SummaryBadgeData badgeData = foldable.Folded && !string.IsNullOrWhiteSpace(summary)
			? CreateBadgeData(summary, badgeKind, isConstant, highlightedVariableName)
			: SummaryBadgeData.Hidden;

		PanelContainer badge = GetOrCreateSummaryBadge(foldable);
		ConfigureSummaryBadge(badge, badgeData);
		SynchronizeSiblingBadgeWidths(foldable);
	}

	public static string GetFoldableTitle(
		string baseTitle,
		FoldableContainer foldable,
		NodeEditorProperty? editor)
	{
		if (!foldable.Folded || editor is null)
		{
			return baseTitle;
		}

		if (editor.TryGetInlineSummary(out string summary) && !string.IsNullOrWhiteSpace(summary))
		{
			return $"{baseTitle} {summary}";
		}

		return string.IsNullOrWhiteSpace(editor.DisplayName)
			? baseTitle
			: $"{baseTitle} {editor.DisplayName}";
	}

	public static string FormatVariant(Variant value, StatescriptVariableType valueType)
	{
		return valueType switch
		{
			StatescriptVariableType.Bool => value.AsBool() ? "True" : "False",
			StatescriptVariableType.Byte => value.AsInt32().ToString(CultureInfo.InvariantCulture),
			StatescriptVariableType.SByte => value.AsInt32().ToString(CultureInfo.InvariantCulture),
			StatescriptVariableType.Char => ((char)value.AsInt32()).ToString(),
			StatescriptVariableType.Decimal => value.AsDouble().ToString("G", CultureInfo.InvariantCulture),
			StatescriptVariableType.Double => value.AsDouble().ToString("G", CultureInfo.InvariantCulture),
			StatescriptVariableType.Float => value.AsSingle().ToString("G", CultureInfo.InvariantCulture),
			StatescriptVariableType.Int => value.AsInt32().ToString(CultureInfo.InvariantCulture),
			StatescriptVariableType.UInt => value.AsInt64().ToString(CultureInfo.InvariantCulture),
			StatescriptVariableType.Long => value.AsInt64().ToString(CultureInfo.InvariantCulture),
			StatescriptVariableType.ULong => value.AsInt64().ToString(CultureInfo.InvariantCulture),
			StatescriptVariableType.Short => value.AsInt32().ToString(CultureInfo.InvariantCulture),
			StatescriptVariableType.UShort => value.AsInt32().ToString(CultureInfo.InvariantCulture),
			StatescriptVariableType.Vector2 => FormatVector2(value.AsVector2()),
			StatescriptVariableType.Vector3 => FormatVector3(value.AsVector3()),
			StatescriptVariableType.Vector4 => FormatVector4(value.AsVector4()),
			StatescriptVariableType.Plane => FormatPlane(value.AsPlane()),
			StatescriptVariableType.Quaternion => FormatQuaternion(value.AsQuaternion()),
			_ => value.ToString(),
		};
	}

	public static InlineSummaryBadgeKind GetBadgeKind(StatescriptVariableType valueType)
	{
		return valueType switch
		{
			StatescriptVariableType.Bool => InlineSummaryBadgeKind.Boolean,
			StatescriptVariableType.Vector2 => InlineSummaryBadgeKind.Vector,
			StatescriptVariableType.Vector3 => InlineSummaryBadgeKind.Vector,
			StatescriptVariableType.Vector4 => InlineSummaryBadgeKind.Vector,
			StatescriptVariableType.Plane => InlineSummaryBadgeKind.Vector,
			StatescriptVariableType.Quaternion => InlineSummaryBadgeKind.Vector,
			_ => InlineSummaryBadgeKind.Numeric,
		};
	}

	private static SummaryBadgeData GetBadgeData(FoldableContainer foldable, NodeEditorProperty? editor)
	{
		if (!foldable.Folded || editor is null)
		{
			return SummaryBadgeData.Hidden;
		}

		string? highlightedVariableName = null;
		if (editor.TryGetHighlightedVariableName(out string propagatedVariableName)
			&& !string.IsNullOrWhiteSpace(propagatedVariableName))
		{
			highlightedVariableName = propagatedVariableName;
		}

		if (editor.TryGetInlineSummary(out string summary) && !string.IsNullOrWhiteSpace(summary))
		{
			InlineSummaryBadgeKind badgeKind = editor.GetInlineSummaryBadgeKind();
			return CreateBadgeData(
				summary,
				badgeKind,
				IsConstantBadgeKind(badgeKind),
				highlightedVariableName);
		}

		return string.IsNullOrWhiteSpace(editor.DisplayName)
			? SummaryBadgeData.Hidden
			: CreateBadgeData(editor.DisplayName, InlineSummaryBadgeKind.Resolver, false, highlightedVariableName);
	}

	private static SummaryBadgeData CreateBadgeData(
		string text,
		InlineSummaryBadgeKind badgeKind,
		bool isConstant,
		string? highlightedVariableName = null)
	{
		BadgeVisualStyle style = GetBadgeStyle(badgeKind);
		return new SummaryBadgeData(
			GetBadgeIcon(badgeKind, isConstant),
			text,
			highlightedVariableName ?? string.Empty,
			badgeKind,
			isConstant,
			style.IconColor,
			style.BackgroundColor,
			style.BorderColor,
			true);
	}

	private static PanelContainer GetOrCreateSummaryBadge(FoldableContainer foldable)
	{
		if (TryGetSummaryBadge(foldable, out PanelContainer? existingBadge))
		{
			return existingBadge;
		}

		var badge = new PanelContainer
		{
			Visible = false,
			MouseFilter = Control.MouseFilterEnum.Ignore,
			SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter,
		};
		badge.SetMeta(SummaryBadgeFoldableMetaKey, Variant.From(foldable));

		var row = new HBoxContainer
		{
			Name = "Row",
			MouseFilter = Control.MouseFilterEnum.Ignore,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
		};
		row.AddThemeConstantOverride("separation", 4);
		badge.AddChild(row);

		var iconLabel = new Label
		{
			Name = "Icon",
			MouseFilter = Control.MouseFilterEnum.Ignore,
			SizeFlagsHorizontal = Control.SizeFlags.ShrinkBegin,
			HorizontalAlignment = HorizontalAlignment.Left,
			VerticalAlignment = VerticalAlignment.Center,
		};
		row.AddChild(iconLabel);

		var textLabel = new Label
		{
			Name = "Text",
			MouseFilter = Control.MouseFilterEnum.Ignore,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			HorizontalAlignment = HorizontalAlignment.Right,
			VerticalAlignment = VerticalAlignment.Center,
			TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis,
		};
		row.AddChild(textLabel);

		foldable.AddTitleBarControl(badge);
		foldable.SetMeta(SummaryBadgeMetaKey, Variant.From(badge));
		return badge;
	}

	private static void EnsureResizeSyncHook(FoldableContainer foldable)
	{
		if (foldable.HasMeta(SummaryBadgeResizeHookMetaKey)
			&& foldable.GetMeta(SummaryBadgeResizeHookMetaKey).AsBool())
		{
			return;
		}

		foldable.Resized += () => SynchronizeSiblingBadgeWidths(foldable);
		foldable.SetMeta(SummaryBadgeResizeHookMetaKey, Variant.From(true));
	}

	private static void ConfigureSummaryBadge(PanelContainer badge, SummaryBadgeData badgeData)
	{
		badge.Visible = badgeData.Visible;
		if (!badgeData.Visible)
		{
			badge.SetMeta(SummaryBadgeKindMetaKey, Variant.From((int)InlineSummaryBadgeKind.Resolver));
			badge.SetMeta(SummaryBadgeTextMetaKey, Variant.From(string.Empty));
			badge.CustomMinimumSize = Vector2.Zero;
			return;
		}

		badge.SetMeta(SummaryBadgeKindMetaKey, Variant.From((int)badgeData.BadgeKind));
		badge.SetMeta(SummaryBadgeTextMetaKey, Variant.From(badgeData.Text));
		badge.SetMeta("forge_inline_summary_badge_highlight_variable", Variant.From(badgeData.HighlightVariableName));

		StyleBoxFlat styleBox = new()
		{
			BgColor = badgeData.BackgroundColor,
			BorderColor = badgeData.BorderColor,
			CornerRadiusTopLeft = 8,
			CornerRadiusTopRight = 8,
			CornerRadiusBottomRight = 8,
			CornerRadiusBottomLeft = 8,
			BorderWidthLeft = 1,
			BorderWidthTop = 1,
			BorderWidthRight = 1,
			BorderWidthBottom = 1,
			ContentMarginLeft = 8,
			ContentMarginTop = 3,
			ContentMarginRight = 8,
			ContentMarginBottom = 3,
		};

		badge.AddThemeStyleboxOverride("panel", styleBox);

		Label? iconLabel = badge.GetNodeOrNull<Label>("Row/Icon");
		Label? textLabel = badge.GetNodeOrNull<Label>("Row/Text");
		if (iconLabel is null || textLabel is null)
		{
			return;
		}

		iconLabel.SizeFlagsHorizontal = Control.SizeFlags.ShrinkBegin;
		textLabel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		iconLabel.Text = badgeData.IconText;
		textLabel.Text = badgeData.Text;
		iconLabel.CustomMinimumSize = new Vector2(MeasureWidestBadgeIconWidth(iconLabel), 0);
		iconLabel.AddThemeColorOverride("font_color", badgeData.IconColor);
		textLabel.AddThemeColorOverride("font_color", textLabel.GetThemeColor("font_color", "Label"));
		textLabel.CustomMinimumSize = Vector2.Zero;

		if (badge.HasMeta(SummaryBadgeSelectedVariableMetaKey))
		{
			string selectedVariableName = badge.GetMeta(SummaryBadgeSelectedVariableMetaKey).AsString();
			if (!string.IsNullOrEmpty(selectedVariableName)
				&& selectedVariableName == badgeData.HighlightVariableName)
			{
				styleBox.BorderWidthLeft = Math.Max(styleBox.BorderWidthLeft, 2);
				styleBox.BorderWidthTop = Math.Max(styleBox.BorderWidthTop, 2);
				styleBox.BorderWidthRight = Math.Max(styleBox.BorderWidthRight, 2);
				styleBox.BorderWidthBottom = Math.Max(styleBox.BorderWidthBottom, 2);
				styleBox.BorderColor = new Color(0x56b6c2ff);
				styleBox.BgColor = new Color(0x56b6c2ff);
				iconLabel.AddThemeColorOverride("font_color", Colors.Black);
				textLabel.AddThemeColorOverride("font_color", Colors.Black);
			}
		}
	}

	private static float MeasureWidestBadgeIconWidth(Label label)
	{
		float maxWidth = 0f;
		foreach (InlineSummaryBadgeKind badgeKind in Enum.GetValues<InlineSummaryBadgeKind>())
		{
			maxWidth = Math.Max(maxWidth, MeasureLabelTextWidth(label, GetBadgeIcon(badgeKind, false)));
		}

		return Math.Max(maxWidth, MeasureLabelTextWidth(label, GetBadgeIcon(InlineSummaryBadgeKind.Resolver, true)));
	}

	private static bool TryGetSummaryBadge(FoldableContainer foldable, [NotNullWhen(true)] out PanelContainer? badge)
	{
		badge = null;
		if (!foldable.HasMeta(SummaryBadgeMetaKey))
		{
			return false;
		}

		Variant existing = foldable.GetMeta(SummaryBadgeMetaKey);
		if (existing.Obj is not PanelContainer existingBadge || !GodotObject.IsInstanceValid(existingBadge))
		{
			return false;
		}

		badge = existingBadge;
		return true;
	}

	private static void SynchronizeSiblingBadgeWidths(FoldableContainer foldable)
	{
		if (foldable.GetParent() is not Node parent)
		{
			return;
		}

		var siblingFoldables = new List<FoldableContainer>();
		var siblingBadges = new List<PanelContainer>();
		foreach (Node child in parent.GetChildren())
		{
			if (child is FoldableContainer siblingFoldable
				&& TryGetSummaryBadge(siblingFoldable, out PanelContainer? badge))
			{
				siblingFoldables.Add(siblingFoldable);
				siblingBadges.Add(badge);
			}
		}

		if (siblingBadges.Count == 0)
		{
			return;
		}

		foreach (PanelContainer badge in siblingBadges)
		{
			badge.CustomMinimumSize = Vector2.Zero;
		}

		float widestTitleWidth = 0;
		foreach (FoldableContainer siblingFoldable in siblingFoldables)
		{
			widestTitleWidth = Math.Max(widestTitleWidth, MeasureFoldableTitleWidth(siblingFoldable));
		}

		float availableWidth = parent is Control parentControl
			? parentControl.Size.X
				- widestTitleWidth
				- FoldableTitleChromeWidth
				- FoldableTitleBadgeGap
				- FoldableTitleRightPadding
			: float.MaxValue;

		float maxWidth = MinimumBadgeWidth;
		if (!float.IsPositiveInfinity(availableWidth) && !float.IsNaN(availableWidth))
		{
			maxWidth = Math.Max(0f, availableWidth);
		}

		for (int i = 0; i < siblingBadges.Count; i++)
		{
			PanelContainer badge = siblingBadges[i];
			badge.CustomMinimumSize = badge.Visible
				? new Vector2(maxWidth, 0)
				: Vector2.Zero;
		}
	}

	private static BadgeVisualStyle GetBadgeStyle(InlineSummaryBadgeKind badgeKind)
	{
		return badgeKind switch
		{
			InlineSummaryBadgeKind.Numeric => new BadgeVisualStyle(
				_numericIconColor,
				_numericBackgroundColor,
				_numericBorderColor),
			InlineSummaryBadgeKind.Vector => new BadgeVisualStyle(
				_vectorIconColor,
				_vectorBackgroundColor,
				_vectorBorderColor),
			InlineSummaryBadgeKind.Boolean => new BadgeVisualStyle(
				_booleanIconColor,
				_booleanBackgroundColor,
				_booleanBorderColor),
			InlineSummaryBadgeKind.Variable => new BadgeVisualStyle(
				_variableIconColor,
				_variableBackgroundColor,
				_variableBorderColor),
			InlineSummaryBadgeKind.SharedVariable => new BadgeVisualStyle(
				_sharedVariableIconColor,
				_sharedVariableBackgroundColor,
				_sharedVariableBorderColor),
			InlineSummaryBadgeKind.Enum => new BadgeVisualStyle(
				_enumIconColor,
				_enumBackgroundColor,
				_enumBorderColor),
			_ => new BadgeVisualStyle(
				_resolverIconColor,
				_resolverBackgroundColor,
				_resolverBorderColor),
		};
	}

	private static bool IsConstantBadgeKind(InlineSummaryBadgeKind badgeKind)
	{
		return badgeKind is InlineSummaryBadgeKind.Numeric
			or InlineSummaryBadgeKind.Vector
			or InlineSummaryBadgeKind.Boolean;
	}

	private static float MeasureFoldableTitleWidth(FoldableContainer foldable)
	{
		Font? font = foldable.GetThemeDefaultFont();
		if (font is null)
		{
			return 0;
		}

		int fontSize = foldable.GetThemeDefaultFontSize();
		return font.GetStringSize(
			foldable.Title,
			HorizontalAlignment.Left,
			-1,
			fontSize,
			TextServer.JustificationFlag.None,
			TextServer.Direction.Inherited,
			TextServer.Orientation.Horizontal).X;
	}

	private static string GetBadgeIcon(InlineSummaryBadgeKind badgeKind, bool isConstant)
	{
		return badgeKind switch
		{
			InlineSummaryBadgeKind.Numeric => "#",
			InlineSummaryBadgeKind.Vector => "▦",
			InlineSummaryBadgeKind.Boolean => "●",
			InlineSummaryBadgeKind.Variable => "𝑥",
			InlineSummaryBadgeKind.SharedVariable => "𝑦",
			InlineSummaryBadgeKind.Enum => "≡",
			_ => isConstant ? "#" : "ƒ",
		};
	}

	private static float MeasureLabelTextWidth(Label label, string text)
	{
		Font? font = label.GetThemeFont("font", "Label") ?? label.GetThemeDefaultFont();
		if (font is null)
		{
			return 0;
		}

		int fontSize = label.GetThemeFontSize("font_size", "Label");
		if (fontSize <= 0)
		{
			fontSize = label.GetThemeDefaultFontSize();
		}

		return font.GetStringSize(
			text,
			HorizontalAlignment.Left,
			-1,
			fontSize,
			TextServer.JustificationFlag.None,
			TextServer.Direction.Inherited,
			TextServer.Orientation.Horizontal).X;
	}

	private static string FormatVector2(Vector2 value)
	{
		return $"({FormatNumber(value.X)}, {FormatNumber(value.Y)})";
	}

	private static string FormatVector3(Vector3 value)
	{
		return $"({FormatNumber(value.X)}, {FormatNumber(value.Y)}, {FormatNumber(value.Z)})";
	}

	private static string FormatVector4(Vector4 value)
	{
		return $"({FormatNumber(value.X)}, {FormatNumber(value.Y)}, {FormatNumber(value.Z)}, {FormatNumber(value.W)})";
	}

	private static string FormatPlane(Plane value)
	{
		return $"({FormatNumber(value.Normal.X)}, {FormatNumber(value.Normal.Y)}, {FormatNumber(value.Normal.Z)}, {FormatNumber(value.D)})";
	}

	private static string FormatQuaternion(Quaternion value)
	{
		return $"({FormatNumber(value.X)}, {FormatNumber(value.Y)}, {FormatNumber(value.Z)}, {FormatNumber(value.W)})";
	}

	private static string FormatNumber(float value)
	{
		return value.ToString("G", CultureInfo.InvariantCulture);
	}

	private readonly record struct BadgeVisualStyle(
		Color IconColor,
		Color BackgroundColor,
		Color BorderColor);

	private readonly record struct SummaryBadgeData(
		string IconText,
		string Text,
		string HighlightVariableName,
		InlineSummaryBadgeKind BadgeKind,
		bool IsConstant,
		Color IconColor,
		Color BackgroundColor,
		Color BorderColor,
		bool Visible)
	{
		public static SummaryBadgeData Hidden => new(
			string.Empty,
			string.Empty,
			string.Empty,
			InlineSummaryBadgeKind.Resolver,
			false,
			Colors.Transparent,
			Colors.Transparent,
			Colors.Transparent,
			false);
	}
}
#endif
