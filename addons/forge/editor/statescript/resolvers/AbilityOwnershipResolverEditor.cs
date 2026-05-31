// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using Gamesmiths.Forge.Effects;
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers;

[Tool]
internal sealed partial class AbilityOwnershipResolverEditor : NodeEditorProperty
{
	public override string DisplayName => "Ability Ownership";

	public override string ResolverTypeId => "AbilityOwnership";

	public override bool SupportsArrayValues => false;

	public override bool IsCompatibleWith(Type expectedType)
	{
		return expectedType == typeof(EffectOwnership);
	}

	public override void Setup(
		StatescriptGraph graph,
		StatescriptNodeProperty? property,
		Type expectedType,
		Action onChanged,
		bool isArray)
	{
		SizeFlagsHorizontal = SizeFlags.ExpandFill;
		AddChild(new Label
		{
			Text = "Uses the current ability owner and source.",
			HorizontalAlignment = HorizontalAlignment.Center,
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
		});
	}

	public override void SaveTo(StatescriptNodeProperty property)
	{
		property.Resolver = new AbilityOwnershipResolverResource();
	}

	public override bool TryGetInlineSummary(out string summary)
	{
		summary = "Ability Ownership";
		return true;
	}
}
#endif
