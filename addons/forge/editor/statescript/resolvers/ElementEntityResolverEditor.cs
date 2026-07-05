// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers;

[Tool]
internal sealed partial class ElementEntityResolverEditor : NodeEditorProperty
{
	public override string DisplayName => "Element Entity";

	public override string ResolverTypeId => "ElementEntity";

	public override bool RequiresIterationScope => true;

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
		AddChild(new Label { Text = "Reads the iterated entity element." });
	}

	public override void SaveTo(StatescriptNodeProperty property)
	{
		property.Resolver = new ElementEntityResolverResource();
	}

	public override bool TryGetInlineSummary(out string summary)
	{
		summary = "Element Entity";
		return true;
	}
}
#endif
