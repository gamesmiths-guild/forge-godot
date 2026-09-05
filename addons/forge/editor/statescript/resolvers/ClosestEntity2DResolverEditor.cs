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
/// Resolver editor that reads the nearest of a group of entities to a point.
/// </summary>
[Tool]
internal sealed partial class ClosestEntity2DResolverEditor : NodeEditorProperty
{
	private static readonly Type[] _entitiesExpectedTypes = [typeof(IForgeEntity)];
	private static readonly Type[] _pointExpectedTypes = [typeof(NumericsVector2)];

	private Action? _onChanged;
	private NestedResolverPicker? _entitiesPicker;
	private NestedResolverPicker? _positionPicker;

	public override string DisplayName => "Closest Entity 2D";

	public override string ResolverTypeId => "ClosestEntity2D";

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
		var resource = property?.Resolver as ClosestEntity2DResolverResource;
		_onChanged = onChanged;

		SizeFlagsHorizontal = SizeFlags.ExpandFill;
		var root = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		AddChild(root);

		// Not seeded: the group is whatever the graph already found - an overlap's results, a variable, a query minus
		// everyone already hit - and seeding one of those would be guessing which. It lands on the picker's own
		// default instead, and an unset group finds nobody.
		_entitiesPicker = AddPicker(
			graph,
			root,
			"Entities:",
			resource?.Entities,
			_entitiesExpectedTypes,
			isArray: true,
			resource?.EntitiesFolded ?? false);

		// Seeded with the caster's position: nearest is nearly always nearest to whoever is asking, and a nested
		// operand has no unbound state, so an unseeded point would silently be the world origin.
		_positionPicker = AddPicker(
			graph,
			root,
			"Position:",
			resource?.Position ?? new EntityPosition2DResolverResource(),
			_pointExpectedTypes,
			isArray: false,
			resource?.PositionFolded ?? true);
	}

	public override void SaveTo(StatescriptNodeProperty property)
	{
		property.Resolver = new ClosestEntity2DResolverResource
		{
			Entities = _entitiesPicker?.BuildResource(),
			EntitiesFolded = _entitiesPicker?.Folded ?? false,
			Position = _positionPicker?.BuildResource(),
			PositionFolded = _positionPicker?.Folded ?? true,
		};
	}

	public override bool TryGetInlineSummary(out string summary)
	{
		summary = "Closest Entity 2D";
		return true;
	}

	public override bool TryGetHighlightedVariableName(out string variableName)
	{
		if (_entitiesPicker is not null && _entitiesPicker.TryGetHighlightedVariableName(out variableName))
		{
			return true;
		}

		variableName = string.Empty;
		return false;
	}

	public override void ClearCallbacks()
	{
		base.ClearCallbacks();
		_onChanged = null;
		_entitiesPicker?.ClearCallbacks();
		_entitiesPicker = null;
		_positionPicker?.ClearCallbacks();
		_positionPicker = null;
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
