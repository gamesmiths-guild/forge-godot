// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Godot;
using ForgeVariant128 = Gamesmiths.Forge.Statescript.Variant128;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers;

/// <summary>
/// Resolver editor that reads how long the tick currently running has taken, in seconds.
/// </summary>
[Tool]
internal sealed partial class DeltaTimeResolverEditor : NodeEditorProperty
{
	public override string DisplayName => "Delta Time";

	public override string ResolverTypeId => "DeltaTime";

	public override bool IsCompatibleWith(Type expectedType)
	{
		return expectedType == typeof(float)
			|| expectedType == typeof(double)
			|| expectedType == typeof(ForgeVariant128);
	}

	public override void Setup(
		StatescriptGraph graph,
		StatescriptNodeProperty? property,
		Type expectedType,
		Action onChanged,
		bool isArray)
	{
		AddChild(new Label { Text = "Length of the tick being run, in seconds." });
	}

	public override void SaveTo(StatescriptNodeProperty property)
	{
		property.Resolver = new DeltaTimeResolverResource();
	}

	public override bool TryGetInlineSummary(out string summary)
	{
		summary = "Delta Time";
		return true;
	}
}
#endif
