// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Godot;
using ForgeVariant128 = Gamesmiths.Forge.Statescript.Variant128;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers;

[Tool]
internal sealed partial class CurveSampleResolverEditor : NodeEditorProperty
{
	private const float LabelWidth = 60.0f;

	private Action? _onChanged;
	private Curve? _selectedCurve;
	private NestedResolverPicker? _timePicker;

	public override string DisplayName => "Curve Sample";

	public override string ResolverTypeId => "CurveSample";

	public override bool IsCompatibleWith(Type expectedType)
	{
		return expectedType == typeof(float)
			|| expectedType == typeof(double)
			|| expectedType == typeof(ForgeVariant128);
	}

	public override void Setup(
		StatescriptGraph graph,
		StatescriptNodeProperty? property,
		Type expectedType,
		Action onChanged,
		bool isArray)
	{
		_onChanged = onChanged;

		var existingResource = property?.Resolver as CurveSampleResolverResource;
		_selectedCurve = existingResource?.Curve;

		SizeFlagsHorizontal = SizeFlags.ExpandFill;
		var root = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		AddChild(root);

		var curvePicker = new EditorResourcePicker
		{
			BaseType = "Curve",
			EditedResource = _selectedCurve,
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
		};
		curvePicker.ResourceChanged += resource =>
		{
			_selectedCurve = resource as Curve;
			_onChanged?.Invoke();
		};
		root.AddChild(ResolverEditorLayoutUtilities.CreateLabeledRow("Curve:", curvePicker, LabelWidth));

		_timePicker = new NestedResolverPicker();
		_timePicker.Initialize(
			graph,
			existingResource?.Time,
			"Time:",
			[typeof(int), typeof(float), typeof(double)],
			isArray: false,
			folded: false,
			() => _onChanged?.Invoke(),
			RaiseLayoutSizeChanged,
			IterationScope);
		root.AddChild(_timePicker);
	}

	public override void SaveTo(StatescriptNodeProperty property)
	{
		property.Resolver = new CurveSampleResolverResource
		{
			Curve = _selectedCurve,
			Time = _timePicker?.BuildResource(),
		};
	}

	public override void ClearCallbacks()
	{
		base.ClearCallbacks();
		_onChanged = null;
		_timePicker?.ClearCallbacks();
	}

	public override bool TryGetHighlightedVariableName(out string variableName)
	{
		if (_timePicker is not null && _timePicker.TryGetHighlightedVariableName(out variableName))
		{
			return true;
		}

		variableName = string.Empty;
		return false;
	}
}
#endif
