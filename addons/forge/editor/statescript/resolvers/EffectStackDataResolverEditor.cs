// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Godot.Resources;
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Gamesmiths.Forge.Statescript.Properties;
using Godot;
using ForgeVariant128 = Gamesmiths.Forge.Statescript.Variant128;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers;

/// <summary>
/// Resolver editor that computes an aggregate (total stacks, instance count, max level) over the active
/// applications of an effect on an entity.
/// </summary>
[Tool]
internal sealed partial class EffectStackDataResolverEditor : EntityScopedResolverEditorBase
{
	private const float LabelWidth = 60.0f;

	private ForgeEffectData? _selectedEffectData;
	private EffectStackDataType _dataType;

	public override string DisplayName => "Effect Stack Data";

	public override string ResolverTypeId => "EffectStackData";

	public override bool IsCompatibleWith(Type expectedType)
	{
		return expectedType == typeof(int) || expectedType == typeof(ForgeVariant128);
	}

	public override void Setup(
		StatescriptGraph graph,
		StatescriptNodeProperty? property,
		Type expectedType,
		Action onChanged,
		bool isArray)
	{
		var existingResource = property?.Resolver as EffectStackDataResolverResource;
		_selectedEffectData = existingResource?.EffectData;
		_dataType = existingResource?.DataType ?? EffectStackDataType.TotalStackCount;

		InitializeEntityScope(graph, onChanged, existingResource?.EntityResolver);

		SizeFlagsHorizontal = SizeFlags.ExpandFill;
		var root = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		AddChild(root);

		var effectPicker = new EditorResourcePicker
		{
			BaseType = nameof(ForgeEffectData),
			EditedResource = _selectedEffectData,
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
		};
		effectPicker.ResourceChanged += resource =>
		{
			_selectedEffectData = resource as ForgeEffectData;
			NotifyChanged();
		};
		root.AddChild(ResolverEditorLayoutUtilities.CreateLabeledRow("Effect:", effectPicker, LabelWidth));

		var dataDropdown = new OptionButton { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		foreach (EffectStackDataType value in Enum.GetValues<EffectStackDataType>())
		{
			dataDropdown.AddItem(value.ToString());
		}

		dataDropdown.Selected = (int)_dataType;
		dataDropdown.ItemSelected += index =>
		{
			_dataType = (EffectStackDataType)(int)index;
			NotifyChanged();
		};
		root.AddChild(ResolverEditorLayoutUtilities.CreateLabeledRow("Data:", dataDropdown, LabelWidth));

		root.AddChild(CreateEntitySelectorRow(LabelWidth));
		root.AddChild(CreateEntityScopeEditorRow(LabelWidth));

		PopulateEntityScopeEditor(existingResource?.EntityResolver);
	}

	public override void SaveTo(StatescriptNodeProperty property)
	{
		property.Resolver = new EffectStackDataResolverResource
		{
			EffectData = _selectedEffectData,
			DataType = _dataType,
			EntityResolver = BuildEntityResolverResource(),
		};
	}
}
#endif
