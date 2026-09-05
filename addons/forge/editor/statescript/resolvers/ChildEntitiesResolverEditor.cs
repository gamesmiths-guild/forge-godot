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
/// Resolver editor that reads the entities nested inside an entity.
/// </summary>
[Tool]
internal sealed partial class ChildEntitiesResolverEditor : EntityScopedResolverEditorBase
{
	public override string DisplayName => "Child Entities";

	public override string ResolverTypeId => "ChildEntities";

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
		var existingResource = property?.Resolver as ChildEntitiesResolverResource;

		InitializeEntityScope(graph, onChanged, existingResource?.EntityResolver);

		SizeFlagsHorizontal = SizeFlags.ExpandFill;
		var root = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		AddChild(root);

		root.AddChild(CreateEntitySelectorRow("Of:"));
	}

	public override void SaveTo(StatescriptNodeProperty property)
	{
		property.Resolver = new ChildEntitiesResolverResource
		{
			EntityResolver = BuildEntityResolverResource(),
		};
	}

	public override bool TryGetInlineSummary(out string summary)
	{
		summary = "Child Entities";
		return true;
	}
}
#endif
