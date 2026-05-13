// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers;

[Tool]
internal sealed partial class OwnerEntityResolverEditor : NodeEditorProperty
{
	public override string DisplayName => "Owner";

	public override string ResolverTypeId => "OwnerEntity";

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
		AddChild(new Label { Text = "Uses the ability owner." });
	}

	public override void SaveTo(StatescriptNodeProperty property)
	{
		property.Resolver = new OwnerEntityResolverResource();
	}

	public override bool TryGetInlineSummary(out string summary)
	{
		summary = "Owner";
		return true;
	}
}
#endif
