// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Godot;
using ForgeVariant128 = Gamesmiths.Forge.Statescript.Variant128;
using NumericsVector2 = System.Numerics.Vector2;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers;

/// <summary>
/// Resolver editor that reports whether a point falls inside a cone.
/// </summary>
[Tool]
internal sealed partial class IsInCone2DResolverEditor : NodeEditorProperty
{
	private const double DefaultAngle = 90.0;

	private static readonly Type[] _pointExpectedTypes = [typeof(NumericsVector2)];
	private static readonly Type[] _numberExpectedTypes = [typeof(double)];

	private Action? _onChanged;
	private NestedResolverPicker? _pointPicker;
	private NestedResolverPicker? _originPicker;
	private NestedResolverPicker? _directionPicker;
	private NestedResolverPicker? _anglePicker;
	private NestedResolverPicker? _rangePicker;

	public override string DisplayName => "Is In Cone 2D";

	public override string ResolverTypeId => "IsInCone2D";

	public override bool IsCompatibleWith(Type expectedType)
	{
		return expectedType == typeof(bool) || expectedType == typeof(ForgeVariant128);
	}

	public override void Setup(
		StatescriptGraph graph,
		StatescriptNodeProperty? property,
		Type expectedType,
		Action onChanged,
		bool isArray)
	{
		var resource = property?.Resolver as IsInCone2DResolverResource;
		_onChanged = onChanged;

		SizeFlagsHorizontal = SizeFlags.ExpandFill;
		var root = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		AddChild(root);

		// The point starts on the target's position and the cone on the caster's, which is the question this is asked
		// most: is my target in front of me. Inside a Where the point is repointed at the iterated element.
#pragma warning disable SA1118 // Parameter should not span multiple lines
		_pointPicker = AddPicker(
			graph,
			root,
			"Point:",
			resource?.Point
				?? new EntityPosition2DResolverResource { EntityResolver = new AbilityTargetResolverResource() },
			_pointExpectedTypes,
			resource?.PointFolded ?? false);
#pragma warning restore SA1118 // Parameter should not span multiple lines

		_originPicker = AddPicker(
			graph,
			root,
			"Origin:",
			resource?.Origin ?? new EntityPosition2DResolverResource(),
			_pointExpectedTypes,
			resource?.OriginFolded ?? true);

		_directionPicker = AddPicker(
			graph,
			root,
			"Direction:",
			resource?.Direction ?? new EntityDirection2DResolverResource(),
			_pointExpectedTypes,
			resource?.DirectionFolded ?? true);

		_anglePicker = AddPicker(
			graph,
			root,
			"Angle (deg):",
			resource?.Angle ?? BuildNumber(DefaultAngle),
			_numberExpectedTypes,
			resource?.AngleFolded ?? false);

		// Left unseeded, unlike the query's: this is usually filtering something a query already limited by range, and
		// an unset reach means no limit rather than no reach.
		_rangePicker = AddPicker(
			graph,
			root,
			"Range:",
			resource?.Range,
			_numberExpectedTypes,
			resource?.RangeFolded ?? true);
	}

	public override void SaveTo(StatescriptNodeProperty property)
	{
		property.Resolver = new IsInCone2DResolverResource
		{
			Point = _pointPicker?.BuildResource(),
			PointFolded = _pointPicker?.Folded ?? false,
			Origin = _originPicker?.BuildResource(),
			OriginFolded = _originPicker?.Folded ?? true,
			Direction = _directionPicker?.BuildResource(),
			DirectionFolded = _directionPicker?.Folded ?? true,
			Angle = _anglePicker?.BuildResource(),
			AngleFolded = _anglePicker?.Folded ?? false,
			Range = _rangePicker?.BuildResource(),
			RangeFolded = _rangePicker?.Folded ?? true,
		};
	}

	public override bool TryGetInlineSummary(out string summary)
	{
		summary = "Is In Cone 2D";
		return true;
	}

	public override void ClearCallbacks()
	{
		base.ClearCallbacks();
		_onChanged = null;
		_pointPicker?.ClearCallbacks();
		_pointPicker = null;
		_originPicker?.ClearCallbacks();
		_originPicker = null;
		_directionPicker?.ClearCallbacks();
		_directionPicker = null;
		_anglePicker?.ClearCallbacks();
		_anglePicker = null;
		_rangePicker?.ClearCallbacks();
		_rangePicker = null;
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

	private void NotifyChanged()
	{
		_onChanged?.Invoke();
	}
}
#endif
