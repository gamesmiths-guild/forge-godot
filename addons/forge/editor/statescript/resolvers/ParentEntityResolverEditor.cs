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
/// Resolver editor that reads the entity an entity is nested inside.
/// </summary>
[Tool]
internal sealed partial class ParentEntityResolverEditor : EntityScopedResolverEditorBase
{
	public override string DisplayName => "Parent Entity";

	public override string ResolverTypeId => "ParentEntity";

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
		var existingResource = property?.Resolver as ParentEntityResolverResource;

		InitializeEntityScope(graph, onChanged, existingResource?.EntityResolver);

		SizeFlagsHorizontal = SizeFlags.ExpandFill;
		var root = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		AddChild(root);

		root.AddChild(CreateEntitySelectorRow("Of:"));
	}

	public override void SaveTo(StatescriptNodeProperty property)
	{
		property.Resolver = new ParentEntityResolverResource
		{
			EntityResolver = BuildEntityResolverResource(),
		};
	}

	public override bool TryGetInlineSummary(out string summary)
	{
		summary = "Parent Entity";
		return true;
	}
}
#endif
