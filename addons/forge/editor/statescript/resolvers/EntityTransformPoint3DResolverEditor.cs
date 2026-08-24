// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers.Bases;
using Godot;
using NumericsVector3 = System.Numerics.Vector3;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers;

/// <summary>
/// Resolver editor that converts a point between an entity's local space and world space.
/// </summary>
[Tool]
internal sealed partial class EntityTransformPoint3DResolverEditor : SpatialResolverEditorBase3D
{
	private static readonly Type[] _offsetExpectedTypes = [typeof(NumericsVector3)];

	private NestedResolverPicker? _offsetPicker;
	private CheckBox? _inverseCheckBox;

	public override string DisplayName => "Entity Transform Point 3D";

	public override string ResolverTypeId => "EntityTransformPoint3D";

	protected override Type ValueClrType => typeof(NumericsVector3);

	public override bool TryGetInlineSummary(out string summary)
	{
		bool inverse = _inverseCheckBox is not null
			&& IsInstanceValid(_inverseCheckBox)
			&& _inverseCheckBox.ButtonPressed;
		summary = inverse ? "World to Local 3D" : "Local to World 3D";
		return true;
	}

	public override bool TryGetHighlightedVariableName(out string variableName)
	{
		if (_offsetPicker is not null && _offsetPicker.TryGetHighlightedVariableName(out variableName))
		{
			return true;
		}

		variableName = string.Empty;
		return false;
	}

	public override void ClearCallbacks()
	{
		base.ClearCallbacks();
		_offsetPicker?.ClearCallbacks();
		_offsetPicker = null;
		_inverseCheckBox = null;
	}

	protected override void BuildSettingsRows(VBoxContainer root, SpatialResolverResourceBase3D? existingResource)
	{
		var resource = existingResource as EntityTransformPoint3DResolverResource;

		_inverseCheckBox = new CheckBox
		{
			Text = "World to local",
			ButtonPressed = resource?.Inverse ?? false,
			TooltipText = "Off: an offset relative to the entity becomes a world point. On: the reverse.",
		};
		_inverseCheckBox.Toggled += _ => NotifyChanged();
		root.AddChild(_inverseCheckBox);

		if (Graph is null)
		{
			return;
		}

		_offsetPicker = new NestedResolverPicker();
		_offsetPicker.Initialize(
			Graph,
			resource?.Offset,
			"Offset:",
			_offsetExpectedTypes,
			isArray: false,
			resource?.OffsetFolded ?? false,
			NotifyChanged,
			RaiseLayoutSizeChanged,
			IterationScope);
		root.AddChild(_offsetPicker);
	}

	protected override SpatialResolverResourceBase3D BuildResource()
	{
		return new EntityTransformPoint3DResolverResource
		{
			Offset = _offsetPicker?.BuildResource(),
			OffsetFolded = _offsetPicker?.Folded ?? false,
			Inverse = _inverseCheckBox is not null
				&& IsInstanceValid(_inverseCheckBox)
				&& _inverseCheckBox.ButtonPressed,
		};
	}
}
#endif
