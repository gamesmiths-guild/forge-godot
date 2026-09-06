// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Godot;
using NumericsVector2 = System.Numerics.Vector2;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers;

/// <summary>
/// Resolver editor that reads the nearest point on the navigation mesh to a point.
/// </summary>
[Tool]
internal sealed partial class NavClosestPoint2DResolverEditor : NodeEditorProperty
{
	private static readonly Type[] _pointExpectedTypes = [typeof(NumericsVector2)];

	private Action? _onChanged;
	private NestedResolverPicker? _pointPicker;

	public override string DisplayName => "Nav Closest Point 2D";

	public override string ResolverTypeId => "NavClosestPoint2D";

	public override bool IsCompatibleWith(Type expectedType)
	{
		return expectedType == typeof(NumericsVector2) || expectedType == typeof(Forge.Statescript.Variant128);
	}

	public override void Setup(
		StatescriptGraph graph,
		StatescriptNodeProperty? property,
		Type expectedType,
		Action onChanged,
		bool isArray)
	{
		var resource = property?.Resolver as NavClosestPoint2DResolverResource;
		_onChanged = onChanged;

		SizeFlagsHorizontal = SizeFlags.ExpandFill;
		var root = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		AddChild(root);

		// Seeded with Entity Position 2D like the other point operands, though the point being clamped is more often
		// a cursor or an offset - a seed that resolves to somebody real still beats one that resolves to the origin.
		_pointPicker = new NestedResolverPicker();
		_pointPicker.Initialize(
			graph,
			resource?.Point ?? new EntityPosition2DResolverResource(),
			"Of:",
			_pointExpectedTypes,
			isArray: false,
			resource?.PointFolded ?? false,
			NotifyChanged,
			RaiseLayoutSizeChanged,
			IterationScope);
		root.AddChild(_pointPicker);
	}

	public override void SaveTo(StatescriptNodeProperty property)
	{
		property.Resolver = new NavClosestPoint2DResolverResource
		{
			Point = _pointPicker?.BuildResource(),
			PointFolded = _pointPicker?.Folded ?? false,
		};
	}

	public override bool TryGetInlineSummary(out string summary)
	{
		summary = "Nav Closest Point 2D";
		return true;
	}

	public override void ClearCallbacks()
	{
		base.ClearCallbacks();
		_onChanged = null;
		_pointPicker?.ClearCallbacks();
		_pointPicker = null;
	}

	private void NotifyChanged()
	{
		_onChanged?.Invoke();
	}
}
#endif
