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

	private ForgeEffectQuery? _selectedQuery;

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
		_selectedQuery = existingResource?.Query;

		InitializeEntityScope(graph, onChanged, existingResource?.EntityResolver);

		SizeFlagsHorizontal = SizeFlags.ExpandFill;
		var root = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		AddChild(root);

		var queryPicker = new EditorResourcePicker
		{
			BaseType = nameof(ForgeEffectQuery),
			EditedResource = _selectedQuery,
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
		};
		queryPicker.ResourceChanged += resource =>
		{
			_selectedQuery = resource as ForgeEffectQuery;
			NotifyChanged();
		};
		root.AddChild(ResolverEditorLayoutUtilities.CreateLabeledRow("Query:", queryPicker, LabelWidth));

		root.AddChild(CreateEntitySelectorRow());
	}

	public override void SaveTo(StatescriptNodeProperty property)
	{
		property.Resolver = new QueryActiveEffectsResolverResource
		{
			Query = _selectedQuery,
			EntityResolver = BuildEntityResolverResource(),
		};
	}
}
#endif
