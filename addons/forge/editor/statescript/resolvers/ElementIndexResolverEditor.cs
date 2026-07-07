// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers;

[Tool]
internal sealed partial class ElementIndexResolverEditor : NodeEditorProperty
{
	public override string DisplayName => "Element Index";

	public override string ResolverTypeId => "ElementIndex";

	public override bool RequiresIterationScope => true;

	public override bool IsCompatibleWith(Type expectedType)
	{
		return StatescriptVariableTypeConverter.IsCompatible(expectedType, StatescriptVariableType.Int);
	}

	public override void Setup(
		StatescriptGraph graph,
		StatescriptNodeProperty? property,
		Type expectedType,
		Action onChanged,
		bool isArray)
	{
		AddChild(new Label { Text = "Reads the iterated element's index." });
	}

	public override void SaveTo(StatescriptNodeProperty property)
	{
		property.Resolver = new ElementIndexResolverResource();
	}

	public override bool TryGetInlineSummary(out string summary)
	{
		summary = "Element Index";
		return true;
	}
}
#endif
