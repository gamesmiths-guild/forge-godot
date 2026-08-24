// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers;

/// <summary>
/// Resolver editor that reads the entities an area already in the scene is overlapping.
/// </summary>
[Tool]
internal sealed partial class AreaOverlaps3DResolverEditor : EntityScopedResolverEditorBase
{
	private const float LabelWidth = 88.0f;

	private static readonly Type[] _ignoreExpectedTypes = [typeof(IForgeEntity)];

	private LineEdit? _nodePathField;
	private CheckBox? _includeAreasCheckBox;
	private NestedResolverPicker? _ignorePicker;

	public override string DisplayName => "Area Overlaps 3D";

	public override string ResolverTypeId => "AreaOverlaps3D";

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
		var existingResource = property?.Resolver as AreaOverlaps3DResolverResource;

		InitializeEntityScope(graph, onChanged, existingResource?.EntityResolver);

		SizeFlagsHorizontal = SizeFlags.ExpandFill;
		var root = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		AddChild(root);

		root.AddChild(CreateEntitySelectorRow());

		_nodePathField = new LineEdit
		{
			PlaceholderText = "%TriggerArea",
			Text = existingResource?.NodePath ?? string.Empty,
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			TooltipText = "The Area3D to read, as a path from the entity's own node.",
		};
		_nodePathField.TextChanged += _ => NotifyChanged();
		root.AddChild(ResolverEditorLayoutUtilities.CreateLabeledRow("Area:", _nodePathField, LabelWidth));

		_includeAreasCheckBox = new CheckBox
		{
			Text = "Include areas",
			ButtonPressed = existingResource?.IncludeAreas ?? false,
			TooltipText = "Count overlapping areas as well as bodies, for entities whose hurtbox is an Area3D.",
		};
		_includeAreasCheckBox.Toggled += _ => NotifyChanged();
		root.AddChild(_includeAreasCheckBox);

		// Seeded with the caster: an aura on someone is not meant to find that someone, and a nested operand has no
		// unbound state to mean it.
		_ignorePicker = new NestedResolverPicker();
		_ignorePicker.Initialize(
			graph,
			existingResource?.IgnoreResolver ?? EntityIgnoreOperand.BuildOwner(),
			"Ignore:",
			_ignoreExpectedTypes,
			isArray: true,
			existingResource?.IgnoreFolded ?? true,
			NotifyChanged,
			RaiseLayoutSizeChanged,
			IterationScope);
		root.AddChild(_ignorePicker);
	}

	public override void SaveTo(StatescriptNodeProperty property)
	{
		property.Resolver = new AreaOverlaps3DResolverResource
		{
			EntityResolver = BuildEntityResolverResource(),
			NodePath = _nodePathField is not null && IsInstanceValid(_nodePathField)
				? _nodePathField.Text
				: string.Empty,
			IncludeAreas = _includeAreasCheckBox is not null
				&& IsInstanceValid(_includeAreasCheckBox)
				&& _includeAreasCheckBox.ButtonPressed,
			IgnoreResolver = _ignorePicker?.BuildResource(),
			IgnoreFolded = _ignorePicker?.Folded ?? true,
		};
	}

	public override bool TryGetInlineSummary(out string summary)
	{
		summary = "Area Overlaps 3D";
		return true;
	}

	public override void ClearCallbacks()
	{
		base.ClearCallbacks();
		_nodePathField = null;
		_includeAreasCheckBox = null;
		_ignorePicker?.ClearCallbacks();
		_ignorePicker = null;
	}
}
#endif
