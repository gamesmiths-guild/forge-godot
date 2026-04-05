// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Gamesmiths.Forge.Statescript;
using Godot;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers;

/// <summary>
/// Resolver editor for the ability activation magnitude. No configuration is needed, it simply reads the magnitude from
/// the <see cref="Abilities.AbilityBehaviorContext"/> at runtime. Only compatible with <see langword="float"/> inputs.
/// </summary>
[Tool]
internal sealed partial class MagnitudeResolverEditor : NodeEditorProperty
{
	/// <inheritdoc/>
	public override string DisplayName => "Magnitude";

	/// <inheritdoc/>
	public override string ResolverTypeId => "Magnitude";

	/// <inheritdoc/>
	public override bool IsCompatibleWith(Type expectedType)
	{
		return expectedType == typeof(float) || expectedType == typeof(Variant128);
	}

	/// <inheritdoc/>
	public override void Setup(
		StatescriptGraph graph,
		StatescriptNodeProperty? property,
		Type expectedType,
		Action onChanged,
		bool isArray)
	{
		SizeFlagsHorizontal = SizeFlags.ExpandFill;

		var label = new Label
		{
			Text = "Ability Magnitude",
			HorizontalAlignment = HorizontalAlignment.Center,
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
		};

		AddChild(label);
	}

	/// <inheritdoc/>
	public override void SaveTo(StatescriptNodeProperty property)
	{
		property.Resolver = new MagnitudeResolverResource();
	}
}
#endif
