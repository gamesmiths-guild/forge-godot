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
/// Resolver editor that provides an ability data value from a <see cref="ForgeAbilityData"/> resource, for ability
/// grant and lookup node inputs.
/// </summary>
[Tool]
internal sealed partial class AbilityDataResolverEditor : NodeEditorProperty
{
	private const float LabelWidth = 60.0f;

	private Action? _onChanged;
	private ForgeAbilityData? _selectedAbilityData;

	public override string DisplayName => "Ability Data";

	public override string ResolverTypeId => "AbilityData";

	public override bool IsCompatibleWith(Type expectedType)
	{
		return expectedType == typeof(AbilityData);
	}

	public override void Setup(
		StatescriptGraph graph,
		StatescriptNodeProperty? property,
		Type expectedType,
		Action onChanged,
		bool isArray)
	{
		_onChanged = onChanged;

		var existingResource = property?.Resolver as AbilityDataResolverResource;
		_selectedAbilityData = existingResource?.AbilityData;

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
			_onChanged?.Invoke();
		};
		root.AddChild(ResolverEditorLayoutUtilities.CreateLabeledRow("Ability:", abilityPicker, LabelWidth));
	}

	public override void SaveTo(StatescriptNodeProperty property)
	{
		property.Resolver = new AbilityDataResolverResource
		{
			AbilityData = _selectedAbilityData,
		};
	}
}
#endif
