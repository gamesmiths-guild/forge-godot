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
internal sealed partial class EffectInfoResolverEditor : EntityScopedResolverEditorBase
{
	private const float LabelWidth = 60.0f;

	private ForgeEffectData? _selectedEffectData;
	private EffectInfoType _infoType;

	public override string DisplayName => "Effect Info";

	public override string ResolverTypeId => "EffectInfo";

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
		var existingResource = property?.Resolver as EffectInfoResolverResource;
		_selectedEffectData = existingResource?.EffectData;
		_infoType = existingResource?.InfoType ?? EffectInfoType.TotalStackCount;

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

		var infoDropdown = new OptionButton { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		foreach (EffectInfoType value in Enum.GetValues<EffectInfoType>())
		{
			infoDropdown.AddItem(value.ToString());
		}

		infoDropdown.Selected = (int)_infoType;
		infoDropdown.ItemSelected += index =>
		{
			_infoType = (EffectInfoType)(int)index;
			NotifyChanged();
		};
		root.AddChild(ResolverEditorLayoutUtilities.CreateLabeledRow("Info:", infoDropdown, LabelWidth));

		root.AddChild(CreateEntitySelectorRow(LabelWidth));
		root.AddChild(CreateEntityScopeEditorRow(LabelWidth));

		PopulateEntityScopeEditor(existingResource?.EntityResolver);
	}

	public override void SaveTo(StatescriptNodeProperty property)
	{
		property.Resolver = new EffectInfoResolverResource
		{
			EffectData = _selectedEffectData,
			InfoType = _infoType,
			EntityResolver = BuildEntityResolverResource(),
		};
	}
}
#endif
