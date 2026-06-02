// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using Gamesmiths.Forge.Core;
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers;

[Tool]
internal sealed partial class AbilitySourceResolverEditor : NodeEditorProperty
{
	public override string DisplayName => "Ability Source";

	public override string ResolverTypeId => "AbilitySource";

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
		AddChild(new Label { Text = "Uses the current ability source." });
	}

	public override void SaveTo(StatescriptNodeProperty property)
	{
		property.Resolver = new AbilitySourceResolverResource();
	}

	public override bool TryGetInlineSummary(out string summary)
	{
		summary = "Ability Source";
		return true;
	}
}
#endif
