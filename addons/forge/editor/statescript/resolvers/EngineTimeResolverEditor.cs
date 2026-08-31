// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Godot;
using ForgeVariant128 = Gamesmiths.Forge.Statescript.Variant128;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers;

/// <summary>
/// Resolver editor that reads how long the game has been running, in seconds.
/// </summary>
[Tool]
internal sealed partial class EngineTimeResolverEditor : NodeEditorProperty
{
	public override string DisplayName => "Engine Time";

	public override string ResolverTypeId => "EngineTime";

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
		AddChild(new Label { Text = "Seconds since the game started." });
	}

	public override void SaveTo(StatescriptNodeProperty property)
	{
		property.Resolver = new EngineTimeResolverResource();
	}

	public override bool TryGetInlineSummary(out string summary)
	{
		summary = "Engine Time";
		return true;
	}
}
#endif
