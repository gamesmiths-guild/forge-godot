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
	private const float MinimumBadgeWidth = 76f;
	private const float FoldableTitleChromeWidth = 30f;
	private const float FoldableTitleBadgeGap = 6f;
	private const float FoldableTitleRightPadding = 8f;
	private const float BadgeTextWidthPadding = 12f;

	private static readonly Color _constantNumericIconColor = new(0x4f88acff);
	private static readonly Color _constantNumericBackgroundColor = new(0x4f88ac18);
	private static readonly Color _constantNumericBorderColor = new(0x4f88acff);
	private static readonly Color _constantVectorIconColor = new(0x4aa7b2ff);
	private static readonly Color _constantVectorBackgroundColor = new(0x4aa7b218);
	private static readonly Color _constantVectorBorderColor = new(0x4aa7b2ff);
	private static readonly Color _constantBooleanIconColor = new(0xc2a24fff);
	private static readonly Color _constantBooleanBackgroundColor = new(0xc2a24f18);
	private static readonly Color _constantBooleanBorderColor = new(0xc2a24fff);
	private static readonly Color _resolverIconColor = new(0x8f68b8ff);
	private static readonly Color _resolverBackgroundColor = new(0x8f68b818);
	private static readonly Color _resolverBorderColor = new(0x8f68b8ff);

	public static void ApplyFoldableTitle(
		string baseTitle,
		FoldableContainer foldable,
		NodeEditorProperty? editor)
	{
		foldable.Title = baseTitle;

		SummaryBadgeData badgeData = GetBadgeData(foldable, editor);
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

		if (editor.TryGetInlineSummary(out string summary) && !string.IsNullOrWhiteSpace(summary))
		{
			return CreateBadgeData(summary, editor.GetInlineSummaryBadgeKind(), true);
		}

		return string.IsNullOrWhiteSpace(editor.DisplayName)
			? SummaryBadgeData.Hidden
			: CreateBadgeData(editor.DisplayName, InlineSummaryBadgeKind.Resolver, false);
	}

	private static SummaryBadgeData CreateBadgeData(
		string text,
		InlineSummaryBadgeKind badgeKind,
		bool isConstant)
	{
		BadgeVisualStyle style = GetBadgeStyle(badgeKind);
		return new SummaryBadgeData(
			$" {GetBadgeIcon(badgeKind, isConstant)}",
			$" {text} ",
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
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center,
		};
		row.AddChild(iconLabel);

		var spacer = new Control
		{
			Name = "Spacer",
			MouseFilter = Control.MouseFilterEnum.Ignore,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
		};
		row.AddChild(spacer);

		var textLabel = new Label
		{
			Name = "Text",
			MouseFilter = Control.MouseFilterEnum.Ignore,
			SizeFlagsHorizontal = Control.SizeFlags.ShrinkEnd,
			HorizontalAlignment = HorizontalAlignment.Right,
			VerticalAlignment = VerticalAlignment.Center,
			TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis,
		};
		row.AddChild(textLabel);

		foldable.AddTitleBarControl(badge);
		foldable.SetMeta(SummaryBadgeMetaKey, Variant.From(badge));
		return badge;
	}

	private static void ConfigureSummaryBadge(PanelContainer badge, SummaryBadgeData badgeData)
	{
		badge.Visible = badgeData.Visible;
		if (!badgeData.Visible)
		{
			badge.CustomMinimumSize = Vector2.Zero;
			return;
		}

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

		iconLabel.Text = badgeData.IconText;
		textLabel.Text = badgeData.Text;
		iconLabel.AddThemeColorOverride("font_color", badgeData.IconColor);
		textLabel.AddThemeColorOverride("font_color", textLabel.GetThemeColor("font_color", "Label"));
		textLabel.CustomMinimumSize = new Vector2(
			MeasureLabelTextWidth(textLabel, badgeData.Text) + BadgeTextWidthPadding,
			0);
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

		float preferredMaxWidth = MinimumBadgeWidth;
		foreach (PanelContainer badge in siblingBadges)
		{
			badge.CustomMinimumSize = Vector2.Zero;
			if (!badge.Visible)
			{
				continue;
			}

			preferredMaxWidth = Math.Max(preferredMaxWidth, badge.GetCombinedMinimumSize().X);
		}

		float widestTitleWidth = 0;
		foreach (FoldableContainer siblingFoldable in siblingFoldables)
		{
			widestTitleWidth = Math.Max(widestTitleWidth, MeasureFoldableTitleWidth(siblingFoldable));
		}

		float availableWidth = float.MaxValue;
		foreach (FoldableContainer siblingFoldable in siblingFoldables)
		{
			availableWidth = Math.Min(
				availableWidth,
				siblingFoldable.Size.X - widestTitleWidth - FoldableTitleChromeWidth - FoldableTitleBadgeGap - FoldableTitleRightPadding);
		}

		float maxWidth = Math.Max(preferredMaxWidth, availableWidth);

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
				_constantNumericIconColor,
				_constantNumericBackgroundColor,
				_constantNumericBorderColor),
			InlineSummaryBadgeKind.Vector => new BadgeVisualStyle(
				_constantVectorIconColor,
				_constantVectorBackgroundColor,
				_constantVectorBorderColor),
			InlineSummaryBadgeKind.Boolean => new BadgeVisualStyle(
				_constantBooleanIconColor,
				_constantBooleanBackgroundColor,
				_constantBooleanBorderColor),
			_ => new BadgeVisualStyle(
				_resolverIconColor,
				_resolverBackgroundColor,
				_resolverBorderColor),
		};
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
		if (!isConstant)
		{
			return "ƒ";
		}

		return badgeKind switch
		{
			InlineSummaryBadgeKind.Numeric => "#",
			InlineSummaryBadgeKind.Vector => "▦",
			InlineSummaryBadgeKind.Boolean => "●",
			_ => "ƒ",
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
			InlineSummaryBadgeKind.Resolver,
			false,
			Colors.Transparent,
			Colors.Transparent,
			Colors.Transparent,
			false);
	}
}
#endif
