// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using Gamesmiths.Forge.Abilities;
using Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Godot.Resources.Abilities;
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers;

/// <summary>
/// Resolver editor that looks up the handle of a granted ability on an entity — the entry point for cross-ability
/// queries.
/// </summary>
[Tool]
internal sealed partial class GetAbilityHandleResolverEditor : EntityScopedResolverEditorBase
{
	private const float LabelWidth = 60.0f;

	private ForgeAbilityData? _selectedAbilityData;
	private EntityOperandPicker? _sourcePicker;
	private bool _exactSourceMatch;

	public override string DisplayName => "Get Ability";

	public override string ResolverTypeId => "GetAbilityHandle";

	public override bool IsCompatibleWith(Type expectedType)
	{
		return expectedType == typeof(AbilityHandle);
	}

	public override void Setup(
		StatescriptGraph graph,
		StatescriptNodeProperty? property,
		Type expectedType,
		Action onChanged,
		bool isArray)
	{
		var existingResource = property?.Resolver as GetAbilityHandleResolverResource;
		_selectedAbilityData = existingResource?.AbilityData;
		_exactSourceMatch = existingResource?.ExactSourceMatch ?? false;

		InitializeEntityScope(graph, onChanged, existingResource?.EntityResolver);

		SizeFlagsHorizontal = SizeFlags.ExpandFill;
		var root = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		AddChild(root);

		var abilityPicker = new EditorResourcePicker
		{
			BaseType = nameof(ForgeAbilityData),
			EditedResource = _selectedAbilityData,
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
		};
		abilityPicker.ResourceChanged += resource =>
		{
			_selectedAbilityData = resource as ForgeAbilityData;
			NotifyChanged();
		};
		root.AddChild(ResolverEditorLayoutUtilities.CreateLabeledRow("Ability:", abilityPicker, LabelWidth));

		root.AddChild(CreateEntitySelectorRow(LabelWidth));
		root.AddChild(CreateEntityScopeEditorRow(LabelWidth));

		// The source filter is genuinely three-state, so it needs a None entry the other entity pickers do not have:
		// None matches any granting source, a selected entity narrows the lookup to grants from that entity, and None
		// combined with "Exact source match" finds only grants made without a source at all.
		_sourcePicker = new EntityOperandPicker();
		_sourcePicker.Initialize(
			graph,
			existingResource?.SourceResolver,
			"Source:",
			LabelWidth,
			NotifyChanged,
			RaiseLayoutSizeChanged,
			IterationScope,
			allowNone: true);
		root.AddChild(_sourcePicker);

		var exactSourceCheckBox = new CheckBox
		{
			Text = "Exact source match",
			ButtonPressed = _exactSourceMatch,
			TooltipText = "Match only the instance granted by exactly the resolved source. With the source set to " +
				"None, finds only abilities granted without a source.",
		};
		exactSourceCheckBox.Toggled += toggledOn =>
		{
			_exactSourceMatch = toggledOn;
			NotifyChanged();
		};
		root.AddChild(exactSourceCheckBox);

		PopulateEntityScopeEditor(existingResource?.EntityResolver);
	}

	public override void SaveTo(StatescriptNodeProperty property)
	{
		property.Resolver = new GetAbilityHandleResolverResource
		{
			AbilityData = _selectedAbilityData,
			EntityResolver = BuildEntityResolverResource(),
			SourceResolver = _sourcePicker?.BuildResource(),
			ExactSourceMatch = _exactSourceMatch,
		};
	}

	public override void ClearCallbacks()
	{
		base.ClearCallbacks();
		_sourcePicker?.ClearCallbacks();
	}
}
#endif
