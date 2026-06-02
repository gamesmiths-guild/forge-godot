// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Gamesmiths.Forge.Statescript;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers;

[Tool]
internal sealed partial class AbilityLevelResolverEditor : NodeEditorProperty
{
	public override string DisplayName => "Ability Level";

	public override string ResolverTypeId => "AbilityLevel";

	public override bool SupportsArrayValues => false;

	public override bool IsCompatibleWith(Type expectedType)
	{
		return expectedType == typeof(Variant128)
			|| StatescriptVariableTypeConverter.IsCompatible(expectedType, StatescriptVariableType.Int);
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
			Text = "Uses the current ability level.",
			HorizontalAlignment = HorizontalAlignment.Center,
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
		});
	}

	public override void SaveTo(StatescriptNodeProperty property)
	{
		property.Resolver = new AbilityLevelResolverResource();
	}

	public override bool TryGetInlineSummary(out string summary)
	{
		summary = "Ability Level";
		return true;
	}
}
#endif
