// Copyright © Gamesmiths Guild.

#if TOOLS
using System.Globalization;
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Statescript;

internal static class InlineConstantSummaryFormatter
{
	private const string SummaryLabelMetaKey = "forge_inline_summary_label";
	private static readonly Color _summaryColor = new(0x61afefff);

	public static void ApplyFoldableTitle(
		string baseTitle,
		FoldableContainer foldable,
		NodeEditorProperty? editor)
	{
		foldable.Title = baseTitle;

		string suffix = GetSuffixText(foldable, editor);
		Label summaryLabel = GetOrCreateSummaryLabel(foldable);
		summaryLabel.Text = suffix;
		summaryLabel.Visible = !string.IsNullOrWhiteSpace(suffix);
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

	private static string GetSuffixText(FoldableContainer foldable, NodeEditorProperty? editor)
	{
		if (!foldable.Folded || editor is null)
		{
			return string.Empty;
		}

		if (editor.TryGetInlineSummary(out string summary) && !string.IsNullOrWhiteSpace(summary))
		{
			return $" {summary}";
		}

		return string.IsNullOrWhiteSpace(editor.DisplayName)
			? string.Empty
			: $" {editor.DisplayName}";
	}

	private static Label GetOrCreateSummaryLabel(FoldableContainer foldable)
	{
		if (foldable.HasMeta(SummaryLabelMetaKey))
		{
			Variant existing = foldable.GetMeta(SummaryLabelMetaKey);
			if (existing.Obj is Label existingLabel && GodotObject.IsInstanceValid(existingLabel))
			{
				return existingLabel;
			}
		}

		var label = new Label
		{
			Visible = false,
			MouseFilter = Control.MouseFilterEnum.Ignore,
		};

		label.AddThemeColorOverride("font_color", _summaryColor);
		foldable.AddTitleBarControl(label);
		foldable.SetMeta(SummaryLabelMetaKey, Variant.From(label));
		return label;
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
}
#endif
