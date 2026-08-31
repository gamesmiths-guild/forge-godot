// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Godot;
using NumericsVector3 = System.Numerics.Vector3;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers;

/// <summary>
/// Resolver editor that reads the entities standing inside a cone opening from a point.
/// </summary>
[Tool]
internal sealed partial class EntitiesInCone3DResolverEditor : NodeEditorProperty
{
	private const double DefaultRange = 5.0;
	private const double DefaultAngle = 90.0;

	private static readonly Type[] _pointExpectedTypes = [typeof(NumericsVector3)];
	private static readonly Type[] _numberExpectedTypes = [typeof(double)];
	private static readonly Type[] _maskExpectedTypes = [typeof(int)];
	private static readonly Type[] _ignoreExpectedTypes = [typeof(IForgeEntity)];

	private Action? _onChanged;
	private NestedResolverPicker? _originPicker;
	private NestedResolverPicker? _directionPicker;
	private NestedResolverPicker? _rangePicker;
	private NestedResolverPicker? _anglePicker;
	private NestedResolverPicker? _maskPicker;
	private NestedResolverPicker? _ignorePicker;
	private CheckBox? _includeAreasCheckBox;

	public override string DisplayName => "Entities In Cone 3D";

	public override string ResolverTypeId => "EntitiesInCone3D";

	public override bool SupportsScalarValues => false;

	public override bool SupportsArrayValues => true;

	public override bool IsCompatibleWith(Type expectedType)
	{
		return expectedType == typeof(IForgeEntity);
	}

	public override void Setup(
		StatescriptGraph graph,
		StatescriptNodeProperty? property,
		Type expectedType,
		Action onChanged,
		bool isArray)
	{
		var resource = property?.Resolver as EntitiesInCone3DResolverResource;
		_onChanged = onChanged;

		SizeFlagsHorizontal = SizeFlags.ExpandFill;
		var root = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		AddChild(root);

		_includeAreasCheckBox = new CheckBox
		{
			Text = "Include areas",
			ButtonPressed = resource?.IncludeAreas ?? false,
			TooltipText = "Count areas as overlaps as well as bodies, for entities whose hurtbox is an Area3D.",
		};
		_includeAreasCheckBox.Toggled += _ => NotifyChanged();
		root.AddChild(_includeAreasCheckBox);

		// Every operand is seeded, because a nested one has no unbound state: an unseeded origin would be the world
		// origin, an unseeded direction would be the zero vector, and an unseeded range or angle would be a cone that
		// finds nobody. A fresh row is therefore a working cleave in front of the caster.
		_originPicker = AddPicker(
			graph,
			root,
			"Origin:",
			resource?.Origin ?? new EntityPosition3DResolverResource(),
			_pointExpectedTypes,
			isArray: false,
			resource?.OriginFolded ?? true);

		_directionPicker = AddPicker(
			graph,
			root,
			"Direction:",
			resource?.Direction ?? new EntityDirection3DResolverResource(),
			_pointExpectedTypes,
			isArray: false,
			resource?.DirectionFolded ?? true);

		_rangePicker = AddPicker(
			graph,
			root,
			"Range:",
			resource?.Range ?? BuildNumber(DefaultRange),
			_numberExpectedTypes,
			isArray: false,
			resource?.RangeFolded ?? false);

		_anglePicker = AddPicker(
			graph,
			root,
			"Angle (deg):",
			resource?.Angle ?? BuildNumber(DefaultAngle),
			_numberExpectedTypes,
			isArray: false,
			resource?.AngleFolded ?? false);

		_maskPicker = AddPicker(
			graph,
			root,
			"Mask:",
			resource?.Mask,
			_maskExpectedTypes,
			isArray: false,
			resource?.MaskFolded ?? true);

		_ignorePicker = AddPicker(
			graph,
			root,
			"Ignore:",
			resource?.IgnoreResolver ?? EntityIgnoreOperand.BuildOwner(),
			_ignoreExpectedTypes,
			isArray: true,
			resource?.IgnoreFolded ?? true);
	}

	public override void SaveTo(StatescriptNodeProperty property)
	{
		property.Resolver = new EntitiesInCone3DResolverResource
		{
			Origin = _originPicker?.BuildResource(),
			OriginFolded = _originPicker?.Folded ?? true,
			Direction = _directionPicker?.BuildResource(),
			DirectionFolded = _directionPicker?.Folded ?? true,
			Range = _rangePicker?.BuildResource(),
			RangeFolded = _rangePicker?.Folded ?? false,
			Angle = _anglePicker?.BuildResource(),
			AngleFolded = _anglePicker?.Folded ?? false,
			Mask = _maskPicker?.BuildResource(),
			MaskFolded = _maskPicker?.Folded ?? true,
			IgnoreResolver = _ignorePicker?.BuildResource(),
			IgnoreFolded = _ignorePicker?.Folded ?? true,
			IncludeAreas = _includeAreasCheckBox is not null
				&& IsInstanceValid(_includeAreasCheckBox)
				&& _includeAreasCheckBox.ButtonPressed,
		};
	}

	public override bool TryGetInlineSummary(out string summary)
	{
		summary = "Entities In Cone 3D";
		return true;
	}

	public override void ClearCallbacks()
	{
		base.ClearCallbacks();
		_onChanged = null;
		_originPicker?.ClearCallbacks();
		_originPicker = null;
		_directionPicker?.ClearCallbacks();
		_directionPicker = null;
		_rangePicker?.ClearCallbacks();
		_rangePicker = null;
		_anglePicker?.ClearCallbacks();
		_anglePicker = null;
		_maskPicker?.ClearCallbacks();
		_maskPicker = null;
		_ignorePicker?.ClearCallbacks();
		_ignorePicker = null;
		_includeAreasCheckBox = null;
	}

	private static VariantResolverResource BuildNumber(double value)
	{
		return new VariantResolverResource { Value = value, ValueType = StatescriptVariableType.Double };
	}

	private NestedResolverPicker AddPicker(
		StatescriptGraph graph,
		VBoxContainer root,
		string title,
		StatescriptResolverResource? existing,
		Type[] expectedTypes,
		bool isArray,
		bool folded)
	{
		var picker = new NestedResolverPicker();
		picker.Initialize(
			graph,
			existing,
			title,
			expectedTypes,
			isArray,
			folded,
			NotifyChanged,
			RaiseLayoutSizeChanged,
			IterationScope);

		root.AddChild(picker);
		return picker;
	}

	private void NotifyChanged()
	{
		_onChanged?.Invoke();
	}
}
#endif
