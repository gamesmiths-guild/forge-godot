// Copyright © Gamesmiths Guild.

#if TOOLS
using System;
using Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers.Bases;
using Gamesmiths.Forge.Godot.Resources.Statescript;
using Gamesmiths.Forge.Godot.Resources.Statescript.Resolvers;
using Godot;
using ForgeVariant128 = Gamesmiths.Forge.Statescript.Variant128;
using NumericsVector2 = System.Numerics.Vector2;

namespace Gamesmiths.Forge.Godot.Editor.Statescript.Resolvers;

/// <summary>
/// Editor for <see cref="MouseWorldPosition2DResolverResource"/>: its only row picks the entity whose input supplies
/// the aim.
/// </summary>
[Tool]
internal sealed partial class MouseWorldPosition2DResolverEditor : EntityScopedResolverEditorBase
{
	public override string DisplayName => "Mouse World Position 2D";

	public override string ResolverTypeId => "MouseWorldPosition2D";

	public override bool IsCompatibleWith(Type expectedType)
	{
		return expectedType == typeof(NumericsVector2) || expectedType == typeof(ForgeVariant128);
	}

	public override void Setup(
		StatescriptGraph graph,
		StatescriptNodeProperty? property,
		Type expectedType,
		Action onChanged,
		bool isArray)
	{
		InitializeEntityScope(
			graph,
			onChanged,
			(property?.Resolver as MouseWorldPosition2DResolverResource)?.InputSource);

		SizeFlagsHorizontal = SizeFlags.ExpandFill;
		var root = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		AddChild(root);

		root.AddChild(CreateEntitySelectorRow("Input Source:"));
		root.AddChild(new Label { Text = "The input source's aim, or the owner's viewport without one." });
	}

	public override void SaveTo(StatescriptNodeProperty property)
	{
		property.Resolver = new MouseWorldPosition2DResolverResource { InputSource = BuildEntityResolverResource() };
	}

	public override bool TryGetInlineSummary(out string summary)
	{
		summary = "Mouse World Position 2D";
		return true;
	}
}
#endif
