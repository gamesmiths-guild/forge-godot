// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using Gamesmiths.Forge.Effects;
using Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Godot.Resources;
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers;

/// <summary>
/// Resolver editor that queries the active effect handles on an entity, optionally filtered by an effect data
/// resource.
/// </summary>
[Tool]
internal sealed partial class QueryActiveEffectsResolverEditor : EntityScopedResolverEditorBase
{
	private const float LabelWidth = 60.0f;

	private ForgeEffectData? _selectedEffectData;

	public override string DisplayName => "Query Active Effects";

	public override string ResolverTypeId => "QueryActiveEffects";

	public override bool SupportsScalarValues => false;

	public override bool SupportsArrayValues => true;

	public override bool IsCompatibleWith(Type expectedType)
	{
		return expectedType == typeof(ActiveEffectHandle);
	}

	public override void Setup(
		StatescriptGraph graph,
		StatescriptNodeProperty? property,
		Type expectedType,
		Action onChanged,
		bool isArray)
	{
		var existingResource = property?.Resolver as QueryActiveEffectsResolverResource;
		_selectedEffectData = existingResource?.EffectData;

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
		root.AddChild(ResolverEditorLayoutUtilities.CreateLabeledRow("Filter:", effectPicker, LabelWidth));

		root.AddChild(CreateEntitySelectorRow(LabelWidth));
		root.AddChild(CreateEntityScopeEditorRow(LabelWidth));

		PopulateEntityScopeEditor(existingResource?.EntityResolver);
	}

	public override void SaveTo(StatescriptNodeProperty property)
	{
		property.Resolver = new QueryActiveEffectsResolverResource
		{
			EffectData = _selectedEffectData,
			EntityResolver = BuildEntityResolverResource(),
		};
	}
}
#endif
