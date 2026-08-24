// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Godot;
using NumericsQuaternion = System.Numerics.Quaternion;
using NumericsVector3 = System.Numerics.Vector3;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers;

/// <summary>
/// Resolver editor that reads the entities inside a shape swept through the world at query time.
/// </summary>
[Tool]
internal sealed partial class Overlap3DResolverEditor : NodeEditorProperty
{
	private static readonly Type[] _shapeExpectedTypes = [typeof(Shape3D)];
	private static readonly Type[] _positionExpectedTypes = [typeof(NumericsVector3)];
	private static readonly Type[] _rotationExpectedTypes = [typeof(NumericsQuaternion)];
	private static readonly Type[] _ignoreExpectedTypes = [typeof(IForgeEntity)];
	private static readonly Type[] _maskExpectedTypes = [typeof(int)];

	private Action? _onChanged;
	private NestedResolverPicker? _shapePicker;
	private NestedResolverPicker? _positionPicker;
	private NestedResolverPicker? _rotationPicker;
	private NestedResolverPicker? _ignorePicker;
	private NestedResolverPicker? _maskPicker;
	private CheckBox? _includeAreasCheckBox;

	public override string DisplayName => "Overlap 3D";

	public override string ResolverTypeId => "Overlap3D";

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
		var resource = property?.Resolver as Overlap3DResolverResource;
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

		BuildOperandPickers(graph, resource, root);
	}

	public override void SaveTo(StatescriptNodeProperty property)
	{
		property.Resolver = new Overlap3DResolverResource
		{
			Shape = _shapePicker?.BuildResource(),
			ShapeFolded = _shapePicker?.Folded ?? false,
			Position = _positionPicker?.BuildResource(),
			PositionFolded = _positionPicker?.Folded ?? false,
			Rotation = _rotationPicker?.BuildResource(),
			RotationFolded = _rotationPicker?.Folded ?? true,
			IgnoreResolver = _ignorePicker?.BuildResource(),
			IgnoreFolded = _ignorePicker?.Folded ?? true,
			Mask = _maskPicker?.BuildResource(),
			MaskFolded = _maskPicker?.Folded ?? true,
			IncludeAreas = _includeAreasCheckBox is not null
				&& IsInstanceValid(_includeAreasCheckBox)
				&& _includeAreasCheckBox.ButtonPressed,
		};
	}

	public override bool TryGetInlineSummary(out string summary)
	{
		summary = "Overlap 3D";
		return true;
	}

	public override void ClearCallbacks()
	{
		base.ClearCallbacks();
		_onChanged = null;
		_shapePicker?.ClearCallbacks();
		_positionPicker?.ClearCallbacks();
		_rotationPicker?.ClearCallbacks();
		_ignorePicker?.ClearCallbacks();
		_shapePicker = null;
		_positionPicker = null;
		_rotationPicker = null;
		_ignorePicker = null;
		_maskPicker?.ClearCallbacks();
		_maskPicker = null;
		_includeAreasCheckBox = null;
	}

	private void BuildOperandPickers(
		StatescriptGraph graph,
		Overlap3DResolverResource? resource,
		VBoxContainer root)
	{
		// Seeded with a sphere, an Entity Position 3D and the caster: a nested operand has no unbound state, so an
		// unseeded shape would be nothing, an unseeded position would be the world origin, and an unseeded ignore would
		// let a blast catch the caster who cast it. The rotation is deliberately not seeded - most queries are upright,
		// and an unset one is read as no rotation at all.
		_shapePicker = AddPicker(
			graph,
			root,
			"Shape:",
			resource?.Shape ?? new SphereShape3DResolverResource(),
			_shapeExpectedTypes,
			isArray: false,
			resource?.ShapeFolded ?? false);

		_positionPicker = AddPicker(
			graph,
			root,
			"Position:",
			resource?.Position ?? new EntityPosition3DResolverResource(),
			_positionExpectedTypes,
			isArray: false,
			resource?.PositionFolded ?? true);

		_rotationPicker = AddPicker(
			graph,
			root,
			"Rotation:",
			resource?.Rotation,
			_rotationExpectedTypes,
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
