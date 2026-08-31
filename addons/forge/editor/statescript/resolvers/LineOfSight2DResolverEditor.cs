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
/// Resolver editor that reports whether nothing blocks the line between two points.
/// </summary>
[Tool]
internal sealed partial class LineOfSight2DResolverEditor : NodeEditorProperty
{
	private static readonly Type[] _pointExpectedTypes = [typeof(NumericsVector2)];
	private static readonly Type[] _ignoreExpectedTypes = [typeof(IForgeEntity)];
	private static readonly Type[] _maskExpectedTypes = [typeof(int)];

	private Action? _onChanged;
	private NestedResolverPicker? _fromPicker;
	private NestedResolverPicker? _toPicker;
	private NestedResolverPicker? _ignorePicker;
	private NestedResolverPicker? _maskPicker;

	public override string DisplayName => "Line Of Sight 2D";

	public override string ResolverTypeId => "LineOfSight2D";

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
		var resource = property?.Resolver as LineOfSight2DResolverResource;
		_onChanged = onChanged;

		SizeFlagsHorizontal = SizeFlags.ExpandFill;
		var root = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		AddChild(root);

		// Seeded with Entity Position 2D on both ends: the two points are almost always somebody's position, and a
		// nested operand has no unbound state, so an unseeded one would silently be the world origin.
		_fromPicker = BuildPointPicker(graph, root, "From:", resource?.From, resource?.FromFolded ?? false);
		_toPicker = BuildPointPicker(graph, root, "To:", resource?.To, resource?.ToFolded ?? false);

		// Seeded with the caster and its target rather than left empty: both ends of a line usually sit inside a body,
		// and either one reports itself as cover. Clearing the array is how a line that nothing should pass through is
		// authored.
		_ignorePicker = new NestedResolverPicker();
		_ignorePicker.Initialize(
			graph,
			resource?.IgnoreResolver ?? EntityIgnoreOperand.BuildOwnerAndTarget(),
			"Ignore:",
			_ignoreExpectedTypes,
			isArray: true,
			resource?.IgnoreFolded ?? true,
			NotifyChanged,
			RaiseLayoutSizeChanged,
			IterationScope);
		root.AddChild(_ignorePicker);

		_maskPicker = new NestedResolverPicker();
		_maskPicker.Initialize(
			graph,
			resource?.Mask,
			"Blocked By:",
			_maskExpectedTypes,
			isArray: false,
			resource?.MaskFolded ?? true,
			NotifyChanged,
			RaiseLayoutSizeChanged,
			IterationScope);
		root.AddChild(_maskPicker);
	}

	public override void SaveTo(StatescriptNodeProperty property)
	{
		property.Resolver = new LineOfSight2DResolverResource
		{
			From = _fromPicker?.BuildResource(),
			FromFolded = _fromPicker?.Folded ?? false,
			To = _toPicker?.BuildResource(),
			ToFolded = _toPicker?.Folded ?? false,
			IgnoreResolver = _ignorePicker?.BuildResource(),
			IgnoreFolded = _ignorePicker?.Folded ?? true,
			Mask = _maskPicker?.BuildResource(),
			MaskFolded = _maskPicker?.Folded ?? true,
		};
	}

	public override bool TryGetInlineSummary(out string summary)
	{
		summary = "Line Of Sight 2D";
		return true;
	}

	public override void ClearCallbacks()
	{
		base.ClearCallbacks();
		_onChanged = null;
		_fromPicker?.ClearCallbacks();
		_toPicker?.ClearCallbacks();
		_ignorePicker?.ClearCallbacks();
		_fromPicker = null;
		_toPicker = null;
		_ignorePicker = null;
		_maskPicker?.ClearCallbacks();
		_maskPicker = null;
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
