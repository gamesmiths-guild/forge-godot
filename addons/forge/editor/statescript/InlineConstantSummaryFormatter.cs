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
	private const string SummaryBadgeInstanceIdMetaKey = "forge_inline_summary_badge_instance_id";
	private const string SummaryBadgeResizeHookMetaKey = "forge_inline_summary_badge_resize_hook";
	private const string SummaryBadgeKindMetaKey = "forge_inline_summary_badge_kind";
	private const string SummaryBadgeTextMetaKey = "forge_inline_summary_badge_text";
	private const string SummaryBadgeSelectedVariableMetaKey = "forge_inline_summary_badge_selected_variable";

	private const string SummaryBadgeSelectedSharedVariableSetPathMetaKey =
		"forge_inline_summary_badge_selected_shared_set_path";

	private const string SummaryBadgeSelectedSharedVariableMetaKey =
		"forge_inline_summary_badge_selected_shared_variable";

	private const float MinimumBadgeWidth = 76f;
	private const float FoldableTitleChromeWidth = 30f;
	private const float FoldableTitleBadgeGap = 6f;
	private const float FoldableTitleRightPadding = 8f;
	private const float LabelColumnGap = 6f;
	private const float LabelColumnCenterNudge = 18f;

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

		// The label is the foldable's built-in title (set at build time); only overwrite it when a caller passes an
		// explicit title, so callers that pass an empty base title keep the build-time label.
		if (!string.IsNullOrEmpty(baseTitle))
		{
			foldable.Title = baseTitle;
		}

		SummaryBadgeData badgeData = GetBadgeData(foldable, editor);
		PanelContainer badge = GetOrCreateSummaryBadge(foldable);
		ConfigureSummaryBadge(badge, badgeData);
		ScheduleSync(foldable);
	}

	public static void ApplyFoldableTitle(
		string baseTitle,
		FoldableContainer foldable,
		string? summary,
		InlineSummaryBadgeKind badgeKind,
		bool isConstant = false,
		string? highlightedVariableName = null,
		string? highlightedSharedVariableSetPath = null,
		string? highlightedSharedVariableName = null)
	{
		EnsureResizeSyncHook(foldable);

		// The label is the foldable's built-in title (set at build time); only overwrite it when a caller passes an
		// explicit title, so callers that pass an empty base title keep the build-time label.
		if (!string.IsNullOrEmpty(baseTitle))
		{
			foldable.Title = baseTitle;
		}

		SummaryBadgeData badgeData = foldable.Folded && !string.IsNullOrWhiteSpace(summary)
			? CreateBadgeData(
				summary,
				badgeKind,
				isConstant,
				highlightedVariableName,
				highlightedSharedVariableSetPath,
				highlightedSharedVariableName)
			: SummaryBadgeData.Hidden;

		PanelContainer badge = GetOrCreateSummaryBadge(foldable);
		ConfigureSummaryBadge(badge, badgeData);
		ScheduleSync(foldable);
	}

	/// <summary>
	/// Builds a fixed two-column property row: a clipping label column on the left and the value-pill column on the
	/// right, each sized to half the node width by the title-bar width sync. Long labels and values ellipsize, with the
	/// full text shown on hover. The caller fills the foldable's content with the editor UI and badges it with
	/// <c>ApplyFoldableTitle</c>.
	/// </summary>
	/// <param name="parent">The container to add the row to.</param>
	/// <param name="label">The property's display label.</param>
	/// <param name="folded">The initial fold state.</param>
	/// <returns>The created foldable that the caller fills and badges.</returns>
	public static FoldableContainer BuildColumnedFoldable(Control parent, string label, bool folded)
	{
		FoldableContainer foldable = BuildColumnedFoldable(label, folded);
		parent.AddChild(foldable);
		return foldable;
	}

	/// <summary>
	/// Builds a columned property foldable (see the parent overload) without parenting it, for callers that add the
	/// foldable to their own container themselves. A trailing colon on <paramref name="label"/> is tolerated.
	/// </summary>
	/// <param name="label">The property's display label (a trailing ':' is normalized away).</param>
	/// <param name="folded">The initial fold state.</param>
	/// <returns>The created, unparented foldable that the caller fills and badges.</returns>
	public static FoldableContainer BuildColumnedFoldable(string label, bool folded)
	{
		string text = label.TrimEnd(':');

		// The label is the foldable's built-in title (left column), kept flush-left and ellipsized natively, so it
		// stays aligned whether or not a value pill is present. The pill (right column) is added as a title-bar control
		// by ApplyFoldableTitle.
		return new FoldableContainer
		{
			Title = $"{text}:",
			TitleAlignment = HorizontalAlignment.Left,
			TitleTextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis,
			TooltipText = text,
			Folded = folded,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
		};
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

	internal static bool TryGetSummaryBadgeForHighlighting(
		FoldableContainer foldable,
		[NotNullWhen(true)] out PanelContainer? badge)
	{
		return TryGetSummaryBadge(foldable, out badge);
	}

	private static SummaryBadgeData GetBadgeData(FoldableContainer foldable, NodeEditorProperty? editor)
	{
		if (!foldable.Folded || editor is null)
		{
			return SummaryBadgeData.Hidden;
		}

		string? highlightedVariableName = null;
		string? highlightedSharedVariableSetPath = null;
		string? highlightedSharedVariableName = null;

		if (editor.TryGetHighlightedVariableName(out string propagatedVariableName)
			&& !string.IsNullOrWhiteSpace(propagatedVariableName))
		{
			highlightedVariableName = propagatedVariableName;
		}

		if (editor.TryGetHighlightedSharedVariable(
			out string propagatedSharedVariableSetPath,
			out string propagatedSharedVariableName)
			&& !string.IsNullOrWhiteSpace(propagatedSharedVariableSetPath)
			&& !string.IsNullOrWhiteSpace(propagatedSharedVariableName))
		{
			highlightedSharedVariableSetPath = propagatedSharedVariableSetPath;
			highlightedSharedVariableName = propagatedSharedVariableName;
		}

		if (editor.TryGetInlineSummary(out string summary) && !string.IsNullOrWhiteSpace(summary))
		{
			InlineSummaryBadgeKind badgeKind = editor.GetInlineSummaryBadgeKind();
			return CreateBadgeData(
				summary,
				badgeKind,
				IsConstantBadgeKind(badgeKind),
				highlightedVariableName,
				highlightedSharedVariableSetPath,
				highlightedSharedVariableName);
		}

		return string.IsNullOrWhiteSpace(editor.DisplayName)
			? SummaryBadgeData.Hidden
			: CreateBadgeData(
				editor.DisplayName,
				InlineSummaryBadgeKind.Resolver,
				false,
				highlightedVariableName,
				highlightedSharedVariableSetPath,
				highlightedSharedVariableName);
	}

	private static SummaryBadgeData CreateBadgeData(
		string text,
		InlineSummaryBadgeKind badgeKind,
		bool isConstant,
		string? highlightedVariableName = null,
		string? highlightedSharedVariableSetPath = null,
		string? highlightedSharedVariableName = null)
	{
		BadgeVisualStyle style = GetBadgeStyle(badgeKind);
		return new SummaryBadgeData(
			GetBadgeIcon(badgeKind, isConstant),
			text,
			highlightedVariableName ?? string.Empty,
			highlightedSharedVariableSetPath ?? string.Empty,
			highlightedSharedVariableName ?? string.Empty,
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
			Name = "InlineSummaryBadge",
			Visible = false,
			MouseFilter = Control.MouseFilterEnum.Ignore,

			// The foldable title bar sizes its controls to content (it ignores expand flags), so the pill's width comes
			// entirely from the CustomMinimumSize set by SynchronizeSiblingBadgeWidths.
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
		foldable.SetMeta(SummaryBadgeMetaKey, Variant.From(true));
		foldable.SetMeta(SummaryBadgeInstanceIdMetaKey, Variant.From((long)badge.GetInstanceId()));
		return badge;
	}

	private static void EnsureResizeSyncHook(FoldableContainer foldable)
	{
		if (foldable.HasMeta(SummaryBadgeResizeHookMetaKey)
			&& foldable.GetMeta(SummaryBadgeResizeHookMetaKey).AsBool())
		{
			return;
		}

		foldable.Resized += () => ScheduleSync(foldable);
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
		badge.SetMeta(
			"forge_inline_summary_badge_highlight_variable",
			Variant.From(badgeData.HighlightVariableName));
		badge.SetMeta(
			"forge_inline_summary_badge_highlight_shared_set_path",
			Variant.From(badgeData.HighlightSharedVariableSetPath));
		badge.SetMeta(
			"forge_inline_summary_badge_highlight_shared_variable",
			Variant.From(badgeData.HighlightSharedVariableName));

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
		badge.SetMeta("forge_inline_summary_badge_base_stylebox", Variant.From(styleBox.Duplicate()));

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

		if (badge.HasMeta(SummaryBadgeSelectedSharedVariableSetPathMetaKey)
			&& badge.HasMeta(SummaryBadgeSelectedSharedVariableMetaKey))
		{
			string selectedSharedVariableSetPath = badge.GetMeta(SummaryBadgeSelectedSharedVariableSetPathMetaKey)
				.AsString();
			string selectedSharedVariableName = badge.GetMeta(SummaryBadgeSelectedSharedVariableMetaKey)
				.AsString();

			if (!string.IsNullOrEmpty(selectedSharedVariableSetPath)
				&& !string.IsNullOrEmpty(selectedSharedVariableName)
				&& selectedSharedVariableSetPath == badgeData.HighlightSharedVariableSetPath
				&& selectedSharedVariableName == badgeData.HighlightSharedVariableName)
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
		if (!foldable.HasMeta(SummaryBadgeMetaKey) || !foldable.HasMeta(SummaryBadgeInstanceIdMetaKey))
		{
			return false;
		}

		ulong badgeInstanceId = (ulong)foldable.GetMeta(SummaryBadgeInstanceIdMetaKey).AsInt64();
		if (badgeInstanceId == 0)
		{
			return false;
		}

		if (GodotObject.InstanceFromId(badgeInstanceId) is PanelContainer existingBadge
			&& GodotObject.IsInstanceValid(existingBadge))
		{
			badge = existingBadge;
			return true;
		}

		return false;
	}

	private static void ScheduleSync(FoldableContainer foldable)
	{
		// Defer to idle so the rows are measured after this change's layout has settled. Without this, adding a nested
		// resolver (or any rebuild) measures stale sizes and the pills render past the node edge until the next resize.
		const string syncMetaKey = "forge_inline_summary_badge_sync_scheduled";

		Node root = foldable;
		while (root.GetParent() is Node parent)
		{
			root = parent;
			if (root is GraphNode)
			{
				break;
			}
		}

		if (root.HasMeta(syncMetaKey))
		{
			return;
		}

		root.SetMeta(syncMetaKey, Variant.From(true));

		Callable.From(() =>
		{
			if (!GodotObject.IsInstanceValid(foldable) || !GodotObject.IsInstanceValid(root))
			{
				return;
			}

			root.RemoveMeta(syncMetaKey);
			SynchronizeSiblingBadgeWidths(foldable);
		}).CallDeferred();
	}

	private static void SynchronizeSiblingBadgeWidths(FoldableContainer foldable)
	{
		// Gather every collapsed (pill-showing) property foldable in the whole node, across both the Input Properties
		// and Output Variables sections and any nesting depth, so the label/pill split is computed once and the pills
		// line up node-wide rather than per section.
		Node root = foldable;
		while (root.GetParent() is Node parent)
		{
			root = parent;
			if (root is GraphNode)
			{
				break;
			}
		}

		var columnedFoldables = new List<FoldableContainer>();
		CollectColumnedFoldables(root, columnedFoldables);

		if (columnedFoldables.Count == 0)
		{
			return;
		}

		// Clear current minimums so a stale pill width can't skew the row measurements below.
		foreach (FoldableContainer columned in columnedFoldables)
		{
			if (TryGetSummaryBadge(columned, out PanelContainer? badge))
			{
				badge.CustomMinimumSize = Vector2.Zero;
			}
		}

		// Group rows by nesting level: top-level rows (no badged-foldable ancestor) span both sections and align as one
		// group; each nested resolver's rows form their own group, aligned among themselves and naturally indented.
		// This also stops a narrow nested row from shrinking the top-level label column and clipping the outer labels.
		var rowsByGroup = new Dictionary<ulong, List<FoldableContainer>>();
		foreach (FoldableContainer columned in columnedFoldables)
		{
			ulong groupKey = FindBadgedAncestorFoldable(columned)?.GetInstanceId() ?? 0UL;
			if (!rowsByGroup.TryGetValue(groupKey, out List<FoldableContainer>? group))
			{
				group = [];
				rowsByGroup[groupKey] = group;
			}

			group.Add(columned);
		}

		foreach (List<FoldableContainer> group in rowsByGroup.Values)
		{
			ApplyColumnedLayout(group);
		}
	}

	private static void ApplyColumnedLayout(List<FoldableContainer> group)
	{
		float longestLabelWidth = 0f;
		float narrowestRowWidth = float.MaxValue;
		foreach (FoldableContainer columned in group)
		{
			longestLabelWidth = Math.Max(longestLabelWidth, MeasureFoldableTitleWidth(columned));
			float rowWidth = RowContentWidth(columned);
			if (rowWidth > 0f)
			{
				narrowestRowWidth = Math.Min(narrowestRowWidth, rowWidth);
			}
		}

		if (float.IsPositiveInfinity(narrowestRowWidth) || narrowestRowWidth <= 0f)
		{
			// Rows are not laid out yet; the resize hook re-runs this once they have a real width.
			return;
		}

		// The label column takes only what the longest label in this group needs (plus a small gap), but never more
		// than half the narrowest row so it can't starve the pill. The pill column then gets whatever width is left
		// over. So once every label fits, extra node width flows into the pills instead of padding the labels. When
		// labels don't fit, this clamps near 50/50, with the center nudge shifting the split toward the pill to offset
		// the fold arrow on the label side (so the two columns read evenly); labels ellipsize.
		float labelColumnWidth = Math.Min(
			longestLabelWidth + LabelColumnGap,
			Math.Max(0f, (narrowestRowWidth / 2f) - LabelColumnCenterNudge));

		foreach (FoldableContainer columned in group)
		{
			if (!TryGetSummaryBadge(columned, out PanelContainer? badge))
			{
				continue;
			}

			badge.CustomMinimumSize = new Vector2(
				Math.Max(MinimumBadgeWidth, RowContentWidth(columned) - labelColumnWidth),
				0);
		}
	}

	private static FoldableContainer? FindBadgedAncestorFoldable(FoldableContainer foldable)
	{
		for (Node? ancestor = foldable.GetParent(); ancestor is not null; ancestor = ancestor.GetParent())
		{
			if (ancestor is FoldableContainer ancestorFoldable && TryGetSummaryBadge(ancestorFoldable, out _))
			{
				return ancestorFoldable;
			}
		}

		return null;
	}

	private static void CollectColumnedFoldables(Node node, List<FoldableContainer> result)
	{
		foreach (Node child in node.GetChildren())
		{
			if (child is FoldableContainer foldable
				&& TryGetSummaryBadge(foldable, out PanelContainer? badge)
				&& badge.Visible)
			{
				result.Add(foldable);
			}

			CollectColumnedFoldables(child, result);
		}
	}

	private static float RowContentWidth(FoldableContainer foldable)
	{
		return foldable.Size.X - FoldableTitleChromeWidth - FoldableTitleBadgeGap - FoldableTitleRightPadding;
	}

	private static float MeasureFoldableTitleWidth(FoldableContainer foldable)
	{
		Font? font = foldable.GetThemeDefaultFont();
		if (font is null)
		{
			return 0f;
		}

		int fontSize = foldable.GetThemeDefaultFontSize();
		return font.GetStringSize(
			foldable.Title,
			HorizontalAlignment.Left,
			-1,
			fontSize,
			TextServer.JustificationFlag.None,
			TextServer.Direction.Auto,
			TextServer.Orientation.Horizontal).X;
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
			TextServer.Direction.Auto,
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
		return $"({FormatNumber(value.Normal.X)}, {FormatNumber(value.Normal.Y)}, {FormatNumber(value.Normal.Z)}, " +
			$"{FormatNumber(value.D)})";
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
		string HighlightSharedVariableSetPath,
		string HighlightSharedVariableName,
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
