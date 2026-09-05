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
/// Resolver editor that reads the entities whose colliders contain a point.
/// </summary>
[Tool]
internal sealed partial class EntitiesAtPoint3DResolverEditor : NodeEditorProperty
{
	private static readonly Type[] _pointExpectedTypes = [typeof(NumericsVector3)];
	private static readonly Type[] _maskExpectedTypes = [typeof(int)];
	private static readonly Type[] _ignoreExpectedTypes = [typeof(IForgeEntity)];

	private Action? _onChanged;
	private NestedResolverPicker? _positionPicker;
	private NestedResolverPicker? _maskPicker;
	private NestedResolverPicker? _ignorePicker;
	private CheckBox? _includeAreasCheckBox;

	public override string DisplayName => "Entities At Point 3D";

	public override string ResolverTypeId => "EntitiesAtPoint3D";

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
		var resource = property?.Resolver as EntitiesAtPoint3DResolverResource;
		_onChanged = onChanged;

		SizeFlagsHorizontal = SizeFlags.ExpandFill;
		var root = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		AddChild(root);

		_includeAreasCheckBox = new CheckBox
		{
			Text = "Include areas",
			ButtonPressed = resource?.IncludeAreas ?? false,
			TooltipText = "Count areas as well as bodies, for entities whose hurtbox is an Area3D.",
		};
		_includeAreasCheckBox.Toggled += _ => NotifyChanged();
		root.AddChild(_includeAreasCheckBox);

		// Seeded with the caster's own position, because a nested operand has no unbound state and the world origin is
		// never the point anybody meant. A cursor or a marker is what usually replaces it.
		_positionPicker = AddPicker(
			graph,
			root,
			"Position:",
			resource?.Position ?? new EntityPosition3DResolverResource(),
			_pointExpectedTypes,
			isArray: false,
			resource?.PositionFolded ?? false);

		_maskPicker = AddPicker(
			graph,
			root,
			"Mask:",
			resource?.Mask,
			_maskExpectedTypes,
			isArray: false,
			resource?.MaskFolded ?? true);

		// Not seeded with the caster, unlike the casts and the overlaps: a point query has no body at either end to
		// start inside, so leaving the owner out would be a guess rather than a fix.
		_ignorePicker = AddPicker(
			graph,
			root,
			"Ignore:",
			resource?.IgnoreResolver,
			_ignoreExpectedTypes,
			isArray: true,
			resource?.IgnoreFolded ?? true);
	}

	public override void SaveTo(StatescriptNodeProperty property)
	{
		property.Resolver = new EntitiesAtPoint3DResolverResource
		{
			Position = _positionPicker?.BuildResource(),
			PositionFolded = _positionPicker?.Folded ?? false,
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
		summary = "Entities At Point 3D";
		return true;
	}

	public override void ClearCallbacks()
	{
		base.ClearCallbacks();
		_onChanged = null;
		_positionPicker?.ClearCallbacks();
		_positionPicker = null;
		_maskPicker?.ClearCallbacks();
		_maskPicker = null;
		_ignorePicker?.ClearCallbacks();
		_ignorePicker = null;
		_includeAreasCheckBox = null;
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
