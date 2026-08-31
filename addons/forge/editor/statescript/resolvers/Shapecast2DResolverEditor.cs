// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Godot;
using NumericsVector2 = System.Numerics.Vector2;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers;

/// <summary>
/// Resolver editor that reads the first entity a shape swept through the world meets.
/// </summary>
[Tool]
internal sealed partial class Shapecast2DResolverEditor : NodeEditorProperty
{
	private const double DefaultMaxDistance = 400.0;

	private static readonly Type[] _shapeExpectedTypes = [typeof(Shape2D)];
	private static readonly Type[] _pointExpectedTypes = [typeof(NumericsVector2)];
	private static readonly Type[] _numberExpectedTypes = [typeof(double)];
	private static readonly Type[] _maskExpectedTypes = [typeof(int)];
	private static readonly Type[] _ignoreExpectedTypes = [typeof(IForgeEntity)];

	private Action? _onChanged;
	private NestedResolverPicker? _shapePicker;
	private NestedResolverPicker? _originPicker;
	private NestedResolverPicker? _directionPicker;
	private NestedResolverPicker? _maxDistancePicker;
	private NestedResolverPicker? _rotationPicker;
	private NestedResolverPicker? _maskPicker;
	private NestedResolverPicker? _ignorePicker;
	private CheckBox? _collideWithAreasCheckBox;

	public override string DisplayName => "Shapecast 2D";

	public override string ResolverTypeId => "Shapecast2D";

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
		var resource = property?.Resolver as Shapecast2DResolverResource;
		_onChanged = onChanged;

		SizeFlagsHorizontal = SizeFlags.ExpandFill;
		var root = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		AddChild(root);

		_collideWithAreasCheckBox = new CheckBox
		{
			Text = "Collide with areas",
			ButtonPressed = resource?.CollideWithAreas ?? false,
			TooltipText = "Let areas stop the sweep as well as bodies, for entities whose hurtbox is an Area2D.",
		};
		_collideWithAreasCheckBox.Toggled += _ => NotifyChanged();
		root.AddChild(_collideWithAreasCheckBox);

		// Seeded the same way the ray family is: a circle to sweep, the caster's own position and facing, a reach that
		// is not zero, and the caster left out - which together make a fresh row a working cast rather than one that
		// stops on its own collider at zero distance. The rotation is deliberately not seeded, since most sweeps are
		// unturned and zero radians is exactly that.
		_shapePicker = AddPicker(
			graph,
			root,
			"Shape:",
			resource?.Shape ?? new CircleShape2DResolverResource(),
			_shapeExpectedTypes,
			isArray: false,
			resource?.ShapeFolded ?? false);

		_originPicker = AddPicker(
			graph,
			root,
			"Origin:",
			resource?.Origin ?? new EntityPosition2DResolverResource(),
			_pointExpectedTypes,
			isArray: false,
			resource?.OriginFolded ?? true);

		_directionPicker = AddPicker(
			graph,
			root,
			"Direction:",
			resource?.Direction ?? new EntityDirection2DResolverResource(),
			_pointExpectedTypes,
			isArray: false,
			resource?.DirectionFolded ?? true);

		_maxDistancePicker = AddPicker(
			graph,
			root,
			"Max Dist:",
			resource?.MaxDistance ?? BuildNumber(DefaultMaxDistance),
			_numberExpectedTypes,
			isArray: false,
			resource?.MaxDistanceFolded ?? false);

		_rotationPicker = AddPicker(
			graph,
			root,
			"Rotation:",
			resource?.Rotation,
			_numberExpectedTypes,
			isArray: false,
			resource?.RotationFolded ?? true);

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
		property.Resolver = new Shapecast2DResolverResource
		{
			Shape = _shapePicker?.BuildResource(),
			ShapeFolded = _shapePicker?.Folded ?? false,
			Origin = _originPicker?.BuildResource(),
			OriginFolded = _originPicker?.Folded ?? true,
			Direction = _directionPicker?.BuildResource(),
			DirectionFolded = _directionPicker?.Folded ?? true,
			MaxDistance = _maxDistancePicker?.BuildResource(),
			MaxDistanceFolded = _maxDistancePicker?.Folded ?? false,
			Rotation = _rotationPicker?.BuildResource(),
			RotationFolded = _rotationPicker?.Folded ?? true,
			Mask = _maskPicker?.BuildResource(),
			MaskFolded = _maskPicker?.Folded ?? true,
			IgnoreResolver = _ignorePicker?.BuildResource(),
			IgnoreFolded = _ignorePicker?.Folded ?? true,
			CollideWithAreas = _collideWithAreasCheckBox is not null
				&& IsInstanceValid(_collideWithAreasCheckBox)
				&& _collideWithAreasCheckBox.ButtonPressed,
		};
	}

	public override bool TryGetInlineSummary(out string summary)
	{
		summary = "Shapecast 2D";
		return true;
	}

	public override void ClearCallbacks()
	{
		base.ClearCallbacks();
		_onChanged = null;
		_shapePicker?.ClearCallbacks();
		_shapePicker = null;
		_originPicker?.ClearCallbacks();
		_originPicker = null;
		_directionPicker?.ClearCallbacks();
		_directionPicker = null;
		_maxDistancePicker?.ClearCallbacks();
		_maxDistancePicker = null;
		_rotationPicker?.ClearCallbacks();
		_rotationPicker = null;
		_maskPicker?.ClearCallbacks();
		_maskPicker = null;
		_ignorePicker?.ClearCallbacks();
		_ignorePicker = null;
		_collideWithAreasCheckBox = null;
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
