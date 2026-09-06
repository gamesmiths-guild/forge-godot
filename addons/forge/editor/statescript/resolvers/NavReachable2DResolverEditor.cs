// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using Gamesmiths.Forge.Godot.Core.Statescript.Resolvers;
using Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Godot;
using NumericsVector2 = System.Numerics.Vector2;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers;

/// <summary>
/// Resolver editor that reports whether a destination can be walked to from a point.
/// </summary>
[Tool]
internal sealed partial class NavReachable2DResolverEditor : NodeEditorProperty
{
	private static readonly Type[] _pointExpectedTypes = [typeof(NumericsVector2)];
	private static readonly Type[] _toleranceExpectedTypes = [typeof(double)];

	private Action? _onChanged;
	private NestedResolverPicker? _fromPicker;
	private NestedResolverPicker? _toPicker;
	private NestedResolverPicker? _tolerancePicker;

	public override string DisplayName => "Nav Reachable 2D";

	public override string ResolverTypeId => "NavReachable2D";

	public override bool IsCompatibleWith(Type expectedType)
	{
		return expectedType == typeof(bool) || expectedType == typeof(Forge.Statescript.Variant128);
	}

	public override void Setup(
		StatescriptGraph graph,
		StatescriptNodeProperty? property,
		Type expectedType,
		Action onChanged,
		bool isArray)
	{
		var resource = property?.Resolver as NavReachable2DResolverResource;
		_onChanged = onChanged;

		SizeFlagsHorizontal = SizeFlags.ExpandFill;
		var root = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		AddChild(root);

		// Seeded with Entity Position 2D on both ends, matching Line Of Sight 2D: the two points are almost always
		// somebody's position, and a nested operand has no unbound state, so an unseeded one is silently the origin.
		_fromPicker = BuildPointPicker(graph, root, "From:", resource?.From, resource?.FromFolded ?? false);
		_toPicker = BuildPointPicker(graph, root, "To:", resource?.To, resource?.ToFolded ?? false);

		// Seeded with the agent default rather than left unset. A nested operand has no unbound state, so an unset
		// one is a constant at zero - and a tolerance of zero reports every destination unreachable, since a
		// destination is snapped onto the mesh before the path is built and never comes back exactly.
		_tolerancePicker = new NestedResolverPicker();
		_tolerancePicker.Initialize(
			graph,
			resource?.Tolerance ?? new VariantResolverResource { Value = NavReachable2DResolver.Tolerance },
			"Within:",
			_toleranceExpectedTypes,
			isArray: false,
			resource?.ToleranceFolded ?? true,
			NotifyChanged,
			RaiseLayoutSizeChanged,
			IterationScope);
		root.AddChild(_tolerancePicker);
	}

	public override void SaveTo(StatescriptNodeProperty property)
	{
		property.Resolver = new NavReachable2DResolverResource
		{
			From = _fromPicker?.BuildResource(),
			FromFolded = _fromPicker?.Folded ?? false,
			To = _toPicker?.BuildResource(),
			ToFolded = _toPicker?.Folded ?? false,
			Tolerance = _tolerancePicker?.BuildResource(),
			ToleranceFolded = _tolerancePicker?.Folded ?? true,
		};
	}

	public override bool TryGetInlineSummary(out string summary)
	{
		summary = "Nav Reachable 2D";
		return true;
	}

	public override void ClearCallbacks()
	{
		base.ClearCallbacks();
		_onChanged = null;
		_fromPicker?.ClearCallbacks();
		_toPicker?.ClearCallbacks();
		_tolerancePicker?.ClearCallbacks();
		_fromPicker = null;
		_toPicker = null;
		_tolerancePicker = null;
	}

	private NestedResolverPicker BuildPointPicker(
		StatescriptGraph graph,
		VBoxContainer root,
		string title,
		StatescriptResolverResource? existing,
		bool folded)
	{
		var picker = new NestedResolverPicker();
		picker.Initialize(
			graph,
			existing ?? new EntityPosition2DResolverResource(),
			title,
			_pointExpectedTypes,
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
