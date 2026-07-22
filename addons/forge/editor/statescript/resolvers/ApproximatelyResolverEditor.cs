// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using System.Globalization;
using Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Godot;
using ForgeVariant128 = Gamesmiths.Forge.Statescript.Variant128;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers;

[Tool]
internal sealed partial class ApproximatelyResolverEditor
	: ScalarBinaryResolverEditorBase<ApproximatelyResolverResource>
{
	private const float LabelWidth = 60.0f;

	private double _tolerance = 1e-6;
	private LineEdit? _toleranceEdit;

	public override string DisplayName => "Approximately";

	public override string ResolverTypeId => "Approximately";

	protected override string LeftTitle => "A:";

	protected override string RightTitle => "B:";

	public override bool IsCompatibleWith(Type expectedType)
	{
		return expectedType == typeof(bool) || expectedType == typeof(ForgeVariant128);
	}

	public override void ClearCallbacks()
	{
		base.ClearCallbacks();
		_toleranceEdit = null;
	}

	protected override void BuildAdditionalRows(
		VBoxContainer container,
		ApproximatelyResolverResource? existingResource)
	{
		_tolerance = existingResource?.Tolerance ?? 1e-6;

		_toleranceEdit = new LineEdit
		{
			Text = FormatTolerance(_tolerance),
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			TooltipText = "Maximum absolute difference considered equal. Accepts scientific notation (1e-6).",
		};

		_toleranceEdit.TextChanged += OnToleranceTextChanged;
		_toleranceEdit.TextSubmitted += OnToleranceTextSubmitted;
		_toleranceEdit.FocusExited += OnToleranceFocusExited;

		container.AddChild(ResolverEditorLayoutUtilities.CreateLabeledRow("Tolerance:", _toleranceEdit, LabelWidth));
	}

	protected override void ApplyAdditionalProperties(ApproximatelyResolverResource resource)
	{
		resource.Tolerance = _tolerance;
	}

	private static string FormatTolerance(double tolerance)
	{
		return tolerance.ToString("0.################", CultureInfo.InvariantCulture);
	}

	private void OnToleranceTextChanged(string text)
	{
		// Commit live while typing (like the spin fields do) so running the scene without unfocusing the field
		// still picks up the value. The text is only reformatted on submit/focus-exit to not fight the typing.
		CommitToleranceText(text, reformat: false);
	}

	private void OnToleranceTextSubmitted(string text)
	{
		CommitToleranceText(text, reformat: true);
	}

	private void OnToleranceFocusExited()
	{
		if (_toleranceEdit is not null)
		{
			CommitToleranceText(_toleranceEdit.Text, reformat: true);
		}
	}

	private void CommitToleranceText(string text, bool reformat)
	{
		if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out double parsed)
			&& parsed >= 0
			&& double.IsFinite(parsed)
			&& BitConverter.DoubleToInt64Bits(parsed) != BitConverter.DoubleToInt64Bits(_tolerance))
		{
			_tolerance = parsed;
			NotifyChanged();
		}

		if (reformat)
		{
			_toleranceEdit?.SetText(FormatTolerance(_tolerance));
		}
	}
}
#endif
