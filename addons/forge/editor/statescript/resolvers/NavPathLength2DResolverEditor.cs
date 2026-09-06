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
/// Resolver editor that reads how far the walk between two points actually is.
/// </summary>
[Tool]
internal sealed partial class NavPathLength2DResolverEditor : NodeEditorProperty
{
	private static readonly Type[] _pointExpectedTypes = [typeof(NumericsVector2)];

	private Action? _onChanged;
	private NestedResolverPicker? _fromPicker;
	private NestedResolverPicker? _toPicker;

	public override string DisplayName => "Nav Path Length 2D";

	public override string ResolverTypeId => "NavPathLength2D";

	public override bool IsCompatibleWith(Type expectedType)
	{
		return expectedType == typeof(float)
			|| expectedType == typeof(double)
			|| expectedType == typeof(Forge.Statescript.Variant128);
	}

	public override void Setup(
		StatescriptGraph graph,
		StatescriptNodeProperty? property,
		Type expectedType,
		Action onChanged,
		bool isArray)
	{
		var resource = property?.Resolver as NavPathLength2DResolverResource;
		_onChanged = onChanged;

		SizeFlagsHorizontal = SizeFlags.ExpandFill;
		var root = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		AddChild(root);

		_fromPicker = BuildPointPicker(graph, root, "From:", resource?.From, resource?.FromFolded ?? false);
		_toPicker = BuildPointPicker(graph, root, "To:", resource?.To, resource?.ToFolded ?? false);
	}

	public override void SaveTo(StatescriptNodeProperty property)
	{
		property.Resolver = new NavPathLength2DResolverResource
		{
			From = _fromPicker?.BuildResource(),
			FromFolded = _fromPicker?.Folded ?? false,
			To = _toPicker?.BuildResource(),
			ToFolded = _toPicker?.Folded ?? false,
		};
	}

	public override bool TryGetInlineSummary(out string summary)
	{
		summary = "Nav Path Length 2D";
		return true;
	}

	public override void ClearCallbacks()
	{
		base.ClearCallbacks();
		_onChanged = null;
		_fromPicker?.ClearCallbacks();
		_toPicker?.ClearCallbacks();
		_fromPicker = null;
		_toPicker = null;
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
