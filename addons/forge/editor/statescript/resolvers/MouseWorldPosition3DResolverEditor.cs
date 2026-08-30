// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using Gamesmiths.Forge.Godot.Core.Statescript.Resolvers;
using Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Godot;
using ForgeVariant128 = Gamesmiths.Forge.Statescript.Variant128;
using NumericsVector3 = System.Numerics.Vector3;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers;

/// <summary>
/// Resolver editor that reads the point in the world the mouse cursor is over.
/// </summary>
[Tool]
internal sealed partial class MouseWorldPosition3DResolverEditor : NodeEditorProperty
{
	private const float LabelWidth = 74.0f;
	private const double DefaultMaxDistance = 1000.0;

	private static readonly string[] _modeNames = ["Physics Ray", "Plane Intersect"];
	private static readonly Type[] _maskExpectedTypes = [typeof(int)];
	private static readonly Type[] _maxDistanceExpectedTypes = [typeof(double)];

	private Action? _onChanged;
	private OptionButton? _modeDropdown;
	private NestedResolverPicker? _maskPicker;
	private NestedResolverPicker? _maxDistancePicker;

	public override string DisplayName => "Mouse World Position 3D";

	public override string ResolverTypeId => "MouseWorldPosition3D";

	public override bool IsCompatibleWith(Type expectedType)
	{
		return expectedType == typeof(NumericsVector3) || expectedType == typeof(ForgeVariant128);
	}

	public override void Setup(
		StatescriptGraph graph,
		StatescriptNodeProperty? property,
		Type expectedType,
		Action onChanged,
		bool isArray)
	{
		var resource = property?.Resolver as MouseWorldPosition3DResolverResource;
		_onChanged = onChanged;

		SizeFlagsHorizontal = SizeFlags.ExpandFill;
		var root = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		AddChild(root);

		_modeDropdown = new OptionButton { SizeFlagsHorizontal = SizeFlags.ExpandFill };

		foreach (string modeName in _modeNames)
		{
			_modeDropdown.AddItem(modeName);
		}

		_modeDropdown.Selected = (int)(resource?.Mode ?? MouseWorldMode.PhysicsRay);
		_modeDropdown.ItemSelected += _ => OnModeSelected();
		root.AddChild(ResolverEditorLayoutUtilities.CreateLabeledRow("Mode:", _modeDropdown, LabelWidth));

		_maskPicker = AddPicker(
			graph,
			root,
			"Mask:",
			resource?.Mask,
			_maskExpectedTypes,
			resource?.MaskFolded ?? true);

		// Seeded with the same constant the resource falls back to: a nested operand has no unbound state, so an
		// untouched distance would be zero and every query would resolve onto the camera itself.
		_maxDistancePicker = AddPicker(
			graph,
			root,
			"Max Dist:",
			resource?.MaxDistance ?? BuildDefaultMaxDistance(),
			_maxDistanceExpectedTypes,
			resource?.MaxDistanceFolded ?? true);

		UpdateMaskVisibility();
	}

	public override void SaveTo(StatescriptNodeProperty property)
	{
		property.Resolver = new MouseWorldPosition3DResolverResource
		{
			Mode = (MouseWorldMode)(_modeDropdown?.Selected ?? 0),
			Mask = _maskPicker?.BuildResource(),
			MaskFolded = _maskPicker?.Folded ?? true,
			MaxDistance = _maxDistancePicker?.BuildResource(),
			MaxDistanceFolded = _maxDistancePicker?.Folded ?? true,
		};
	}

	public override bool TryGetInlineSummary(out string summary)
	{
		summary = "Mouse World Position 3D";
		return true;
	}

	public override void ClearCallbacks()
	{
		base.ClearCallbacks();
		_onChanged = null;
		_modeDropdown = null;
		_maskPicker?.ClearCallbacks();
		_maskPicker = null;
		_maxDistancePicker?.ClearCallbacks();
		_maxDistancePicker = null;
	}

	private static VariantResolverResource BuildDefaultMaxDistance()
	{
		return new VariantResolverResource
		{
			Value = DefaultMaxDistance,
			ValueType = StatescriptVariableType.Double,
		};
	}

	private NestedResolverPicker AddPicker(
		StatescriptGraph graph,
		VBoxContainer root,
		string title,
		StatescriptResolverResource? existing,
		Type[] expectedTypes,
		bool folded)
	{
		var picker = new NestedResolverPicker();
		picker.Initialize(
			graph,
			existing,
			title,
			expectedTypes,
			isArray: false,
			folded,
			NotifyChanged,
			RaiseLayoutSizeChanged,
			IterationScope);

		root.AddChild(picker);
		return picker;
	}

	private void OnModeSelected()
	{
		UpdateMaskVisibility();
		NotifyChanged();
		RaiseLayoutSizeChanged();
	}

	// Plane Intersect asks nothing of physics, so the mask is not merely unused there - it would be a row an author
	// can fill in and have silently ignored.
	private void UpdateMaskVisibility()
	{
		if (_maskPicker is not null && IsInstanceValid(_maskPicker))
		{
			_maskPicker.Visible = _modeDropdown?.Selected == (int)MouseWorldMode.PhysicsRay;
		}
	}

	private void NotifyChanged()
	{
		_onChanged?.Invoke();
	}
}
#endif
